load('/tools/build/csharp', 'csharp_binary', 'csharp_assembly_info')
load('/config', 'version', 'copyright')

csharp_binary(
    name = 'ServiceDefinitions',
    srcs = glob(['**/*.cs']) + [':AssemblyInfo'],
    deps = [
        '//server:KRPC',
        '//tools/build/protobuf:Google.Protobuf',
        '//lib-ksp:Assembly-CSharp', '//lib-ksp:Assembly-CSharp-firstpass',
        '//lib-ksp:UnityEngine', '//lib-ksp:TDx.TDxInput',
        '//lib:mscorlib', '//lib:System', '//lib:System.Core',
        '//lib:System.Xml', '//lib:System.Xml.Linq',
        '//lib:Newtonsoft.Json'
    ],
    warn = 1, #TODO: relax this
    visibility = ['//visibility:public']
)

csharp_assembly_info(
    name = 'AssemblyInfo',
    title = 'ServiceDefinitions',
    version = version,
    copyright = copyright
)
