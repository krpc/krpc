load('/tools/build/csharp', 'csharp_library', 'csharp_assembly_info')
load('/tools/ServiceDefinitions/build', 'service_definitions')
load('/config', 'version', 'copyright')

filegroup(
    name = 'KerbalAlarmClock',
    srcs = [':kRPCKerbalAlarmClock', ':ServiceDefinitions', 'CHANGES.txt'],
    visibility = ['//:__pkg__']
)

csharp_assembly_info(
    name = 'AssemblyInfo',
    title = 'kRPCKerbalAlarmClock',
    description = 'Kerbal Alarm Clock service for kRPC',
    version = version,
    copyright = copyright
)

csharp_library(
    name = 'kRPCKerbalAlarmClock',
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
    assemblies = ['//service/SpaceCenter:kRPCSpaceCenter', ':kRPCKerbalAlarmClock'],
    service = 'KerbalAlarmClock',
    out = 'kRPCKerbalAlarmClock.json',
    visibility = ['//visibility:public']
)
