import asyncio
import json
from pathlib import Path
from microhawk.agents.intent import IntentAgent
from microhawk.missions.planner import MissionPlanner
async def main():
    out=Path('artifacts/integration');out.mkdir(parents=True,exist_ok=True)
    for name,text in [('wall','Inspect the structural inspection wall for cracks and return home.'),('zone','Patrol Restricted Zone A. If anyone remains inside for more than 10 seconds, investigate, capture evidence, report what happened, and return home.')]:
        intent,raw=await IntentAgent().interpret(text)
        plan=MissionPlanner().plan(text,intent)
        (out/f'{name}-intent.json').write_text(raw)
        (out/f'{name}-plan.json').write_text(plan.model_dump_json(indent=2))
        print(name,raw,flush=True)
asyncio.run(main())
