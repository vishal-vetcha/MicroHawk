"""Run and assert real API/LLM/Unity demonstrations; requires run-demo.ps1 first."""
import argparse,asyncio,json,time
from pathlib import Path
import httpx
from microhawk.config.settings import TOKEN,ROOT
REQUESTS={"zone":"Patrol Restricted Zone A. If anyone remains inside for more than 10 seconds, investigate, capture evidence, report what happened, and return home.","wall":"Inspect the structural inspection wall for cracks and return home."}
async def main(scenarios):
    output=ROOT/'artifacts/integration';output.mkdir(parents=True,exist_ok=True)
    async with httpx.AsyncClient(base_url='http://127.0.0.1:8000',timeout=45,headers={'Authorization':'Bearer '+TOKEN}) as client:
        for scenario in scenarios:
            if scenario=='safety':
                r=await client.post('/api/safety-demo',json={'kind':'unsafe_altitude'});r.raise_for_status();record=r.json()
                assert 'AltitudeLimit' in record.get('unity_result',''),record
                state=(await client.get('/api/state')).json();assert state['telemetry']['position']['up']<.6
                record['final_telemetry']=state['telemetry'];(output/'final-safety.json').write_text(json.dumps(record,indent=2));print('PASS safety: Unity AltitudeLimit rejection, no takeoff',flush=True);continue
            before=(await client.get('/api/state')).json();previous=(before.get('mission') or {}).get('id')
            response=await client.post('/api/command',json={'text':REQUESTS[scenario]});response.raise_for_status()
            deadline=time.monotonic()+600
            while time.monotonic()<deadline:
                state=(await client.get('/api/state')).json();record=state.get('mission') or {}
                if state.get('error'):raise RuntimeError(state['error'])
                if record.get('id')!=previous and record.get('status') in {'Completed','Failed','Interrupted'}:break
                await asyncio.sleep(1) # Observer polling only; completion is the actual terminal record.
            else:raise TimeoutError('Integrated mission did not terminate')
            assert record['status']=='Completed',record.get('failure')
            assert record['final_telemetry']['state']=='Landed'
            assert record['evidence'] and record['raw_intent']
            if scenario=='zone':
                assert len(record['events'])==1,record['events']
                assert record['events'][0]['dwell_seconds']>10 and record['revisions']
                assert any(x['reason']=='Touchdown' for x in record['outcomes'])
                assert any(d['category']=='person' for d in record['detections'])
            else:assert any(d['category']=='damage' for d in record['detections'])
            (output/f'final-{scenario}.json').write_text(json.dumps(record,indent=2))
            print(f"PASS {scenario}: mission={record['id']} state=Landed battery={record['final_telemetry']['battery']:.3f} evidence={len(record['evidence'])}",flush=True)
if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('scenarios',nargs='*',default=['wall','zone','safety']);args=parser.parse_args();asyncio.run(main(args.scenarios))
