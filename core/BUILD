load('//tools/build:csharp.bzl', 'csharp_library', 'csharp_nunit_test', 'csharp_gendarme_test', 'csharp_assembly_info')
load('//tools/ServiceDefinitions:build.bzl', 'service_definitions')
load('//tools/build/mono-4.5:build.bzl', 'mono_net_libs')
load('//:config.bzl', 'version', 'assembly_version', 'author')

filegroup(
    name = 'core',
    srcs = [':KRPC.Core', ':ServiceDefinitions'],
    visibility = ['//:__pkg__']
)

test_suite(
    name = 'test',
    tests = [':test-KRPC.Core', ':lint']
)

test_suite(
    name = 'lint',
    tests = [':gendarme-KRPC.Core', ':gendarme-KRPC.Core.Test']
)

genrule(
    name = 'version',
    outs = ['VERSION.txt'],
    cmd = 'echo "%s" > "$@"' % version
)

csharp_assembly_info(
    name = 'AssemblyInfo',
    title = 'KRPC.Core',
    description = 'Core server functionality for kRPC',
    copyright = author,
    version = assembly_version,
    internals_visible_to = [
        'KRPC.Core.Test',
        'DynamicProxyGenAssembly2'
    ],
    cls_compliant = False,
    visibility = [
        '//tools/ServiceDefinitions:__pkg__', # Make visible to ServiceDefinitions so it can build KRPC.Core.dll with .NET 4.5 references
        '//:__pkg__' # Make visible to //:csproj-deps so it can copy AssemblyInfo.cs to generated_deps
    ]
)

# Make sources visible to ServiceDefinitions so it can build KRPC.Core.dll with .NET 4.5 references
filegroup(
    name = 'KRPC.Core-src',
    srcs = glob(['src/**/*.cs']),
    visibility = ['//tools/ServiceDefinitions:__pkg__']
)

srcs = glob(['src/**/*.cs']) + [':AssemblyInfo', '//protobuf:csharp_unity']

deps = [
    '//tools/build/ksp:Google.Protobuf',
    '//tools/build/ksp:KRPC.IO.Ports'
] + mono_net_libs

csharp_library(
    name = 'KRPC.Core',
    srcs = srcs,
    deps = deps,
    visibility = ['//visibility:public']
)

service_definitions(
    name = 'ServiceDefinitions',
    assemblies = [':KRPC.Core'],
    service = 'KRPC',
    out = 'KRPC.Core.json',
    visibility = ['//visibility:public']
)

csharp_assembly_info(
    name = 'TestAssemblyInfo',
    title = 'KRPC.Core.Test',
    copyright = author,
    version = assembly_version,
    cls_compliant = False,
    visibility = [
        '//:__pkg__' # Make visible to krpc-genfiles so it can copy AssemblyInfo.cs to zip
    ]
)

test_deps = [
    ':KRPC.Core',
    '//tools/build/mono-4.5:Moq',
    '//tools/build/ksp:Google.Protobuf',
    '//tools/build/ksp:KRPC.IO.Ports',
    '//tools/build/mono-4.5:Newtonsoft.Json',
    '//tools/build/mono-4.5:System.Threading.Tasks.Extensions',
    '//tools/build/mono-4.5:Castle.Core'
] + mono_net_libs

csharp_library(
    name = 'KRPC.Core.Test',
    srcs = glob(['test/**/*.cs']) + [':TestAssemblyInfo'],
    deps = test_deps,
    nunit_test = True,
    optimize = False,
    define = ['CODE_ANALYSIS'],
    nowarn = ['1591', '1685'],
    visibility = ['//:__pkg__']
)

csharp_nunit_test(
    name = 'test-KRPC.Core',
    lib = ':KRPC.Core.Test',
    deps = test_deps,
    size = 'small',
    # FIXME: need to run test outside the sandbox to work around nunit file accesses
    tags = ['local']
)

csharp_library(
    name = 'KRPC.Core.Debug',
    srcs = srcs,
    deps = deps,
    optimize = False,
    define = ['CODE_ANALYSIS']
)

csharp_gendarme_test(
    name = 'gendarme-KRPC.Core',
    lib = ':KRPC.Core.Debug',
    ignores = 'src/ignores.txt',
    size = 'small'
)

csharp_gendarme_test(
    name = 'gendarme-KRPC.Core.Test',
    lib = ':KRPC.Core.Test',
    ignores = 'test/ignores.txt',
    size = 'small'
)
