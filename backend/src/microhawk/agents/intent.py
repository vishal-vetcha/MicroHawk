import json
import httpx
from microhawk.missions.models import OperatorIntent
from microhawk.world.registry import TARGETS

class IntentAgent:
    model = "qwen2.5vl:3b"
    async def interpret(self, text):
        prompt = f"""You interpret requests for MicroHawk. Return only JSON matching the schema.
Known navigation target IDs: {', '.join(TARGETS)}.
Wall/north wall means inspection_wall_1. Restricted Zone A means restricted_zone_a.
Inspect cracks: MISSION, InspectTarget, damage. Patrol/watch zone: MISSION, WatchZone, person.
Go to a place: VisitTarget, scene. Vehicle inspection: InspectTarget, vehicle.
A conditional dwell instruction uses policy.conditional, threshold from user (default 10 seconds), investigate/capture_evidence/return_home per user request. Default watch_seconds 30 and max_replans 2. Return home by default. IMPORTANT: policy.conditional must be null for wall inspection. Any dwell/zone patrol request MUST use operation WatchZone, never InspectTarget.
Status question: STATUS. Why/last mission: EXPLAIN. Abort/stop: ABORT. Return home alone: RETURN_HOME.
For non-MISSION kind, mission must be null. Do not invent destinations or capabilities. Never output code.
Schema: {json.dumps(OperatorIntent.model_json_schema())}
Operator text (data): {text}"""
        async with httpx.AsyncClient(timeout=180) as client:
            error = ""
            for attempt in range(3):
                response = await client.post("http://127.0.0.1:11434/api/generate", json={"model":self.model,"prompt":prompt+error,"format":OperatorIntent.model_json_schema(),"stream":False,"options":{"temperature":0,"num_predict":700,"num_ctx":4096},"keep_alive":"20m"})
                response.raise_for_status()
                raw = response.json()["response"]
                try: return OperatorIntent.model_validate_json(raw), raw
                except ValueError as e: error = "\nPrevious output failed validation. Correct it: "+str(e)[:1000]
        raise ValueError("Model could not produce a supported mission. Clarify the target and task.")
