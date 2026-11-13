" the services the clients are generated for "

load("//service:registry.bzl", KSP_SERVICES = "SERVICES")

# The services, in the order the Python client declares them: the core's, the test service, then
# the KSP services of the registry. `deps` names the services whose types a service uses, or whose
# members its documentation links to. `test` marks the service the client tests run against.
# `game` names the game a service is for, or is None for a service of the core, which every server
# has.
SERVICES = [
    struct(name = "KRPC", defs = "//core:ServiceDefinitions", deps = [], test = False, game = None),
    struct(name = "StdLib", defs = "//core:StdLibServiceDefinitions", deps = [], test = False, game = None),
    struct(name = "TestService", defs = "//tools/TestServer:ServiceDefinitions", deps = ["KRPC"], test = True, game = None),
] + [
    struct(
        name = s.name,
        defs = "//service/%s:ServiceDefinitions" % s.package,
        deps = s.deps,
        test = False,
        game = "ksp",
    )
    for s in KSP_SERVICES
]

# The services only the conformance testees use, declared by clientgen_dispatch
CONFORMANCE_SERVICES = [
    struct(name = "ConformanceService", defs = "//tools/TestServer:ConformanceServiceDefinitions", deps = ["KRPC"], test = True, game = None),
    struct(name = "DeprecatedService", defs = "//tools/TestServer:DeprecatedServiceDefinitions", deps = [], test = True, game = None),
]
