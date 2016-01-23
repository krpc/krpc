load('/tools/build/csharp', 'csharp_library', 'csharp_assembly_info')
load('/tools/ServiceDefinitions/build', 'service_definitions')
load('/config', 'version', 'copyright')

filegroup(
    name = 'SpaceCenter',
    srcs = [':KRPC.SpaceCenter', ':ServiceDefinitions', 'LICENSE', 'CHANGES.txt'],
    visibility = ['//:__pkg__']
)

csharp_assembly_info(
    name = 'AssemblyInfo',
    title = 'KRPC.SpaceCenter',
    description = 'Basic services for kRPC',
    version = version,
    copyright = copyright,
    internals_visible_to = ['TestingTools']
)

csharp_library(
    name = 'KRPC.SpaceCenter',
    srcs = glob(['**/*.cs']) + [':AssemblyInfo'],
    deps = [
        '//server:KRPC',
        '//tools/build/protobuf:Google.Protobuf',
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
    assemblies = [':KRPC.SpaceCenter'],
    service = 'SpaceCenter',
    out = 'KRPC.SpaceCenter.json',
    visibility = ['//visibility:public']
)
