load('/tools/build/dotnet/csharp', 'csharp_library')
load('/tools/build/dotnet/genproto', 'proto_csharp_library')
load('/tools/ServiceDefinitions/build', 'service_definitions')
load('/tools/build/inkscape', 'png_images')

proto_csharp_library(
    name = 'csharp.proto',
    src = '//protobuf:krpc.proto'
)

csharp_library(
    name = 'kRPC',
    srcs = glob(['src/kRPC/**/*.cs']) + [':csharp.proto'],
    deps = [
        '//lib:Google.Protobuf',
        '//lib:UnityEngine', '//lib:Assembly-CSharp', '//lib:Assembly-CSharp-firstpass', '//lib:TDx.TDxInput',
        '//lib:mscorlib', '//lib:System', '//lib:System.Core', '//lib:System.Runtime',
        '//lib:System.IO', '//lib:System.Xml', '//lib:System.Xml.Linq'
    ],
    warn = 1, #TODO: relax this
    visibility = ['//visibility:public']
)

png_images(
    name = 'icons',
    srcs = glob(['src/icons/*.svg']),
    visibility = ['//visibility:public']
)

service_definitions(
    name = 'ServiceDefinitions',
    assemblies = [':kRPC'],
    service = 'KRPC',
    out = 'kRPC.json',
    visibility = ['//visibility:public']
)
