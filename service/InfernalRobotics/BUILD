load('/tools/build/csharp', 'csharp_library', 'csharp_assembly_info')
load('/tools/ServiceDefinitions/build', 'service_definitions')
load('/config', 'version', 'copyright')

filegroup(
    name = 'InfernalRobotics',
    srcs = [':kRPCInfernalRobotics', ':ServiceDefinitions', 'CHANGES.txt'],
    visibility = ['//:__pkg__']
)

csharp_assembly_info(
    name = 'AssemblyInfo',
    title = 'kRPCInfernalRobotics',
    description = 'Infernal Robotics service for kRPC',
    version = version,
    copyright = copyright
)

csharp_library(
    name = 'kRPCInfernalRobotics',
    srcs = glob(['**/*.cs']) + [':AssemblyInfo'],
    deps = [
        '//server:kRPC',
        '//service/SpaceCenter:kRPCSpaceCenter',
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
    assemblies = [':kRPCInfernalRobotics'],
    service = 'InfernalRobotics',
    out = 'kRPCInfernalRobotics.json',
    visibility = ['//visibility:public']
)
