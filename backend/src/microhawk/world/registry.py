"""Known navigation geometry only. No worker, vehicle, crack or detection positions."""
from dataclasses import dataclass

@dataclass(frozen=True)
class Target:
    position: tuple[float, float, float]
    look: tuple[float, float, float]
    alternate: tuple[float, float, float] | None = None

TARGETS = {
    "home_pad": Target((-20.,-15.,4.535),(-20.,-15.,0.)),
    "warehouse": Target((-12.,7.,4.535),(-13.,15.,3.)),
    "loading_bay_1": Target((-23.,10.,4.535),(-23.,14.,1.5)),
    "loading_bay_2": Target((-13.,10.,4.535),(-13.,14.,1.5)),
    "loading_bay_3": Target((-3.,10.,4.535),(-3.,14.,1.5)),
    "restricted_zone_a": Target((9.,0.,4.535),(9.,6.,.8),(7.,0.,4.535)),
    "security_gate_1": Target((24.,-21.,4.535),(24.,-25.,1.5)),
    "inspection_wall_1": Target((22.,-11.,4.535),(22.,-7.,2.7),(20.,-12.,4.535)),
    "utility_area": Target((23.,10.,6.535),(23.,20.,3.)),
    "navigation_course": Target((-21.,-8.,5.535),(-21.,-1.,1.)),
    "inspection_pad_1": Target((12.,-18.,4.535),(12.,-18.,0.)),
}
ZONE = {"east_min":4.,"east_max":14.,"north_min":1.5,"north_max":10.5}

def vector(values):
    return dict(zip(("east","north","up"), values))
