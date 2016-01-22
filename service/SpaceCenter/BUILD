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
    assemblies = [':KRPC.SpaceCenter'],
    service = 'SpaceCenter',
    out = 'KRPC.SpaceCenter.json',
    visibility = ['//visibility:public']
)
