import math
import uuid
from dataclasses import dataclass
from microhawk.world.registry import ZONE
from microhawk.contracts.sensors import MissionEvent

@dataclass
class Track:
    id: str
    position: tuple
    first_seen: float
    last_seen: float
    entered_tick: int
    confidence: float
    emitted: bool = False

class DwellTracker:
    """Nearest-neighbour spatial association; short occlusions preserve but cannot trigger dwell."""
    def __init__(self, threshold=10., occlusion=2.):
        self.threshold=threshold;self.occlusion=occlusion;self.tracks=[];self.session=None;self.last_tick=-1
    def update(self, frame, detections):
        if frame["session_id"]!=self.session:
            self.tracks=[];self.session=frame["session_id"];self.last_tick=-1
        if frame["tick"]<=self.last_tick: return []
        self.last_tick=frame["tick"];now=frame["simulation_seconds"]
        self.tracks=[t for t in self.tracks if now-t.last_seen<=self.occlusion]
        events=[];used=set()
        for detection in detections:
            if detection.category!="person" or detection.position is None: continue
            p=detection.position;inside=ZONE["east_min"]<=p.east<=ZONE["east_max"] and ZONE["north_min"]<=p.north<=ZONE["north_max"]
            matches=[t for t in self.tracks if t.id not in used and math.dist(t.position,(p.east,p.north))<1.2]
            track=min(matches,key=lambda t:math.dist(t.position,(p.east,p.north))) if matches else None
            if not inside:
                if track:self.tracks.remove(track)
                continue
            if track is None:
                track=Track(uuid.uuid4().hex,(p.east,p.north),now,now,frame["tick"],detection.confidence);self.tracks.append(track)
            track.last_seen=now;track.position=(p.east,p.north);used.add(track.id)
            dwell=now-track.first_seen
            if dwell>self.threshold and not track.emitted:
                track.emitted=True
                events.append(dict(event_id=uuid.uuid4().hex,type="RestrictedZoneDwell",session_id=self.session,zone="restricted_zone_a",track_id=track.id,entered_tick=track.entered_tick,entered_time=track.first_seen,trigger_tick=frame["tick"],trigger_time=now,dwell_seconds=dwell,confidence=detection.confidence,evidence=[detection.evidence],position=p.model_dump()))
        return [MissionEvent.model_validate(e).model_dump() for e in events]
