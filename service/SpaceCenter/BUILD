load('/tools/build/csharp', 'csharp_library')
load('/tools/ServiceDefinitions/build', 'service_definitions')

filegroup(
    name = 'SpaceCenter',
    srcs = [':kRPCSpaceCenter', ':ServiceDefinitions', 'LICENSE'],
    visibility = ['//:__pkg__']
)

csharp_library(
    name = 'kRPCSpaceCenter',
    srcs = glob(['**/*.cs']),
    deps = [
        '//server:kRPC',
        '//tools/build/protobuf:Google.Protobuf',
        '//lib:UnityEngine', '//lib:Assembly-CSharp', '//lib:Assembly-CSharp-firstpass', '//lib:TDx.TDxInput',
        '//lib:mscorlib', '//lib:System', '//lib:System.Core', '//lib:System.Runtime',
        '//lib:System.IO', '//lib:System.Xml', '//lib:System.Xml.Linq'
    ],
    warn = 1, #TODO: relax this
    visibility = ['//visibility:public']
)

service_definitions(
    name = 'ServiceDefinitions',
    assemblies = [':kRPCSpaceCenter'],
    service = 'SpaceCenter',
    out = 'kRPCSpaceCenter.json',
    visibility = ['//visibility:public']
)
