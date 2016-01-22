load('/tools/build/csharp', 'csharp_library', 'csharp_nunit_test', 'csharp_assembly_info')
load('/tools/ServiceDefinitions/build', 'service_definitions')
load('/tools/build/image', 'png_images')
load('/config', 'version', 'copyright')

filegroup(
    name = 'server',
    srcs = [':version', 'CHANGES.txt', ':KRPC', ':ServiceDefinitions', ':icons'],
    visibility = ['//:__pkg__']
)

filegroup(
    name = 'test',
    srcs = [':KRPC.Test'],
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
        '//lib-ksp:Assembly-CSharp', '//lib-ksp:Assembly-CSharp-firstpass',
        '//lib-ksp:UnityEngine', '//lib-ksp:TDx.TDxInput',
        '//lib-ksp:mscorlib', '//lib-ksp:System', '//lib-ksp:System.Core',
        '//lib-ksp:System.Xml', '//lib-ksp:System.Xml.Linq'
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
        '//lib:Moq',
        '//tools/build/protobuf:Google.Protobuf',
        '//lib-ksp:Assembly-CSharp', '//lib-ksp:Assembly-CSharp-firstpass',
        '//lib-ksp:UnityEngine', '//lib-ksp:TDx.TDxInput',
        '//lib:mscorlib', '//lib:System', '//lib:System.Core', '//lib:System.Runtime',
        '//lib:System.IO', '//lib:System.Xml', '//lib:System.Xml.Linq'
    ],
    warn = 1, #TODO: relax this
    size = 'small'
)
