import json
from pathlib import Path
root=Path('artifacts/integration');dest=Path('docs/media');dest.mkdir(parents=True,exist_ok=True)
for name in ['wall','zone']:
    record=json.loads((root/f'final-{name}.json').read_text())
    e=record['evidence'][-1]
    (dest/f'{name}-evidence.jpg').write_bytes(Path(e['overlay']).read_bytes())
    (Path('artifacts/visuals')/f'{name}-evidence.jpg').write_bytes(Path(e['overlay']).read_bytes())
    (root/f'{name}-report.txt').write_text(record['report'])
    print(name,record['id'],record['final_telemetry']['tick'],record['final_telemetry']['battery'],len(record['detections']),len(record['events']))
(dest/'dashboard.png').write_bytes(Path('artifacts/visuals/dashboard-live.png').read_bytes())
(dest/'flight.png').write_bytes(Path('artifacts/visuals/flight-inspection.png').read_bytes())
