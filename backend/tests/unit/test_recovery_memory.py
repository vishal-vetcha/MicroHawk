import json
from types import SimpleNamespace
from unittest.mock import AsyncMock
import pytest
from pydantic import ValidationError
from microhawk.missions.executor import MissionExecutor
from microhawk.transport.link import FlightFailure
from microhawk.memory.store import MissionMemory
from microhawk.reporting.grounded import explain
from microhawk.contracts.sensors import CameraFrame
from test_perception import load

@pytest.mark.asyncio
async def test_blocked_route_replans_once_to_known_alternate(tmp_path):
    link=SimpleNamespace(telemetry={"tick":1},command=AsyncMock(side_effect=[FlightFailure({"status":"Rejected","reason":"BlockedRoute"}),{"tick":2,"status":"Completed"}]),wait_tick=AsyncMock())
    executor=MissionExecutor(link,MissionMemory(tmp_path));executor.current={"id":"test","replans":0,"plan":{"intent":{"mission":{"policy":{"max_replans":1}}}},"decisions":[],"outcomes":[],"telemetry":[]}
    await executor.move("inspection_wall_1")
    assert executor.current["replans"]==1 and link.command.await_count==2
    assert link.command.call_args.args[1]["position"]=={"east":20.,"north":-12.,"up":4.535}

@pytest.mark.asyncio
async def test_replan_budget_does_not_override_unity(tmp_path):
    link=SimpleNamespace(telemetry={"tick":1},command=AsyncMock(side_effect=FlightFailure({"status":"Rejected","reason":"BlockedRoute"})))
    executor=MissionExecutor(link,MissionMemory(tmp_path));executor.current={"replans":0,"plan":{"intent":{"mission":{"policy":{"max_replans":0}}}}}
    with pytest.raises(FlightFailure):await executor.move("inspection_wall_1")
    assert link.command.await_count==1

def test_mission_memory_persists_exact_facts(tmp_path):
    memory=MissionMemory(tmp_path);record={"id":"mission1","status":"Failed","failure":"BlockedRoute"};memory.save(record)
    assert MissionMemory(tmp_path).last()==record

def test_explanation_uses_actual_safety_event():
    assert 'CriticalBattery' in explain(None,[{"reason":"CriticalBattery","status":"Accepted","tick":500}])

@pytest.mark.parametrize("change",[{"session_id":"../../escape"},{"tick":-1},{"width":4096},{"simulation_seconds":float('nan')},{"unknown":True}])
def test_invalid_sensor_metadata_rejected(change):
    frame=load('wall');frame.update(change)
    with pytest.raises(ValidationError):CameraFrame.model_validate(frame)

@pytest.mark.asyncio
async def test_invalid_model_output_exhausts_bounded_retry(monkeypatch):
    import httpx
    from microhawk.agents.intent import IntentAgent
    calls=[]
    def handler(request):
        calls.append(request)
        return httpx.Response(200,json={"response":'{"kind":"Teleport","forces":[1,2,3]}'})
    client=httpx.AsyncClient(transport=httpx.MockTransport(handler))
    monkeypatch.setattr('microhawk.agents.intent.httpx.AsyncClient',lambda **kwargs:client)
    with pytest.raises(ValueError,match='could not produce'):await IntentAgent().interpret('arbitrary prompt')
    assert len(calls)==3
