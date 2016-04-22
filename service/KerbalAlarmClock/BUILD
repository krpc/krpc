load('/tools/build/csharp', 'csharp_library', 'csharp_assembly_info')
load('/tools/ServiceDefinitions/build', 'service_definitions')
load('/config', 'assembly_version', 'author')

filegroup(
    name = 'KerbalAlarmClock',
    srcs = [':KRPC.KerbalAlarmClock', ':ServiceDefinitions', 'CHANGES.txt'],
    visibility = ['//:__pkg__']
)

csharp_assembly_info(
    name = 'AssemblyInfo',
    title = 'KRPC.KerbalAlarmClock',
    description = 'Kerbal Alarm Clock service for kRPC',
    version = assembly_version,
    copyright = author
)

csharp_library(
    name = 'KRPC.KerbalAlarmClock',
    srcs = glob(['src/**/*.cs']) + [':AssemblyInfo'],
    deps = [
        '//server:KRPC',
        '//service/SpaceCenter:KRPC.SpaceCenter',
        '//tools/build/ksp:Google.Protobuf',
        '//tools/build/ksp:Assembly-CSharp',
        '//tools/build/ksp:Assembly-CSharp-firstpass',
        '//tools/build/ksp:UnityEngine',
        '//tools/build/ksp:UnityEngine.UI',
        '//tools/build/ksp:KSPUtil',
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
    assemblies = ['//service/SpaceCenter:KRPC.SpaceCenter', ':KRPC.KerbalAlarmClock'],
    service = 'KerbalAlarmClock',
    out = 'KRPC.KerbalAlarmClock.json',
    visibility = ['//visibility:public']
)
