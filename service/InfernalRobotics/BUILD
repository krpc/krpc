load('/tools/build/dotnet/csharp', 'csharp_library')
load('/tools/ServiceDefinitions/build', 'service_definitions')

package(default_visibility = ['//visibility:public'])

csharp_library(
    name = 'kRPCInfernalRobotics',
    srcs = glob(['**/*.cs']),
    deps = [
        '//server:kRPC',
        '//service/SpaceCenter:kRPCSpaceCenter',
        '//lib:Google.Protobuf',
        '//lib:UnityEngine', '//lib:Assembly-CSharp', '//lib:Assembly-CSharp-firstpass', '//lib:TDx.TDxInput',
        '//lib:mscorlib', '//lib:System', '//lib:System.Core', '//lib:System.Runtime',
        '//lib:System.IO', '//lib:System.Xml', '//lib:System.Xml.Linq'
    ],
    warn = 1, #TODO: relax this
)

service_definitions(
    name = 'ServiceDefinitions',
    assemblies = [':kRPCInfernalRobotics'],
    service = 'InfernalRobotics',
    out = 'kRPCInfernalRobotics.json'
)
