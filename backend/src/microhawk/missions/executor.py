import asyncio
from microhawk.events.dwell import DwellTracker
from microhawk.missions.models import MissionAction
from microhawk.perception.images import ImagePerception
from microhawk.reporting.grounded import report
from microhawk.transport.link import FlightFailure
from microhawk.transport.endpoint import FlightEndpoint
from microhawk.world.registry import TARGETS, vector

class MissionExecutor:
    def __init__(self, link: FlightEndpoint, memory):
        self.link=link;self.memory=memory;self.perception=ImagePerception(memory.root)
        self.current=None;self.latest_image=None;self.task=None

    def decision(self, reason, **data):
        self.current["decisions"].append(dict(tick=(self.link.telemetry or {}).get("tick"),reason=reason,**data))

    async def flight(self, kind, parameters=None):
        outcome=await self.link.command(kind,parameters)
        # The terminal outcome can precede the next 10 Hz snapshot; wait for a matching post-outcome tick.
        await self.link.wait_tick(outcome["tick"])
        self.current["outcomes"].append(outcome)
        self.current["telemetry"].append(self.link.telemetry)
        return outcome

    async def move(self, target_id, position=None):
        target=TARGETS[target_id]
        try: await self.flight("MoveTo",{"position":vector(position or target.position)})
        except FlightFailure as error:
            if error.outcome["reason"] not in {"BlockedRoute","Obstacle"} or target.alternate is None or self.current["replans"]>=self.current["plan"]["intent"]["mission"]["policy"]["max_replans"]: raise
            self.current["replans"]+=1
            self.decision("Unity rejected the route; retrying one configured alternate stand-off through the same safety gateway.",rejection=error.outcome,alternate=vector(target.alternate))
            await self.flight("MoveTo",{"position":vector(target.alternate)})

    async def observe(self, target_id):
        frame=await self.link.capture(vector(TARGETS[target_id].look))
        detections,path,overlay=self.perception.process(frame,self.current["id"],self.current["plan"]["intent"]["mission"]["perception"])
        self.latest_image=overlay
        self.current["evidence"].append(dict(path=path,overlay=overlay,provenance={k:v for k,v in frame.items() if k!="jpeg"},target_context=target_id))
        self.current["detections"].extend(d.model_dump() for d in detections)
        return frame,detections

    async def watch(self, action, policy):
        rule=policy.conditional
        tracker=DwellTracker(rule.dwell_seconds if rule else 10.)
        start=self.link.telemetry["simulation_seconds"]
        self.decision("Monitoring camera observations using simulation time and configured Zone A geometry.")
        while self.link.telemetry["simulation_seconds"]-start < policy.watch_seconds:
            if self.link.telemetry["safety_status"] in {"ReturnHomeRequired","EmergencyLandRequired"}: raise FlightFailure({"status":"Interrupted","reason":self.link.telemetry["safety_reason"]})
            frame,detections=await self.observe(action.target)
            events=tracker.update(frame,detections)
            self.current["tracks"]=[vars(t) for t in tracker.tracks]
            self.current["events"].extend(events)
            if events and rule:
                if self.current["replans"]>=policy.max_replans: raise ValueError("Replan budget exhausted")
                self.current["replans"]+=1
                branch=[]
                if rule.investigate: branch.append(MissionAction(kind="Investigate",target=action.target))
                if rule.capture_evidence: branch.append(MissionAction(kind="CaptureEvidence",target=action.target))
                self.current["revisions"].append(dict(revision=self.current["replans"],event_id=events[0]["event_id"],actions=[a.model_dump() for a in branch]))
                self.decision(f"Observed person dwell {events[0]['dwell_seconds']:.1f}s exceeded the operator threshold; executing the bounded investigation branch.",event_id=events[0]["event_id"])
                for step in branch:
                    if step.kind=="Investigate":
                        # Approach along the configured observation corridor; never fly to a detected person's body.
                        await self.move(step.target, (9.,2.,4.535))
                        await self.flight("Hold",{"duration":1.})
                    else: await self.observe(step.target)
                if rule.return_home: self.current["conditional_return"]=True
                return
            self.memory.save(self.current)
            await self.link.wait_tick(frame["tick"]+30)
        self.decision("Observation window ended without a qualifying conditional event; no event was fabricated.")

    async def execute(self, mission, raw_intent=None):
        if self.current and self.current["status"]=="Executing": raise RuntimeError("Mission already executing")
        self.current=dict(id=mission.id,request=mission.request,plan=mission.model_dump(),raw_intent=raw_intent,status="Executing",progress=0,outcomes=[],telemetry=[],detections=[],events=[],evidence=[],decisions=[],replans=0,revisions=[],tracks=[])
        self.decision("Validated structured mission; every flight action remains subject to Unity safety.")
        try:
            if not self.link.connected or not self.link.telemetry: raise RuntimeError("Simulator is not connected")
            if self.link.telemetry["state"]!="Landed": raise RuntimeError("Start a new mission only from Landed; use Return Home first")
            for index,action in enumerate(mission.actions):
                self.current["progress"]=index
                self.current["active_action"]=action.model_dump()
                if action.kind=="MoveTo": await self.move(action.target)
                elif action.kind in {"ObserveArea","CaptureEvidence"}:
                    # Rate-limited snapshots are scheduled by simulation ticks.
                    if self.current["evidence"]: await self.link.wait_tick(self.current["evidence"][-1]["provenance"]["tick"]+25)
                    frame,_=await self.observe(action.target)
                elif action.kind=="WatchZone": await self.watch(action,mission.intent.mission.policy)
                else:
                    if action.kind=="ReturnHome": self.decision("Returning home because the validated operator mission policy requests safe completion at home.")
                    await self.flight(action.kind,action.parameters)
                self.memory.save(self.current)
            if self.current.get("conditional_return") and self.link.telemetry["state"]!="Landed": await self.flight("ReturnHome")
            self.current["progress"]=len(mission.actions)
            self.current["status"]="Completed"
        except asyncio.CancelledError:
            self.current["status"]="Interrupted";self.current["failure"]="Operator intervention";self.decision("Operator interrupted mission sequencing; follow-up action passes through Unity safety.")
            raise
        except Exception as error:
            self.current["status"]="Failed";self.current["failure"]=error.outcome if isinstance(error,FlightFailure) else str(error)
            self.decision("Mission sequencing stopped after failure; Unity safety remains authoritative.",failure=self.current["failure"])
            # Never resend an uncertain action. If still connected, request a validated RTH once, or preserve failsafe.
            if self.link.connected and self.link.telemetry["state"] not in {"Landed","Armed","ReturningHome","Landing","Emergency"}:
                try: await self.flight("ReturnHome")
                except Exception as recovery: self.decision("Recovery RTH did not complete; inspect the reported safety state.",error=str(recovery))
        finally:
            self.current["final_telemetry"]=self.link.telemetry
            self.current["protocol_events"]=list(self.link.events)
            self.current["report"]=report(self.current)
            self.memory.save(self.current)
        return self.current

