load('/tools/build/csharp', 'csharp_binary', 'csharp_assembly_info')
load('/config', 'version', 'copyright')

csharp_binary(
    name = 'ServiceDefinitions',
    srcs = glob(['**/*.cs']) + [':AssemblyInfo'],
    deps = [
       '//server:kRPC',
       '//tools/build/protobuf:Google.Protobuf',
       '//lib:UnityEngine', '//lib:Assembly-CSharp', '//lib:Assembly-CSharp-firstpass', '//lib:TDx.TDxInput',
       '//lib:mscorlib', '//lib:System', '//lib:System.Core', '//lib:System.Runtime',
       '//lib:System.IO', '//lib:System.Xml', '//lib:System.Xml.Linq',
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
