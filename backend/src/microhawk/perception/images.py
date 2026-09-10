"""Simulation-calibrated visual candidates from RGB only; no scene labels."""
import base64
import math
import uuid
from pathlib import Path
from typing import Literal
import cv2
import numpy as np
from pydantic import Field
from microhawk.contracts.wire import StrictModel, Vector
from microhawk.contracts.sensors import CameraFrame

class Detection(StrictModel):
    detection_id: str
    category: Literal["person", "vehicle", "damage", "inspection_marker"]
    confidence: float = Field(ge=0,le=1)
    session_id: str
    tick: int
    source: Literal["camera_perception"] = "camera_perception"
    method: str
    bbox: list[int] = Field(min_length=4,max_length=4)
    evidence: str
    position: Vector | None = None

class ImagePerception:
    def __init__(self, root):
        self.root=Path(root);self.root.mkdir(parents=True,exist_ok=True)

    @staticmethod
    def project(frame, x, y, height=1.15):
        def v(key): return np.array([frame[key][a] for a in ("east","north","up")],dtype=float)
        scale=math.tan(math.radians(frame["vertical_fov"])/2)
        ray=v("forward")+v("right")*((2*x/frame["width"]-1)*scale*frame["width"]/frame["height"])+v("up")*((1-2*y/frame["height"])*scale)
        origin=v("camera_position")
        if abs(ray[2])<.05: return None
        distance=(height-origin[2])/ray[2]
        if not 0<distance<50: return None
        pos=origin+ray*distance
        return Vector(east=float(pos[0]),north=float(pos[1]),up=height)

    def process(self, frame, mission_id, category="scene"):
        frame=CameraFrame.model_validate(frame).model_dump()
        raw=base64.b64decode(frame["jpeg"],validate=True)
        if len(raw)>600_000: raise ValueError("Oversized RGB image")
        image=cv2.imdecode(np.frombuffer(raw,np.uint8),cv2.IMREAD_COLOR)
        if image is None or image.shape[:2]!=(480,640): raise ValueError("Invalid sensor image")
        folder=self.root/mission_id;folder.mkdir(exist_ok=True)
        path=folder/f"{frame['session_id']}_{frame['tick']}.jpg";path.write_bytes(raw)
        hsv=cv2.cvtColor(image,cv2.COLOR_BGR2HSV);gray=cv2.cvtColor(image,cv2.COLOR_BGR2GRAY)
        detections=[]
        def add(category, box, confidence, method, position=None):
            detections.append(Detection(detection_id=uuid.uuid4().hex,category=category,confidence=confidence,session_id=frame["session_id"],tick=frame["tick"],bbox=[int(x) for x in box],method=method,evidence=str(path),position=position))
        # High-visibility orange torso/arms. Appearance and proportions are calibrated to authored mannequins.
        orange=cv2.inRange(hsv,np.array([4,150,170]),np.array([10,255,255]))
        orange=cv2.morphologyEx(orange,cv2.MORPH_CLOSE,np.ones((5,5),np.uint8))
        contours,_=cv2.findContours(orange,cv2.RETR_EXTERNAL,cv2.CHAIN_APPROX_SIMPLE)
        for c in contours:
            x,y,w,h=cv2.boundingRect(c)
            if 12<w<150 and 10<h<150 and .45<h/w<2.5 and cv2.contourArea(c)>80:
                hat=hsv[max(0,y-int(h*.9)):y+max(1,int(h*.1)),max(0,x):min(640,x+w)]
                yellow=cv2.inRange(hat,np.array([17,100,130]),np.array([35,255,255])) if hat.size else np.zeros((1,1),np.uint8)
                if np.count_nonzero(yellow)<max(5,w*h*.025): continue
                pos=self.project(frame,x+w/2,y+h/2)
                add("person",(x,y,w,h),.78,"high_visibility_mannequin_rgb",pos)
        # Thin branched dark components on a lighter, low-saturation wall surface.
        dark=cv2.inRange(gray,0,72)
        contours,_=cv2.findContours(dark,cv2.RETR_EXTERNAL,cv2.CHAIN_APPROX_SIMPLE)
        for c in contours if category=="damage" else []:
            x,y,w,h=cv2.boundingRect(c);area=cv2.contourArea(c)
            if 8<w<180 and 35<h<300 and h/w>1.25 and .015<area/(w*h)<.4:
                surround=gray[max(0,y-3):min(480,y+h+3),max(0,x-3):min(640,x+w+3)]
                if np.median(surround)>90:
                    add("damage",(x,y,w,h),.72,"thin_branched_dark_surface_candidate")
        # Numbered-sign candidates: dark rectangular plates with light glyph components.
        if category=="damage":
            for c in contours:
                x,y,w,h=cv2.boundingRect(c)
                if 25<w<240 and 12<h<90 and 1.8<w/h<5 and cv2.contourArea(c)/(w*h)>.55:
                    patch=gray[y:y+h,x:x+w];light=(patch>190).astype(np.uint8)
                    count,_,stats,_=cv2.connectedComponentsWithStats(light)
                    glyphs=[r for r in stats[1:] if r[4]>5 and r[3]>h*.2]
                    if 2<=len(glyphs)<=8:add("inspection_marker",(x,y,w,h),.7,"dark_marker_plate_with_light_glyphs")
        if category=="vehicle":
            # Simulation-specific candidates: white service body + teal livery, or yellow forklift body + dark mast/wheels.
            masks=[(cv2.inRange(hsv,np.array([0,0,175]),np.array([179,65,255])),"white_service_vehicle_candidate"),
                   (cv2.inRange(hsv,np.array([17,140,140]),np.array([35,255,255])),"yellow_industrial_vehicle_candidate")]
            for mask,method in masks:
                mask=cv2.morphologyEx(mask,cv2.MORPH_CLOSE,np.ones((5,5),np.uint8))
                parts,_=cv2.findContours(mask,cv2.RETR_EXTERNAL,cv2.CHAIN_APPROX_SIMPLE)
                for c in parts:
                    x,y,w,h=cv2.boundingRect(c)
                    if 35<w<300 and 25<h<200 and .8<w/h<3.5 and cv2.contourArea(c)/(w*h)>.4:
                        patch=hsv[max(0,y-10):min(480,y+h+20),max(0,x-5):min(640,x+w+5)]
                        dark_fraction=np.mean(patch[:,:,2]<85)
                        teal_fraction=np.mean((patch[:,:,0]>70)&(patch[:,:,0]<105)&(patch[:,:,1]>70))
                        if dark_fraction>.08 and (method.startswith("yellow") or teal_fraction>.025):add("vehicle",(x,y,w,h),.65,method)
        annotated=image.copy()
        for d in detections:
            x,y,w,h=d.bbox;cv2.rectangle(annotated,(x,y),(x+w,y+h),(80,220,90),2)
            cv2.putText(annotated,d.category,(x,max(12,y-5)),cv2.FONT_HERSHEY_SIMPLEX,.4,(80,220,90),1)
        overlay=path.with_name(path.stem+"_overlay.jpg");cv2.imwrite(str(overlay),annotated)
        return detections, str(path), str(overlay)
