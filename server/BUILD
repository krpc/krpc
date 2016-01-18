load('/tools/build/csharp', 'csharp_library', 'csharp_nunit_test')
load('/tools/ServiceDefinitions/build', 'service_definitions')
load('/tools/build/inkscape', 'png_images')
load('/version', 'version')

filegroup(
    name = 'server',
    srcs = [':version', 'CHANGES.txt', ':kRPC', ':ServiceDefinitions', ':icons'],
    visibility = ['//:__pkg__']
)

genrule(
    name = 'version',
    outs = ['VERSION.txt'],
    cmd = 'echo "%s" > "$@"' % version
)

csharp_library(
    name = 'kRPC',
    srcs = glob(['src/**/*.cs']) + ['//protobuf:csharp'],
    deps = [
        '//tools/build/protobuf:Google.Protobuf',
        '//lib:UnityEngine', '//lib:Assembly-CSharp', '//lib:Assembly-CSharp-firstpass', '//lib:TDx.TDxInput',
        '//lib:mscorlib', '//lib:System', '//lib:System.Core', '//lib:System.Runtime',
        '//lib:System.IO', '//lib:System.Xml', '//lib:System.Xml.Linq'
    ],
    warn = 1, #TODO: relax this,
    visibility = ['//visibility:public']
)

service_definitions(
    name = 'ServiceDefinitions',
    assemblies = [':kRPC'],
    service = 'KRPC',
    out = 'kRPC.json',
    visibility = ['//visibility:public']
)

png_images(
    name = 'icons',
    srcs = glob(['src/icons/*.svg'])
)

csharp_nunit_test(
    name = 'kRPCTest',
    srcs = glob(['test/**/*.cs']) + ['//protobuf:csharp-test'],
    deps = [
        '//server:kRPC',
        '//lib:Moq',
        '//tools/build/protobuf:Google.Protobuf',
        '//lib:UnityEngine', '//lib:Assembly-CSharp', '//lib:Assembly-CSharp-firstpass', '//lib:TDx.TDxInput',
        '//lib:mscorlib', '//lib:System', '//lib:System.Core', '//lib:System.Runtime',
        '//lib:System.IO', '//lib:System.Xml', '//lib:System.Xml.Linq'
    ],
    warn = 1 #TODO: relax this
)
