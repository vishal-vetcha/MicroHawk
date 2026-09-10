import asyncio,json,time
from pathlib import Path
import httpx
from microhawk.config.settings import TOKEN
async def main():
    async with httpx.AsyncClient(base_url='http://127.0.0.1:8000',headers={'Authorization':'Bearer '+TOKEN},timeout=30) as client:
        before=(await client.get('/api/state')).json()['handshake']
        await client.post('/api/safety-demo',json={'kind':'disconnect'})
        deadline=time.monotonic()+20
        while time.monotonic()<deadline:
            state=(await client.get('/api/state')).json();handshake=state.get('handshake')
            if state['connected'] and handshake['authority_id']!=before['authority_id'] and state['telemetry']:break
            await asyncio.sleep(.2)
        else:raise TimeoutError('Reconnect failed')
        result=(await client.post('/api/safety-demo',json={'kind':'unsafe_altitude'})).json()
        assert 'AltitudeLimit' in result.get('unity_result',''),result
        assert handshake['session_id']==before['session_id']
        Path('artifacts/integration/final-reconnect.json').write_text(json.dumps(dict(before=before,after=handshake,validation=result),indent=2))
        print('PASS reconnect: new authority, retained physical session, fresh commands reach Unity safety')
asyncio.run(main())
