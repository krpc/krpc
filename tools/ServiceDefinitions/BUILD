load('//tools/build:csharp.bzl', 'csharp_binary', 'csharp_library', 'csharp_assembly_info', 'csharp_gendarme_test')
load('//:config.bzl', 'assembly_version', 'author')

srcs = glob(['**/*.cs']) + [':AssemblyInfo']
deps = [
    ':KRPC',
    '//tools/build/ksp:Google.Protobuf',
    '//tools/build/ksp:Assembly-CSharp',
    '//tools/build/ksp:Assembly-CSharp-firstpass',
    '//tools/build/ksp:UnityEngine',
    '//tools/build/ksp:UnityEngine.AnimationModule',
    '//tools/build/ksp:UnityEngine.CoreModule',
    '//tools/build/ksp:UnityEngine.IMGUIModule',
    '//tools/build/ksp:UnityEngine.InputLegacyModule',
    '//tools/build/ksp:UnityEngine.UI',
    '//tools/build/ksp:UnityEngine.SharedInternalsModule',
    '//tools/build/ksp:KRPC.IO.Ports',
    '//tools/build/mono-4.5:mscorlib',
    '//tools/build/mono-4.5:System',
    '//tools/build/mono-4.5:System.Core',
    '//tools/build/mono-4.5:System.Runtime',
    '//tools/build/mono-4.5:System.IO',
    '//tools/build/mono-4.5:System.Xml',
    '//tools/build/mono-4.5:Newtonsoft.Json',
    '//tools/build/mono-4.5:NDesk.Options'
]

csharp_binary(
    name = 'ServiceDefinitions',
    srcs = srcs,
    deps = deps,
    visibility = ['//visibility:public']
)

csharp_assembly_info(
    name = 'AssemblyInfo',
    title = 'ServiceDefinitions',
    version = assembly_version,
    copyright = author,
    visibility = [
        '//:__pkg__' # Make visible to //:csproj-deps so it can copy AssemblyInfo.cs to generated_deps
    ]
)

# Build KRPC.dll using .Net 4.5 libraries as references instead of KSP libraries
# to work around issue with KSP libraries failing strong name validation
csharp_library(
    name = 'KRPC',
    srcs = ['//server:KRPC-src', '//server:AssemblyInfo', '//protobuf:csharp_3.9.1'],
    deps = [
        '//tools/build/protobuf-3.9.1:Google.Protobuf',
        '//tools/build/ksp:Assembly-CSharp',
        '//tools/build/ksp:Assembly-CSharp-firstpass',
        '//tools/build/ksp:UnityEngine',
        '//tools/build/ksp:UnityEngine.AnimationModule',
        '//tools/build/ksp:UnityEngine.CoreModule',
        '//tools/build/ksp:UnityEngine.IMGUIModule',
        '//tools/build/ksp:UnityEngine.InputLegacyModule',
        '//tools/build/ksp:UnityEngine.UI',
        '//tools/build/ksp:UnityEngine.SharedInternalsModule',
        '//tools/build/ksp:KRPC.IO.Ports',
        '//tools/build/mono-4.5:mscorlib',
        '//tools/build/mono-4.5:System',
        '//tools/build/mono-4.5:System.Core',
        '//tools/build/mono-4.5:System.Xml',
        '//tools/build/mono-4.5:System.Runtime',
        '//tools/build/mono-4.5:System.IO'
    ],
    visibility = ['//tools/krpctools:__pkg__', '//tools/TestServer:__pkg__']
)

csharp_binary(
    name = 'ServiceDefinitions.Debug',
    srcs = srcs,
    deps = deps,
    optimize = False,
    define = ['CODE_ANALYSIS'],
    visibility = ['//visibility:public']
)

csharp_gendarme_test(
    name = 'test',
    exe = ':ServiceDefinitions.Debug',
    size = 'small'
)
