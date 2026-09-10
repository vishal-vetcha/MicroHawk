from pathlib import Path
import json
from pydantic import TypeAdapter
from microhawk.contracts.wire import CommandEnvelope, Handshake, Telemetry, Outcome
from microhawk.contracts.sensors import CameraFrame, MissionEvent
root=Path(__file__).resolve().parents[1]
for name,model in {"command":TypeAdapter(CommandEnvelope),"handshake":Handshake,"telemetry":Telemetry,"outcome":Outcome,"frame":CameraFrame,"mission_event":MissionEvent}.items():
    schema=model.json_schema() if isinstance(model,TypeAdapter) else model.model_json_schema()
    (root/"contracts/schemas/v1"/f"{name}.json").write_text(json.dumps(schema,indent=2))
base=dict(type="command",schema_version=1,session_id="session",authority_id="authority",command_id="test1",sequence=1,issued_at_tick=0,expires_at_tick=100,action_type="Takeoff",parameters={"altitude":4.0})
for name,data in {"valid_takeoff":base,"invalid_extra":{**base,"force":100},"invalid_type":{**base,"parameters":{"altitude":"4"}},"invalid_action":{**base,"action_type":"Teleport"},"invalid_lifetime":{**base,"expires_at_tick":0}}.items():
    (root/"contracts/fixtures/v1"/f"{name}.json").write_text(json.dumps(data,indent=2))
