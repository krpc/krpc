load('//:config.bzl', 'assembly_version', 'author')
load('//tools/build:csharp.bzl', 'csharp_library', 'csharp_assembly_info', 'csharp_gendarme_test')
load('//tools/ServiceDefinitions:build.bzl', 'service_definitions')
load('//service:build.bzl', 'service_deps')

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
    cls_compliant = False,
    visibility = [
        '//:__pkg__' # Make visible to //:csproj-deps so it can copy AssemblyInfo.cs to generated_deps
    ]
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
    assemblies = [':KRPC.KerbalAlarmClock', '//service/SpaceCenter:KRPC.SpaceCenter', '//server:KRPC'],
    service = 'KerbalAlarmClock',
    out = 'KRPC.KerbalAlarmClock.json',
    visibility = ['//visibility:public']
)

test_suite(
    name = 'test',
    tests = [':lint']
)

test_suite(
    name = 'lint',
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
