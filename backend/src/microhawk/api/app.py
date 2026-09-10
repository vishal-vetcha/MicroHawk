import asyncio
from contextlib import asynccontextmanager
from pathlib import Path
from fastapi import FastAPI, Header, HTTPException
from fastapi.responses import FileResponse
from fastapi.staticfiles import StaticFiles
from pydantic import Field
from microhawk.config.settings import DATA, TOKEN
from microhawk.contracts.wire import StrictModel
from microhawk.agents.intent import IntentAgent
from microhawk.memory.store import MissionMemory
from microhawk.missions.executor import MissionExecutor
from microhawk.missions.planner import MissionPlanner
from microhawk.reporting.grounded import explain
from microhawk.transport.link import SimulatorLink

link=SimulatorLink(TOKEN)
memory=MissionMemory(DATA/"missions")
executor=MissionExecutor(link,memory)
agent=IntentAgent()
last_error=None
operation=None

@asynccontextmanager
async def lifespan(app):
    await link.start()
    yield
    if executor.task and not executor.task.done():
        executor.task.cancel();await asyncio.gather(executor.task,return_exceptions=True)
    await link.close()

app=FastAPI(title="MicroHawk Command Center",lifespan=lifespan)
static=Path(__file__).parent/"static"
app.mount("/static",StaticFiles(directory=static),name="static")

class TextRequest(StrictModel):
    text: str=Field(min_length=1,max_length=2000)

class ControlRequest(StrictModel):
    action: str

class SafetyDemo(StrictModel):
    kind: str

def authorize(value):
    if value!=f"Bearer {TOKEN}":raise HTTPException(401,"Operator authority required")

@app.get("/")
def index():return FileResponse(static/"index.html")

@app.get("/api/state")
async def state():
    record=executor.current or memory.last()
    public_record=None
    if record:
        public_record={k:v for k,v in record.items() if k not in {"telemetry","protocol_events"}}
    return dict(connected=link.connected,handshake=link.handshake,telemetry=link.telemetry,mission=public_record,events=list(link.events)[-80:],error=last_error,busy=operation is not None and not operation.done(),image=executor.latest_image)

@app.get("/api/evidence")
def evidence():
    if not executor.latest_image:raise HTTPException(404,"No captured evidence")
    return FileResponse(executor.latest_image)

async def intervene(action):
    if executor.task and not executor.task.done():
        executor.task.cancel();await asyncio.gather(executor.task,return_exceptions=True)
    params={"duration":1.} if action=="Hold" else {}
    return await link.command(action,params)

async def process(text):
    global last_error
    last_error=None
    try:
        intent,raw=await agent.interpret(text)
        if intent.kind=="MISSION":
            plan=MissionPlanner().plan(text,intent)
            executor.task=asyncio.create_task(executor.execute(plan,raw))
            await executor.task
        elif intent.kind in {"RETURN_HOME","ABORT"}:await intervene("ReturnHome" if intent.kind=="RETURN_HOME" else "Hold")
        elif intent.kind=="EXPLAIN":last_error=explain(executor.current or memory.last(),list(link.events))
        else:last_error=f"Current flight state: {(link.telemetry or {}).get('state','disconnected')}"
    except asyncio.CancelledError:raise
    except Exception as error:last_error=str(error)

@app.post("/api/command",status_code=202)
async def command(request:TextRequest,authorization:str|None=Header(default=None)):
    global operation
    authorize(authorization)
    if operation and not operation.done():raise HTTPException(409,"Busy; use Return Home or Hold to interrupt")
    operation=asyncio.create_task(process(request.text))
    return {"status":"Interpreting"}

@app.post("/api/control")
async def control(request:ControlRequest,authorization:str|None=Header(default=None)):
    authorize(authorization)
    if request.action not in {"ReturnHome","Hold"}:raise HTTPException(400,"Unsupported control")
    if operation and not operation.done():operation.cancel();await asyncio.gather(operation,return_exceptions=True)
    try:return await intervene(request.action)
    except Exception as error:raise HTTPException(409,str(error))

@app.get("/api/explain")
async def explanation():return {"explanation":explain(executor.current or memory.last(),list(link.events))}

@app.post("/api/safety-demo")
async def safety_demo(request:SafetyDemo,authorization:str|None=Header(default=None)):
    authorize(authorization)
    if operation and not operation.done():raise HTTPException(409,"Mission busy")
    try:
        if request.kind=="unsafe_altitude":
            await link.command("Arm")
            try:await link.command("Takeoff",{"altitude":100.})
            except Exception as rejected:
                await link.command("Disarm")
                return {"requested":{"action":"Takeoff","altitude":100},"unity_result":str(rejected),"safe_state":link.telemetry}
        elif request.kind=="blocked_target":
            return await link.command("MoveTo",{"position":{"east":-13.,"north":21.,"up":4.535}})
        elif request.kind=="disconnect":
            if link.socket:await link.socket.close()
            return {"result":"Connection closed; Unity connection-loss policy applies"}
        else:raise HTTPException(400,"Unknown scenario")
    except Exception as error:return {"unity_result":str(error),"telemetry":link.telemetry}


@app.post("/api/voice")
async def voice(authorization:str|None=Header(default=None)):
    authorize(authorization)
    from microhawk.voice.local import listen
    try:return await listen()
    except Exception as error:raise HTTPException(409,str(error))
