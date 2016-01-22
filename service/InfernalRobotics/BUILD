load('/tools/build/csharp', 'csharp_library', 'csharp_assembly_info')
load('/tools/ServiceDefinitions/build', 'service_definitions')
load('/config', 'version', 'copyright')

filegroup(
    name = 'InfernalRobotics',
    srcs = [':KRPC.InfernalRobotics', ':ServiceDefinitions', 'CHANGES.txt'],
    visibility = ['//:__pkg__']
)

csharp_assembly_info(
    name = 'AssemblyInfo',
    title = 'KRPC.InfernalRobotics',
    description = 'Infernal Robotics service for kRPC',
    version = version,
    copyright = copyright
)

csharp_library(
    name = 'KRPC.InfernalRobotics',
    srcs = glob(['**/*.cs']) + [':AssemblyInfo'],
    deps = [
        '//server:KRPC',
        '//service/SpaceCenter:KRPC.SpaceCenter',
        '//tools/build/protobuf:Google.Protobuf',
        '//lib-ksp:Assembly-CSharp', '//lib-ksp:Assembly-CSharp-firstpass',
        '//lib-ksp:UnityEngine', '//lib-ksp:TDx.TDxInput',
        '//lib-ksp:mscorlib', '//lib-ksp:System', '//lib-ksp:System.Core',
        '//lib-ksp:System.Xml', '//lib-ksp:System.Xml.Linq'
    ],
    warn = 1, #TODO: relax this
    visibility = ['//visibility:public']
)

service_definitions(
    name = 'ServiceDefinitions',
    assemblies = [':KRPC.InfernalRobotics'],
    service = 'InfernalRobotics',
    out = 'KRPC.InfernalRobotics.json',
    visibility = ['//visibility:public']
)
