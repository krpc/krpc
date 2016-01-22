load('/tools/build/csharp', 'csharp_library', 'csharp_assembly_info')
load('/config', 'version', 'copyright')

csharp_library(
    name = 'TestingTools',
    srcs = glob(['**/*.cs']) + [':AssemblyInfo'],
    deps = [
        '//server:KRPC',
        '//service/SpaceCenter:KRPC.SpaceCenter',
        '//lib-ksp:Assembly-CSharp', '//lib-ksp:Assembly-CSharp-firstpass',
        '//lib-ksp:UnityEngine', '//lib-ksp:TDx.TDxInput',
        '//lib-ksp:mscorlib', '//lib-ksp:System', '//lib-ksp:System.Core',
        '//lib-ksp:System.Xml', '//lib-ksp:System.Xml.Linq'
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
