import json
import pytest
from pydantic import ValidationError
from microhawk.missions.models import OperatorIntent
from microhawk.missions.planner import MissionPlanner
from microhawk.events.dwell import DwellTracker
from microhawk.perception.images import Detection
from microhawk.contracts.wire import Vector


def intent(operation="WatchZone",target="restricted_zone_a",conditional=True):
    return {"kind":"MISSION","mission":{"operation":operation,"target":{"id":target},"perception":"person" if operation=="WatchZone" else "damage","policy":{"return_home":True,"conditional":{"dwell_seconds":10.} if conditional else None}}}

@pytest.mark.parametrize("target",["unknown","worker_2","crack_1","__import__('os')"])
def test_unknown_navigation_target_rejected(target):
    with pytest.raises(ValidationError):OperatorIntent.model_validate(intent(target=target))

def test_semantic_conditional_must_watch_zone():
    with pytest.raises(ValidationError):OperatorIntent.model_validate(intent("InspectTarget","inspection_wall_1"))

def test_plan_uses_typed_actions_and_rth():
    parsed=OperatorIntent.model_validate(intent())
    plan=MissionPlanner().plan("watch",parsed)
    assert [a.kind for a in plan.actions]==["Arm","Takeoff","MoveTo","Hold","WatchZone","ReturnHome"]

def test_no_model_force_payload():
    payload=intent();payload["mission"]["forces"]=[100,0,0]
    with pytest.raises(ValidationError):OperatorIntent.model_validate(payload)

def detection(east=9.,north=6.):
    return Detection(detection_id="d",category="person",confidence=.8,session_id="s",tick=0,method="test_fixture",bbox=[1,2,10,30],evidence="fixture.jpg",position=Vector(east=east,north=north,up=1.15))

def frame(time,session="s"):
    return dict(session_id=session,tick=round(time*50),simulation_seconds=float(time))

def test_dwell_strictly_greater_than_ten_and_once():
    tracker=DwellTracker();events=[]
    for second in range(12):
        current=tracker.update(frame(second),[detection()]);events+=current
        if second<=10:assert not current
    assert len(events)==1 and events[0]["dwell_seconds"]==11
    assert tracker.update(frame(12),[detection()])==[]

def test_short_occlusion_retains_but_does_not_emit():
    t=DwellTracker(1.)
    t.update(frame(0),[detection()]);assert t.update(frame(1),[])==[]
    assert len(t.update(frame(1.5),[detection()]))==1

def test_long_occlusion_and_session_reset_dwell():
    t=DwellTracker(1.);t.update(frame(0),[detection()]);assert t.update(frame(3),[detection()])==[]
    assert t.update(frame(4,"new"),[detection()])==[]

def test_outside_zone_and_duplicate_tick_never_trigger():
    t=DwellTracker(1.)
    for second in range(10):assert t.update(frame(second),[detection(20.)])==[]
    assert t.update(frame(9),[detection()])==[]
