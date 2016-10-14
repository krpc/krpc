load('/tools/build/csharp', 'csharp_library', 'csharp_assembly_info', 'csharp_gendarme_test')
load('/config', 'assembly_version', 'author')

srcs = glob(['**/*.cs']) + [':AssemblyInfo']
deps = [
    '//server:KRPC',
    '//service/SpaceCenter:KRPC.SpaceCenter',
    '//tools/build/ksp:Google.Protobuf',
    '//tools/build/ksp:Assembly-CSharp',
    '//tools/build/ksp:Assembly-CSharp-firstpass',
    '//tools/build/ksp:UnityEngine',
    '//tools/build/ksp:UnityEngine.UI',
    '//tools/build/ksp:mscorlib',
    '//tools/build/ksp:System',
    '//tools/build/ksp:System.Core',
    '//tools/build/ksp:System.Xml'
]

csharp_library(
    name = 'TestingTools',
    srcs = srcs,
    deps = deps,
    visibility = ['//visibility:public']
)

csharp_assembly_info(
    name = 'AssemblyInfo',
    title = 'TestingTools',
    version = assembly_version,
    copyright = author,
    cls_compliant = False
)

csharp_library(
    name = 'TestingTools.Debug',
    srcs = srcs,
    deps = deps,
    optimize = False,
    define = ['CODE_ANALYSIS'],
    visibility = ['//visibility:public']
)

csharp_gendarme_test(
    name = 'test',
    lib = ':TestingTools.Debug',
    size = 'small'
)
