load('//:config.bzl', 'assembly_version', 'author')
load('//tools/build:csharp.bzl', 'csharp_library', 'csharp_assembly_info', 'csharp_gendarme_test')
load('//tools/build:python.bzl', 'py_lint_test')
load('//tools/ServiceDefinitions:build.bzl', 'service_definitions')
load('//service:build.bzl', 'service_deps')

filegroup(
    name = 'SpaceCenter',
    srcs = [':KRPC.SpaceCenter', ':ServiceDefinitions',
            'CHANGES.txt', 'LICENSE',
            'src/module-manager.cfg', '@module_manager//file'],
    visibility = ['//:__pkg__']
)

csharp_assembly_info(
    name = 'AssemblyInfo',
    title = 'KRPC.SpaceCenter',
    description = 'Basic service for kRPC',
    version = assembly_version,
    copyright = author,
    cls_compliant = False,
    internals_visible_to = ['TestingTools'],
    visibility = [
        '//:__pkg__' # Make visible to //:csproj-deps so it can copy AssemblyInfo.cs to generated_deps
    ]
)

srcs = [':AssemblyInfo'] + glob(['src/**/*.cs'])
deps = service_deps + ['//tools/build/ksp:UnityEngine.ScreenCaptureModule']

csharp_library(
    name = 'KRPC.SpaceCenter',
    srcs = srcs,
    deps = deps,
    visibility = ['//visibility:public']
)

service_definitions(
    name = 'ServiceDefinitions',
    assemblies = [':KRPC.SpaceCenter'],
    service = 'SpaceCenter',
    out = 'KRPC.SpaceCenter.json',
    visibility = ['//visibility:public']
)

test_suite(
    name = 'test',
    tests = [':lint']
)

test_suite(
    name = 'lint',
    tests = [':pylint', ':gendarme']
)

py_lint_test(
    name = 'pylint',
    srcs = glob(['test/*.py']),
    pylint_config = 'test/pylint.rc',
    deps = ['//client/python', '//tools/krpctest'],
    size = 'medium'
)

csharp_library(
    name = 'KRPC.SpaceCenter.Debug',
    srcs = srcs,
    deps = deps,
    optimize = False,
    define = ['CODE_ANALYSIS']
)

csharp_gendarme_test(
    name = 'gendarme',
    lib = ':KRPC.SpaceCenter.Debug',
    ignores = 'src/ignores.txt',
    size = 'small'
)
