" the services the clients are generated for "

# The services, in the order the Python client declares them. `deps` names the services whose
# types a service uses, or whose members its documentation links to. `test` marks the service the
# client tests run against.
SERVICES = [
    struct(name = "KRPC", defs = "//core:ServiceDefinitions", deps = [], test = False),
    struct(name = "StdLib", defs = "//core:StdLibServiceDefinitions", deps = [], test = False),
    struct(name = "TestService", defs = "//tools/TestServer:ServiceDefinitions", deps = ["KRPC"], test = True),
    struct(name = "SpaceCenter", defs = "//service/SpaceCenter:ServiceDefinitions", deps = ["KRPC", "StdLib"], test = False),
    struct(name = "Drawing", defs = "//service/Drawing:ServiceDefinitions", deps = ["SpaceCenter", "StdLib", "UI"], test = False),
    struct(name = "UI", defs = "//service/UI:ServiceDefinitions", deps = ["StdLib"], test = False),
    struct(name = "InfernalRobotics", defs = "//service/InfernalRobotics:ServiceDefinitions", deps = ["SpaceCenter"], test = False),
    struct(name = "KerbalAlarmClock", defs = "//service/KerbalAlarmClock:ServiceDefinitions", deps = ["SpaceCenter"], test = False),
    struct(name = "RemoteTech", defs = "//service/RemoteTech:ServiceDefinitions", deps = ["SpaceCenter"], test = False),
    struct(name = "LIDAR", defs = "//service/LiDAR:ServiceDefinitions", deps = ["SpaceCenter"], test = False),
    struct(name = "DockingCamera", defs = "//service/DockingCamera:ServiceDefinitions", deps = ["SpaceCenter"], test = False),
    struct(name = "Debug", defs = "//service/Debug:ServiceDefinitions", deps = ["SpaceCenter"], test = False),
]

# The services only the conformance testees use, declared by clientgen_dispatch
CONFORMANCE_SERVICES = [
    struct(name = "ConformanceService", defs = "//tools/TestServer:ConformanceServiceDefinitions", deps = ["KRPC"], test = True),
]
