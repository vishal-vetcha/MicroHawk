import asyncio
import json
import pytest
from websockets.asyncio.client import connect
from microhawk.transport.link import SimulatorLink, FlightFailure

HANDSHAKE=dict(type="handshake",schema_version=1,session_id="s",authority_id="a",current_tick=0,simulator_version="test",world_revision="industrial-test-v1",coordinate_frame="facility_enu_meters",capabilities=["Arm"])
TELEMETRY=dict(type="telemetry",schema_version=1,session_id="s",tick=0,simulation_seconds=0.,position=dict(east=0.,north=0.,up=0.),velocity=dict(east=0.,north=0.,up=0.),heading=0.,altitude=0.,battery=100.,state="Landed",armed=False,command=None,target=None,home=dict(east=0.,north=0.,up=0.),safety_status="Approved",safety_reason="None")

@pytest.mark.asyncio
async def test_executor_waits_terminal_outcome_and_disconnect_fails_pending():
    # In-process socket fixture tests Python transport, not Unity physical flight.
    link=SimulatorLink("fixture-token")
    from websockets.asyncio.server import serve
    link.server=await serve(link._connection,"127.0.0.1",0)
    port=link.server.sockets[0].getsockname()[1]
    try:
        async with connect(f"ws://127.0.0.1:{port}/simulator",additional_headers={"Authorization":"Bearer fixture-token"}) as peer:
            await peer.send(json.dumps(HANDSHAKE));await peer.send(json.dumps(TELEMETRY))
            while not link.telemetry:await asyncio.sleep(.01)
            task=asyncio.create_task(link.command("Arm"))
            while True:
                command=json.loads(await peer.recv())
                if command["type"]=="command":break
            outcome=dict(type="outcome",schema_version=1,session_id="s",command_id=command["command_id"],tick=1,status="Accepted",reason="None")
            await peer.send(json.dumps(outcome));await asyncio.sleep(.02);assert not task.done()
            outcome["status"]="Completed";await peer.send(json.dumps(outcome));assert (await task)["status"]=="Completed"
            second=asyncio.create_task(link.command("Arm"));await asyncio.sleep(.02);await peer.close()
            with pytest.raises(FlightFailure,match="Disconnected"):await second
    finally:await link.close()
