from typing import Literal
from pydantic import Field, model_validator
from microhawk.contracts.wire import StrictModel, Vector

class CameraFrame(StrictModel):
    type: Literal["frame"]
    schema_version: Literal[1]
    session_id: str=Field(pattern=r"^[a-zA-Z0-9_-]{1,100}$")
    request_id: str=Field(pattern=r"^[a-zA-Z0-9_-]{1,100}$")
    tick: int=Field(ge=0)
    simulation_seconds: float=Field(ge=0)
    camera_id: Literal["rgb_gimbal"]
    width: Literal[640]
    height: Literal[480]
    vertical_fov: float=Field(gt=1,lt=170)
    position: Vector
    camera_position: Vector
    forward: Vector
    right: Vector
    up: Vector
    jpeg: str=Field(max_length=800000)

    @model_validator(mode="after")
    def timestamp(self):
        if abs(self.simulation_seconds-self.tick*.02)>.001:raise ValueError("Frame timestamp inconsistent with fixed simulation tick")
        return self

class MissionEvidence(StrictModel):
    path: str
    overlay: str
    provenance: dict
    target_context: str

class MissionEvent(StrictModel):
    event_id: str
    type: Literal["RestrictedZoneDwell"]
    session_id: str
    zone: Literal["restricted_zone_a"]
    track_id: str
    entered_tick: int
    entered_time: float
    trigger_tick: int
    trigger_time: float
    dwell_seconds: float=Field(gt=0)
    confidence: float=Field(ge=0,le=1)
    evidence: list[str]=Field(min_length=1,max_length=16)
    position: Vector
