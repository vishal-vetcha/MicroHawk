from typing import Literal
from pydantic import Field, model_validator
from microhawk.contracts.wire import StrictModel
from microhawk.world.registry import TARGETS

class TargetReference(StrictModel):
    id: str
    @model_validator(mode="after")
    def known(self):
        if self.id not in TARGETS: raise ValueError("Unknown navigation target")
        return self

class ConditionalRule(StrictModel):
    event: Literal["RestrictedZoneDwell"] = "RestrictedZoneDwell"
    dwell_seconds: float = Field(default=10., ge=1, le=120)
    investigate: bool = True
    capture_evidence: bool = True
    return_home: bool = True

class MissionPolicy(StrictModel):
    return_home: bool = True
    max_replans: int = Field(default=2, ge=0, le=3)
    watch_seconds: float = Field(default=30., ge=12, le=180)
    conditional: ConditionalRule | None = None

class MissionRequest(StrictModel):
    operation: Literal["InspectTarget", "WatchZone", "VisitTarget"]
    target: TargetReference
    perception: Literal["damage", "person", "vehicle", "scene"]
    policy: MissionPolicy

class OperatorIntent(StrictModel):
    kind: Literal["MISSION", "STATUS", "RETURN_HOME", "ABORT", "EXPLAIN"]
    mission: MissionRequest | None = None
    @model_validator(mode="after")
    def consistent(self):
        if (self.kind=="MISSION") != (self.mission is not None): raise ValueError("Mission payload mismatch")
        if self.mission and self.mission.operation=="WatchZone" and self.mission.target.id!="restricted_zone_a": raise ValueError("Only Zone A geometry configured")
        if self.mission and self.mission.policy.conditional and self.mission.operation!="WatchZone": raise ValueError("A dwell conditional requires WatchZone, never InspectTarget. For wall inspection set conditional to null.")
        if self.mission and self.mission.operation=="WatchZone" and self.mission.perception!="person": raise ValueError("WatchZone requires person observations")
        return self

class MissionAction(StrictModel):
    kind: Literal["Arm","Takeoff","MoveTo","Hold","ReturnHome","ObserveArea","CaptureEvidence","WatchZone","Investigate"]
    target: str | None = None
    parameters: dict = Field(default_factory=dict)

class Mission(StrictModel):
    id: str
    request: str
    intent: OperatorIntent
    actions: list[MissionAction] = Field(max_length=32)
    revision: int = 0
