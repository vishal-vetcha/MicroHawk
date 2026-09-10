from typing import Annotated, Literal, Union
from pydantic import BaseModel, ConfigDict, Field, model_validator

class StrictModel(BaseModel):
    model_config = ConfigDict(extra="forbid", strict=True, allow_inf_nan=False)

class Vector(StrictModel):
    east: float
    north: float
    up: float

class Empty(StrictModel):
    pass

class Takeoff(StrictModel):
    altitude: float

class MoveTo(StrictModel):
    position: Vector
    speed: float | None = None

class Hold(StrictModel):
    duration: float | None = None

class Envelope(StrictModel):
    type: Literal["command"] = "command"
    schema_version: Literal[1] = 1
    session_id: str = Field(min_length=1, max_length=100)
    authority_id: str = Field(min_length=1, max_length=100)
    command_id: str = Field(pattern=r"^[a-zA-Z0-9_-]{1,64}$")
    sequence: int = Field(ge=1)
    issued_at_tick: int = Field(ge=0)
    expires_at_tick: int = Field(ge=1)

    @model_validator(mode="after")
    def expiry(self):
        if not 0 < self.expires_at_tick-self.issued_at_tick <= 1500:
            raise ValueError("Command admission lifetime must be 1..1500 ticks")
        return self

class SimpleCommand(Envelope):
    action_type: Literal["Arm", "Disarm", "Land", "ReturnHome"]
    parameters: Empty

class TakeoffCommand(Envelope):
    action_type: Literal["Takeoff"]
    parameters: Takeoff

class MoveCommand(Envelope):
    action_type: Literal["MoveTo"]
    parameters: MoveTo

class HoldCommand(Envelope):
    action_type: Literal["Hold"]
    parameters: Hold

CommandEnvelope = Annotated[Union[SimpleCommand, TakeoffCommand, MoveCommand, HoldCommand], Field(discriminator="action_type")]

class Handshake(StrictModel):
    type: Literal["handshake"]
    schema_version: Literal[1]
    session_id: str
    authority_id: str
    current_tick: int
    simulator_version: str
    world_revision: str
    coordinate_frame: Literal["facility_enu_meters"]
    capabilities: list[str]

class Outcome(StrictModel):
    type: Literal["outcome"]
    schema_version: Literal[1]
    session_id: str
    command_id: str
    tick: int
    status: Literal["Received", "Accepted", "Rejected", "Executing", "Completed", "Failed", "Interrupted"]
    reason: str

class Telemetry(StrictModel):
    type: Literal["telemetry"]
    schema_version: Literal[1]
    session_id: str
    tick: int
    simulation_seconds: float
    position: Vector
    velocity: Vector
    heading: float
    altitude: float
    battery: float
    state: str
    armed: bool
    command: str | None
    target: Vector | None
    home: Vector
    safety_status: str
    safety_reason: str
