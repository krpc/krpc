load('/tools/build/csharp', 'csharp_binary', 'csharp_library', 'csharp_assembly_info', 'csharp_gendarme_test')
load('/tools/build/protobuf/csharp', 'protobuf_csharp')
load('/tools/ServiceDefinitions/build', 'service_definitions')
load('/config', 'assembly_version', 'author')

csharp_assembly_info(
    name = 'AssemblyInfo',
    title = 'TestServer',
    version = assembly_version,
    copyright = author,
    cls_compliant = False
)

srcs = ['src/TestService.cs', ':AssemblyInfo']
deps = [
    '//tools/ServiceDefinitions:KRPC',
    '//tools/build/mono-4.5:Google.Protobuf',
    '//tools/build/ksp:Assembly-CSharp',
    '//tools/build/ksp:Assembly-CSharp-firstpass',
    '//tools/build/ksp:UnityEngine',
    '//tools/build/ksp:UnityEngine.UI',
    '//tools/build/mono-4.5:mscorlib',
    '//tools/build/mono-4.5:System',
    '//tools/build/mono-4.5:System.Core',
    '//tools/build/mono-4.5:System.Runtime',
    '//tools/build/mono-4.5:System.IO',
    '//tools/build/mono-4.5:System.Xml'
]

exe_srcs = srcs + ['src/Program.cs']
exe_deps = deps + ['//tools/build/mono-4.5:NDesk.Options']

csharp_binary(
    name = 'TestServer',
    srcs = exe_srcs,
    deps = exe_deps,
    nowarn = ['1591'],
    visibility = ['//visibility:public']
)

csharp_library(
    name = 'TestService',
    srcs = srcs,
    deps = deps,
    nowarn = ['1591']
)

service_definitions(
    name = 'ServiceDefinitions',
    assemblies = [':TestService'],
    service = 'TestService',
    out = 'TestService.json',
    visibility = ['//visibility:public']
)

csharp_binary(
    name = 'TestServer.Debug',
    srcs = exe_srcs,
    deps = exe_deps,
    optimize = False,
    nowarn = ['1591'],
    define = ['CODE_ANALYSIS'],
    visibility = ['//visibility:public']
)

csharp_gendarme_test(
    name = 'test',
    exe = ':TestServer.Debug',
    ignores = 'src/ignores.txt',
    size = 'small'
)
