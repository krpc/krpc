try:
    from krpc.services.krpc import KRPC
    from krpc.services.testservice import TestService
    from krpc.services.spacecenter import SpaceCenter
    from krpc.services.drawing import Drawing
    from krpc.services.ui import UI
    from krpc.services.infernalrobotics import InfernalRobotics
    from krpc.services.kerbalalarmclock import KerbalAlarmClock
    from krpc.services.remotetech import RemoteTech
    from krpc.services.lidar import LiDAR
    from krpc.services.dockingcamera import DockingCamera
except ImportError as exn:
    KRPC = lambda _: None
    TestService = lambda _: None
    SpaceCenter = lambda _: None
    Drawing = lambda _: None
    UI = lambda _: None
    InfernalRobotics = lambda _: None
    KerbalAlarmClock = lambda _: None
    RemoteTech = lambda _: None
    LiDAR = lambda _: None
    DockingCamera = lambda _: None


class Client:
    def __init__(self) -> None:
        self.krpc = KRPC(self)
        self.test_service = TestService(self)
        self.space_center = SpaceCenter(self)
        self.drawing = Drawing(self)
        self.ui = UI(self)
        self.infernal_robotics = InfernalRobotics(self)
        self.kerbal_alarm_clock = KerbalAlarmClock(self)
        self.remote_tech = RemoteTech(self)
        self.lidar = LiDAR(self)
        self.docking_camera = DockingCamera(self)

        self._services = {
            "KRPC": self.krpc,
            "TestService": self.test_service,
            "SpaceCenter": self.space_center,
            "Drawing": self.drawing,
            "UI": self.ui,
            "InfernalRobotics": self.infernal_robotics,
            "KerbalAlarmClock": self.kerbal_alarm_clock,
            "RemoteTech": self.remote_tech,
            "LiDAR": self.lidar,
            "DockingCamera": self.docking_camera
        }
