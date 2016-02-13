load('/tools/build/csharp', 'csharp_library', 'csharp_assembly_info')
load('/tools/ServiceDefinitions/build', 'service_definitions')
load('/config', 'version', 'author')

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
    copyright = author
)

csharp_library(
    name = 'KRPC.InfernalRobotics',
    srcs = glob(['src/**/*.cs']) + [':AssemblyInfo'],
    deps = [
        '//server:KRPC',
        '//tools/build/ksp:Google.Protobuf',
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
    visibility = ['//visibility:public']
)

service_definitions(
    name = 'ServiceDefinitions',
    assemblies = [':KRPC.InfernalRobotics'],
    service = 'InfernalRobotics',
    out = 'KRPC.InfernalRobotics.json',
    visibility = ['//visibility:public']
)
