load('/tools/build/csharp', 'csharp_library', 'csharp_assembly_info')
load('/tools/ServiceDefinitions/build', 'service_definitions')
load('/config', 'version', 'copyright')

filegroup(
    name = 'KerbalAlarmClock',
    srcs = [':KRPC.KerbalAlarmClock', ':ServiceDefinitions', 'CHANGES.txt'],
    visibility = ['//:__pkg__']
)

csharp_assembly_info(
    name = 'AssemblyInfo',
    title = 'KRPC.KerbalAlarmClock',
    description = 'Kerbal Alarm Clock service for kRPC',
    version = version,
    copyright = copyright
)

csharp_library(
    name = 'KRPC.KerbalAlarmClock',
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
    assemblies = ['//service/SpaceCenter:KRPC.SpaceCenter', ':KRPC.KerbalAlarmClock'],
    service = 'KerbalAlarmClock',
    out = 'KRPC.KerbalAlarmClock.json',
    visibility = ['//visibility:public']
)
