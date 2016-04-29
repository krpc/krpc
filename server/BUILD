load('/tools/build/csharp', 'csharp_library', 'csharp_nunit_test', 'csharp_assembly_info')
load('/tools/ServiceDefinitions/build', 'service_definitions')
load('/tools/build/image', 'png_images')
load('/config', 'version', 'ksp_version', 'assembly_version', 'author')

filegroup(
    name = 'server',
    srcs = [':version', 'CHANGES.txt', ':KRPC', ':ServiceDefinitions', ':icons'],
    visibility = ['//:__pkg__']
)

test_suite(
    name = 'test',
    tests = [':KRPC.Test'],
    visibility = ['//:__pkg__']
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
    custom = {'KSPVersion': ksp_version},
    internals_visible_to = ['KRPC.Test', 'TestServer', 'ServiceDefinitions', 'DynamicProxyGenAssembly2'],
    visibility = ['//tools/ServiceDefinitions:__pkg__'] # Make visible to ServiceDefinitions so it can build KRPC.dll with .NET 4.5 references
)

# Make sources visible to ServiceDefinitions so it can build KRPC.dll with .NET 4.5 references
filegroup(
    name = 'KRPC-src',
    srcs = glob(['src/**/*.cs']),
    visibility = ['//tools/ServiceDefinitions:__pkg__']
)

csharp_library(
    name = 'KRPC',
    srcs = [':KRPC-src', ':AssemblyInfo', '//protobuf:csharp'],
    deps = [
        '//tools/build/ksp:Google.Protobuf',
        '//tools/build/ksp:Assembly-CSharp',
        '//tools/build/ksp:Assembly-CSharp-firstpass',
        '//tools/build/ksp:UnityEngine',
        '//tools/build/ksp:UnityEngine.UI',
        '//tools/build/ksp:KSPUtil',
        '//tools/build/ksp:mscorlib',
        '//tools/build/ksp:System',
        '//tools/build/ksp:System.Core',
        '//tools/build/ksp:System.Xml',
        '//tools/build/ksp:System.Xml.Linq'
    ],
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

csharp_nunit_test(
    name = 'KRPC.Test',
    srcs = glob(['test/**/*.cs']),
    deps = [
        '//server:KRPC',
        '//tools/build/mono-4.5:Moq',
        '//tools/build/ksp:Google.Protobuf',
        '//tools/build/ksp:Assembly-CSharp',
        '//tools/build/ksp:Assembly-CSharp-firstpass',
        '//tools/build/ksp:UnityEngine',
        '//tools/build/ksp:UnityEngine.UI',
        '//tools/build/ksp:KSPUtil',
        '//tools/build/mono-4.5:mscorlib',
        '//tools/build/mono-4.5:System',
        '//tools/build/mono-4.5:System.Core',
        '//tools/build/mono-4.5:System.Runtime',
        '//tools/build/mono-4.5:System.IO',
        '//tools/build/mono-4.5:System.Xml',
        '//tools/build/mono-4.5:System.Xml.Linq'
    ],
    nowarn = ['1591'],
    size = 'small'
)
