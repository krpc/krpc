load('/config', 'assembly_version', 'author')
load('/tools/build/csharp', 'csharp_library', 'csharp_assembly_info', 'csharp_gendarme_test')
load('/tools/build/python', 'py_lint_test')
load('/tools/ServiceDefinitions/build', 'service_definitions')
load('/service/build', 'service_deps')

filegroup(
    name = 'Drawing',
    srcs = [':KRPC.Drawing', ':ServiceDefinitions', 'CHANGES.txt'],
    visibility = ['//:__pkg__']
)

csharp_assembly_info(
    name = 'AssemblyInfo',
    title = 'KRPC.Drawing',
    description = 'Drawing API for kRPC',
    version = assembly_version,
    copyright = author,
    cls_compliant = False
)

srcs = [':AssemblyInfo'] + glob(['src/**/*.cs'])
deps = service_deps + ['//service/SpaceCenter:KRPC.SpaceCenter', '//service/UI:KRPC.UI']

csharp_library(
    name = 'KRPC.Drawing',
    srcs = srcs,
    deps = deps,
    visibility = ['//visibility:public']
)

service_definitions(
    name = 'ServiceDefinitions',
    assemblies = [':KRPC.Drawing', '//service/UI:KRPC.UI', '//service/SpaceCenter:KRPC.SpaceCenter'],
    service = 'Drawing',
    out = 'KRPC.Drawing.json',
    visibility = ['//visibility:public']
)

test_suite(
    name = 'test',
    tests = [':lint', ':gendarme']
)

py_lint_test(
    name = 'lint',
    srcs = glob(['test/*.py']),
    pylint_config = 'test/pylint.rc',
    deps = ['//client/python', '//tools/krpctest'],
    size = 'small'
)

csharp_library(
    name = 'KRPC.Drawing.Debug',
    srcs = srcs,
    deps = deps,
    optimize = False,
    define = ['CODE_ANALYSIS']
)

csharp_gendarme_test(
    name = 'gendarme',
    lib = ':KRPC.Drawing.Debug',
    ignores = 'src/ignores.txt',
    size = 'small'
)
