load('/tools/build/csharp', 'csharp_library', 'csharp_nunit_test', 'csharp_assembly_info')
load('/tools/ServiceDefinitions/build', 'service_definitions')
load('/tools/build/image', 'png_images')
load('/config', 'version', 'copyright')

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
    copyright = copyright,
    version = version,
    internals_visible_to = ['KRPC.Test', 'TestServer', 'ServiceDefinitions', 'DynamicProxyGenAssembly2']
)

csharp_library(
    name = 'KRPC',
    srcs = glob(['src/**/*.cs']) + [':AssemblyInfo', '//protobuf:csharp'],
    deps = [
        '//tools/build/protobuf:Google.Protobuf',
        '//tools/build/ksp:Assembly-CSharp',
        '//tools/build/ksp:Assembly-CSharp-firstpass',
        '//tools/build/ksp:UnityEngine',
        '//tools/build/ksp:TDx.TDxInput',
        '//tools/build/ksp:mscorlib',
        '//tools/build/ksp:System',
        '//tools/build/ksp:System.Core',
        '//tools/build/ksp:System.Xml',
        '//tools/build/ksp:System.Xml.Linq'
    ],
    warn = 1, #TODO: relax this,
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
    srcs = glob(['test/**/*.cs']) + ['//protobuf:csharp-test'],
    deps = [
        '//server:KRPC',
        '//tools/build/mono-4.5:Moq',
        '//tools/build/protobuf:Google.Protobuf',
        '//tools/build/ksp:Assembly-CSharp',
        '//tools/build/ksp:Assembly-CSharp-firstpass',
        '//tools/build/ksp:UnityEngine',
        '//tools/build/ksp:TDx.TDxInput',
        '//tools/build/mono-4.5:mscorlib',
        '//tools/build/mono-4.5:System',
        '//tools/build/mono-4.5:System.Core',
        '//tools/build/mono-4.5:System.Runtime',
        '//tools/build/mono-4.5:System.IO',
        '//tools/build/mono-4.5:System.Xml',
        '//tools/build/mono-4.5:System.Xml.Linq'
    ],
    warn = 1, #TODO: relax this
    size = 'small'
)
