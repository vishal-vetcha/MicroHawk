import uuid
from microhawk.missions.models import Mission, MissionAction

class MissionPlanner:
    def plan(self, text, intent):
        request=intent.mission
        if request is None: raise ValueError("Not a mission intent")
        target=request.target.id
        actions=[MissionAction(kind="Arm"),MissionAction(kind="Takeoff",parameters={"altitude":4.}),MissionAction(kind="MoveTo",target=target),MissionAction(kind="Hold",parameters={"duration":1.})]
        if request.operation=="WatchZone": actions.append(MissionAction(kind="WatchZone",target=target))
        elif request.operation=="InspectTarget": actions.extend([MissionAction(kind="ObserveArea",target=target),MissionAction(kind="CaptureEvidence",target=target)])
        else: actions.append(MissionAction(kind="CaptureEvidence",target=target))
        if request.policy.return_home: actions.append(MissionAction(kind="ReturnHome"))
        return Mission(id=uuid.uuid4().hex,request=text,intent=intent,actions=actions)
