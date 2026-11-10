" the services the clients are generated for "

# The services, in the order the Python client declares them. `deps` names the services whose
# types a service uses, or whose members its documentation links to. `test` marks the service the
# client tests run against. `game` names the game a service is for, or is None for a service of
# the core, which every server has.
SERVICES = [
    struct(name = "KRPC", defs = "//core:ServiceDefinitions", deps = [], test = False, game = None),
    struct(name = "StdLib", defs = "//core:StdLibServiceDefinitions", deps = [], test = False, game = None),
    struct(name = "TestService", defs = "//tools/TestServer:ServiceDefinitions", deps = ["KRPC"], test = True, game = None),
    struct(name = "SpaceCenter", defs = "//service/SpaceCenter:ServiceDefinitions", deps = ["KRPC", "StdLib"], test = False, game = "ksp"),
    struct(name = "Drawing", defs = "//service/Drawing:ServiceDefinitions", deps = ["SpaceCenter", "StdLib", "UI"], test = False, game = "ksp"),
    struct(name = "UI", defs = "//service/UI:ServiceDefinitions", deps = ["StdLib"], test = False, game = "ksp"),
    struct(name = "InfernalRobotics", defs = "//service/InfernalRobotics:ServiceDefinitions", deps = ["SpaceCenter"], test = False, game = "ksp"),
    struct(name = "KerbalAlarmClock", defs = "//service/KerbalAlarmClock:ServiceDefinitions", deps = ["SpaceCenter"], test = False, game = "ksp"),
    struct(name = "RemoteTech", defs = "//service/RemoteTech:ServiceDefinitions", deps = ["SpaceCenter"], test = False, game = "ksp"),
    struct(name = "LIDAR", defs = "//service/LiDAR:ServiceDefinitions", deps = ["SpaceCenter"], test = False, game = "ksp"),
    struct(name = "DockingCamera", defs = "//service/DockingCamera:ServiceDefinitions", deps = ["SpaceCenter"], test = False, game = "ksp"),
    struct(name = "Debug", defs = "//service/Debug:ServiceDefinitions", deps = ["SpaceCenter"], test = False, game = "ksp"),
]

# The services only the conformance testees use, declared by clientgen_dispatch
CONFORMANCE_SERVICES = [
    struct(name = "ConformanceService", defs = "//tools/TestServer:ConformanceServiceDefinitions", deps = ["KRPC"], test = True, game = None),
    struct(name = "DeprecatedService", defs = "//tools/TestServer:DeprecatedServiceDefinitions", deps = [], test = True, game = None),
]
