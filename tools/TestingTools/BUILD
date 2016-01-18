load('/tools/build/csharp', 'csharp_library', 'csharp_assembly_info')
load('/config', 'version', 'copyright')

csharp_library(
    name = 'TestingTools',
    srcs = glob(['**/*.cs']) + [':AssemblyInfo'],
    deps = [
        '//server:kRPC',
        '//service/SpaceCenter:kRPCSpaceCenter',
        '//lib:UnityEngine', '//lib:Assembly-CSharp', '//lib:Assembly-CSharp-firstpass', '//lib:TDx.TDxInput',
        '//lib:mscorlib', '//lib:System', '//lib:System.Core', '//lib:System.Runtime',
        '//lib:System.IO', '//lib:System.Xml', '//lib:System.Xml.Linq'
    ],
    warn = 1, #TODO: relax this
    visibility = ['//visibility:public']
)

csharp_assembly_info(
    name = 'AssemblyInfo',
    title = 'TestingTools',
    version = version,
    copyright = copyright
)
