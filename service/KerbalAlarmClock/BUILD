load('/config', 'assembly_version', 'author')
load('/tools/build/csharp', 'csharp_library', 'csharp_assembly_info', 'csharp_gendarme_test')
load('/tools/ServiceDefinitions/build', 'service_definitions')
load('/service/build', 'service_deps')

filegroup(
    name = 'KerbalAlarmClock',
    srcs = [':KRPC.KerbalAlarmClock', ':ServiceDefinitions', 'CHANGES.txt'],
    visibility = ['//:__pkg__']
)

csharp_assembly_info(
    name = 'AssemblyInfo',
    title = 'KRPC.KerbalAlarmClock',
    description = 'Kerbal Alarm Clock API for kRPC',
    version = assembly_version,
    copyright = author,
    cls_compliant = False
)

srcs = [':AssemblyInfo'] + glob(['src/**/*.cs'])
deps = service_deps + ['//service/SpaceCenter:KRPC.SpaceCenter']

csharp_library(
    name = 'KRPC.KerbalAlarmClock',
    srcs = srcs,
    deps = deps,
    visibility = ['//visibility:public']
)

service_definitions(
    name = 'ServiceDefinitions',
    assemblies = [':KRPC.KerbalAlarmClock', '//service/SpaceCenter:KRPC.SpaceCenter'],
    service = 'KerbalAlarmClock',
    out = 'KRPC.KerbalAlarmClock.json',
    visibility = ['//visibility:public']
)

test_suite(
    name = 'test',
    tests = [':gendarme']
)

csharp_library(
    name = 'KRPC.KerbalAlarmClock.Debug',
    srcs = srcs,
    deps = deps,
    optimize = False,
    define = ['CODE_ANALYSIS']
)

csharp_gendarme_test(
    name = 'gendarme',
    lib = ':KRPC.KerbalAlarmClock.Debug',
    ignores = 'src/ignores.txt',
    size = 'small'
)
