"""Persistent endpoint with explicit outcomes, no sleep-based flight completion."""
import asyncio
import json
import uuid
import time
from collections import deque
from pydantic import TypeAdapter
from websockets.asyncio.server import serve
from microhawk.contracts.wire import CommandEnvelope, Handshake, Outcome, Telemetry

class FlightFailure(RuntimeError):
    def __init__(self, outcome):
        self.outcome = outcome
        super().__init__(f"{outcome.get('status')}: {outcome.get('reason')}")

class SimulatorLink:
    def __init__(self, token):
        self.token = token
        self.socket = None
        self.handshake = None
        self.telemetry = None
        self.sequence = 0
        self.pending = {}
        self.frames = {}
        self.events = deque(maxlen=2000)
        self.changed = asyncio.Condition()
        self.server = None
        self.last_telemetry = time.monotonic()

    @property
    def connected(self):
        return self.socket is not None and self.handshake is not None

    async def start(self):
        self.server = await serve(self._connection, "127.0.0.1", 8765, max_size=800_000, max_queue=16)

    async def close(self):
        if self.server:
            self.server.close()
            await self.server.wait_closed()

    def _fail_pending(self, reason):
        for future in list(self.pending.values())+list(self.frames.values()):
            if not future.done():
                future.set_exception(FlightFailure({"status":"Interrupted", "reason":reason}))
        self.pending.clear()
        self.frames.clear()

    async def _connection(self, socket):
        if socket.request.headers.get("Authorization") != f"Bearer {self.token}" or self.socket is not None:
            await socket.close(1008, "Authority denied")
            return
        self.socket = socket
        self.last_telemetry = time.monotonic()
        async def heartbeat():
            while True:
                if time.monotonic()-self.last_telemetry>5:
                    await socket.close(1011,"Telemetry timeout")
                    return
                await socket.send('{"type":"heartbeat"}')
                await asyncio.sleep(1)  # Network liveness only, never mission completion.
        beat = asyncio.create_task(heartbeat())
        try:
            async for raw in socket:
                message = json.loads(raw)
                kind = message.get("type")
                if kind == "handshake":
                    value = Handshake.model_validate(message).model_dump()
                    if value["world_revision"] != "industrial-test-v1":
                        raise ValueError("Unsupported world revision")
                    self._fail_pending("SessionChanged")
                    self.handshake = value
                    self.telemetry = None
                    self.sequence = 0
                elif kind == "telemetry":
                    value = Telemetry.model_validate(message).model_dump()
                    if not self.handshake or value["session_id"] != self.handshake["session_id"]:
                        continue
                    self.telemetry = value
                    self.last_telemetry = time.monotonic()
                    async with self.changed:
                        self.changed.notify_all()
                elif kind == "outcome":
                    value = Outcome.model_validate(message).model_dump()
                    self.events.append(value)
                    future = self.pending.get(value["command_id"])
                    if future and not future.done() and value["status"] in {"Completed","Rejected","Failed","Interrupted"}:
                        if value["status"] == "Completed": future.set_result(value)
                        else: future.set_exception(FlightFailure(value))
                elif kind == "frame":
                    future = self.frames.get(message.get("request_id"))
                    if future and not future.done() and message.get("session_id") == self.handshake["session_id"]:
                        future.set_result(message)
        finally:
            beat.cancel()
            await asyncio.gather(beat, return_exceptions=True)
            self._fail_pending("Disconnected")
            self.socket = self.handshake = None
            async with self.changed: self.changed.notify_all()

    async def command(self, action, parameters=None, timeout=240):
        if not self.connected or not self.telemetry: raise RuntimeError("Simulator disconnected")
        self.sequence += 1
        tick = self.telemetry["tick"]
        data = dict(type="command", schema_version=1, session_id=self.handshake["session_id"], authority_id=self.handshake["authority_id"], command_id=uuid.uuid4().hex, sequence=self.sequence, issued_at_tick=tick, expires_at_tick=tick+500, action_type=action, parameters=parameters or {})
        payload = TypeAdapter(CommandEnvelope).validate_python(data).model_dump()
        future = asyncio.get_running_loop().create_future()
        self.pending[data["command_id"]] = future
        self.events.append({"type":"requested", **payload})
        try:
            await self.socket.send(json.dumps(payload, allow_nan=False))
            return await asyncio.wait_for(future, timeout)
        finally:
            self.pending.pop(data["command_id"], None)

    async def wait_tick(self, tick, timeout=15):
        async with self.changed:
            await asyncio.wait_for(self.changed.wait_for(lambda: not self.connected or (self.telemetry and self.telemetry["tick"] >= tick)), timeout)
        if not self.connected: raise RuntimeError("Simulator disconnected")
        return self.telemetry

    async def capture(self, look):
        if not self.connected: raise RuntimeError("Simulator disconnected")
        request = uuid.uuid4().hex
        future = asyncio.get_running_loop().create_future()
        self.frames[request] = future
        try:
            await self.socket.send(json.dumps(dict(type="capture", session_id=self.handshake["session_id"], authority_id=self.handshake["authority_id"], request_id=request, look_at=look)))
            return await asyncio.wait_for(future, 15)
        finally: self.frames.pop(request, None)
