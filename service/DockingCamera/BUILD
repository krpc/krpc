load('//:config.bzl', 'assembly_version', 'author')
load('//tools/build:csharp.bzl', 'csharp_library', 'csharp_assembly_info', 'csharp_gendarme_test')
load('//tools/build:python.bzl', 'py_lint_test')
load('//tools/ServiceDefinitions:build.bzl', 'service_definitions')
load('//service:build.bzl', 'service_deps')

filegroup(
    name = 'DockingCamera',
    srcs = [':KRPC.DockingCamera', ':ServiceDefinitions', 'CHANGES.txt'],
    visibility = ['//:__pkg__']
)

csharp_assembly_info(
    name = 'AssemblyInfo',
    title = 'KRPC.DockingCamera',
    description = 'DockingCamera API for kRPC',
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
    name = 'KRPC.DockingCamera',
    srcs = srcs,
    deps = deps,
    visibility = ['//visibility:public']
)

service_definitions(
    name = 'ServiceDefinitions',
    assemblies = [':KRPC.DockingCamera', '//service/SpaceCenter:KRPC.SpaceCenter'],
    service = 'DockingCamera',
    out = 'KRPC.DockingCamera.json',
    visibility = ['//visibility:public']
)

test_suite(
    name = 'test',
    tests = [':lint']
)

test_suite(
    name = 'lint',
    tests = [
        # ':pylint',
        ':gendarme'
    ]
)

# FIXME: add tests
# py_lint_test(
#     name = 'pylint',
#     srcs = glob(['test/*.py']),
#     pylint_config = 'test/pylint.rc',
#     deps = ['//client/python', '//tools/krpctest'],
#     size = 'small'
# )

csharp_library(
    name = 'KRPC.DockingCamera.Debug',
    srcs = srcs,
    deps = deps,
    optimize = False,
    define = ['CODE_ANALYSIS']
)

csharp_gendarme_test(
    name = 'gendarme',
    lib = ':KRPC.DockingCamera.Debug',
    ignores = 'src/ignores.txt',
    size = 'small'
)
