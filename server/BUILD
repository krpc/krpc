load('//tools/build:csharp.bzl', 'csharp_library', 'csharp_nunit_test', 'csharp_gendarme_test', 'csharp_assembly_info')
load('//tools/ServiceDefinitions:build.bzl', 'service_definitions')
load('//tools/build:image.bzl', 'png_images')
load('//:config.bzl', 'version', 'ksp_version_max', 'ksp_version_min', 'assembly_version', 'author')

filegroup(
    name = 'server',
    srcs = [':version', 'CHANGES.txt', ':KRPC', ':ServiceDefinitions', ':icons'],
    visibility = ['//:__pkg__']
)

test_suite(
    name = 'test',
    tests = [':test-KRPC', ':lint']
)

test_suite(
    name = 'ci-test',
    tests = [':test-KRPC']
)

test_suite(
    name = 'lint',
    tests = [':gendarme-KRPC', ':gendarme-KRPC.Test']
)

genrule(
    name = 'version',
    outs = ['VERSION.txt'],
    cmd = 'echo "%s" > "$@"' % version
)

csharp_assembly_info(
    name = 'AssemblyInfo',
    title = 'KRPC',
    description = 'RPC server for Kerbal Space Program',
    copyright = author,
    version = assembly_version,
    custom = {'AssemblyKSPVersion': '"%s", "%s"' % (ksp_version_max, ksp_version_min)},
    internals_visible_to = [
        'KRPC.Test',
        'TestServer',
        'TestServer.Debug',
        'ServiceDefinitions',
        'ServiceDefinitions.Debug',
        'DynamicProxyGenAssembly2'
    ],
    cls_compliant = False,
    visibility = ['//tools/ServiceDefinitions:__pkg__'] # Make visible to ServiceDefinitions so it can build KRPC.dll with .NET 4.5 references
)

# Make sources visible to ServiceDefinitions so it can build KRPC.dll with .NET 4.5 references
filegroup(
    name = 'KRPC-src',
    srcs = glob(['src/**/*.cs']),
    visibility = ['//tools/ServiceDefinitions:__pkg__']
)

srcs = [':KRPC-src', ':AssemblyInfo', '//protobuf:csharp']

deps = [
    '//tools/build/ksp:Google.Protobuf',
    '//tools/build/ksp:Assembly-CSharp',
    '//tools/build/ksp:Assembly-CSharp-firstpass',
    '//tools/build/ksp:UnityEngine',
    '//tools/build/ksp:UnityEngine.AnimationModule',
    '//tools/build/ksp:UnityEngine.CoreModule',
    '//tools/build/ksp:UnityEngine.IMGUIModule',
    '//tools/build/ksp:UnityEngine.InputLegacyModule',
    '//tools/build/ksp:UnityEngine.UI',
    '//tools/build/ksp:mscorlib',
    '//tools/build/ksp:System',
    '//tools/build/ksp:System.Core',
    '//tools/build/ksp:System.Xml',
    '//tools/build/ksp:KRPC.IO.Ports'
]

csharp_library(
    name = 'KRPC',
    srcs = srcs,
    deps = deps,
    visibility = ['//visibility:public']
)

service_definitions(
    name = 'ServiceDefinitions',
    assemblies = [':KRPC'],
    service = 'KRPC',
    out = 'KRPC.json',
    visibility = ['//visibility:public']
)

png_images(
    name = 'icons',
    srcs = glob(['src/icons/*.svg'])
)

csharp_assembly_info(
    name = 'TestAssemblyInfo',
    title = 'KRPC.Test',
    copyright = author,
    version = assembly_version,
    cls_compliant = False
)

test_deps = [
    '//server:KRPC',
    '//tools/build/mono-4.5:Moq',
    '//tools/build/ksp:Google.Protobuf',
    '//tools/build/ksp:Assembly-CSharp',
    '//tools/build/ksp:Assembly-CSharp-firstpass',
    '//tools/build/ksp:UnityEngine',
    '//tools/build/ksp:UnityEngine.UI',
    '//tools/build/ksp:KRPC.IO.Ports',
    '//tools/build/mono-4.5:mscorlib',
    '//tools/build/mono-4.5:System',
    '//tools/build/mono-4.5:System.Core',
    '//tools/build/mono-4.5:System.Runtime',
    '//tools/build/mono-4.5:System.IO',
    '//tools/build/mono-4.5:System.Xml',
    '//tools/build/mono-4.5:Newtonsoft.Json'
]

csharp_library(
    name = 'KRPC.Test',
    srcs = glob(['test/**/*.cs']) + [':TestAssemblyInfo'],
    deps = test_deps,
    nunit_test = True,
    optimize = False,
    define = ['CODE_ANALYSIS'],
    nowarn = ['1591'],
    visibility = ['//:__pkg__']
)

csharp_nunit_test(
    name = 'test-KRPC',
    lib = ':KRPC.Test',
    deps = test_deps,
    size = 'small',
    # FIXME: need to run test outside the sandbox to work around nunit file accesses
    tags = ['local']
)

csharp_library(
    name = 'KRPC.Debug',
    srcs = srcs,
    deps = deps,
    optimize = False,
    define = ['CODE_ANALYSIS']
)

csharp_gendarme_test(
    name = 'gendarme-KRPC',
    lib = ':KRPC.Debug',
    ignores = 'src/ignores.txt',
    size = 'small'
)

csharp_gendarme_test(
    name = 'gendarme-KRPC.Test',
    lib = ':KRPC.Test',
    ignores = 'test/ignores.txt',
    size = 'small'
)
