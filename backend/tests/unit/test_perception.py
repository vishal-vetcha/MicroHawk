import base64,json
from pathlib import Path
from microhawk.perception.images import ImagePerception
from microhawk.events.dwell import DwellTracker
ROOT=Path(__file__).parents[1]/"fixtures"
def load(name):
    frame=json.loads((ROOT/f"{name}.json").read_text());frame["jpeg"]=base64.b64encode((ROOT/f"{name}.jpg").read_bytes()).decode();return frame

def test_rendered_zone_detects_workers_not_hatching_or_crate(tmp_path):
    frame=load("zone");detections,_,_=ImagePerception(tmp_path).process(frame,"test","person")
    assert len(detections)==2
    in_zone=[d for d in detections if 4<d.position.east<14 and 1.5<d.position.north<10.5]
    assert len(in_zone)==1 and abs(in_zone[0].position.east-9)<.3 and abs(in_zone[0].position.north-6)<.4
    assert all(d.bbox[1]<300 for d in detections)

def test_rendered_wall_contains_pixel_derived_crack_candidate(tmp_path):
    detections,_,_=ImagePerception(tmp_path).process(load("wall"),"test","damage")
    damage=[d for d in detections if d.category=="damage"]
    assert len(damage)==1 and 220<damage[0].bbox[0]<270

def test_blank_sensor_never_invents_person_or_damage(tmp_path):
    import cv2,numpy as np
    frame=load("zone");frame["jpeg"]=base64.b64encode(cv2.imencode('.jpg',np.full((480,640,3),128,np.uint8))[1]).decode()
    detections,_,_=ImagePerception(tmp_path).process(frame,"test","damage")
    assert detections==[]
