from __future__ import annotations

from typing import TYPE_CHECKING

if TYPE_CHECKING:
    from krpc.services.debug import Debug
    from krpc.services.dockingcamera import DockingCamera
    from krpc.services.drawing import Drawing
    from krpc.services.infernalrobotics import InfernalRobotics
    from krpc.services.kerbalalarmclock import KerbalAlarmClock
    from krpc.services.krpc import KRPC
    from krpc.services.lidar import LIDAR
    from krpc.services.remotetech import RemoteTech
    from krpc.services.spacecenter import SpaceCenter
    from krpc.services.stdlib import StdLib
    from krpc.services.testservice import TestService
    from krpc.services.ui import UI
else:
    try:
        from krpc.services.debug import Debug
        from krpc.services.dockingcamera import DockingCamera
        from krpc.services.drawing import Drawing
        from krpc.services.infernalrobotics import InfernalRobotics
        from krpc.services.kerbalalarmclock import KerbalAlarmClock
        from krpc.services.krpc import KRPC
        from krpc.services.lidar import LIDAR
        from krpc.services.remotetech import RemoteTech
        from krpc.services.spacecenter import SpaceCenter
        from krpc.services.stdlib import StdLib
        from krpc.services.testservice import TestService
        from krpc.services.ui import UI
    except ImportError:
        # Without the generated stubs, every service attribute is None

        def _unavailable(_: object) -> None:
            return None

        KRPC = _unavailable
        StdLib = _unavailable
        TestService = _unavailable
        SpaceCenter = _unavailable
        Drawing = _unavailable
        UI = _unavailable
        InfernalRobotics = _unavailable
        KerbalAlarmClock = _unavailable
        RemoteTech = _unavailable
        LIDAR = _unavailable
        DockingCamera = _unavailable
        Debug = _unavailable


class Client:
    def __init__(self) -> None:
        self.krpc: KRPC = KRPC(self)
        self.std_lib: StdLib = StdLib(self)
        self.test_service: TestService = TestService(self)
        self.space_center: SpaceCenter = SpaceCenter(self)
        self.drawing: Drawing = Drawing(self)
        self.ui: UI = UI(self)
        self.infernal_robotics: InfernalRobotics = InfernalRobotics(self)
        self.kerbal_alarm_clock: KerbalAlarmClock = KerbalAlarmClock(self)
        self.remote_tech: RemoteTech = RemoteTech(self)
        self.lidar: LIDAR = LIDAR(self)
        self.docking_camera: DockingCamera = DockingCamera(self)
        self.debug: Debug = Debug(self)

        self._services = {
            "KRPC": self.krpc,
            "StdLib": self.std_lib,
            "TestService": self.test_service,
            "SpaceCenter": self.space_center,
            "Drawing": self.drawing,
            "UI": self.ui,
            "InfernalRobotics": self.infernal_robotics,
            "KerbalAlarmClock": self.kerbal_alarm_clock,
            "RemoteTech": self.remote_tech,
            "LIDAR": self.lidar,
            "DockingCamera": self.docking_camera,
            "Debug": self.debug,
        }
