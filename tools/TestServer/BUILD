load('/tools/build/csharp', 'csharp_binary', 'csharp_library', 'csharp_assembly_info')
load('/tools/build/protobuf/csharp', 'protobuf_csharp')
load('/tools/ServiceDefinitions/build', 'service_definitions')
load('/config', 'assembly_version', 'author')

csharp_assembly_info(
    name = 'AssemblyInfo',
    title = 'TestServer',
    version = assembly_version,
    copyright = author
)

csharp_binary(
    name = 'TestServer',
    srcs = glob(['**/*.cs']) + [':AssemblyInfo'],
    deps = [
        '//tools/ServiceDefinitions:KRPC',
        '//tools/build/mono-4.5:Google.Protobuf',
        '//tools/build/ksp:Assembly-CSharp',
        '//tools/build/ksp:Assembly-CSharp-firstpass',
        '//tools/build/ksp:UnityEngine',
        '//tools/build/ksp:UnityEngine.UI',
        '//tools/build/ksp:KSPUtil',
        '//tools/build/mono-4.5:mscorlib',
        '//tools/build/mono-4.5:System',
        '//tools/build/mono-4.5:System.Core',
        '//tools/build/mono-4.5:System.Runtime',
        '//tools/build/mono-4.5:System.IO',
        '//tools/build/mono-4.5:System.Xml',
        '//tools/build/mono-4.5:System.Xml.Linq',
        '//tools/build/mono-4.5:NDesk.Options'
    ],
    nowarn = ['1591'],
    visibility = ['//visibility:public']
)

csharp_library(
    name = 'TestService',
    srcs = glob(['Services/*.cs', 'Properties/*.cs']),
    deps = [
        '//tools/ServiceDefinitions:KRPC',
        '//tools/build/mono-4.5:Google.Protobuf',
        '//tools/build/ksp:Assembly-CSharp',
        '//tools/build/ksp:Assembly-CSharp-firstpass',
        '//tools/build/ksp:UnityEngine',
        '//tools/build/ksp:UnityEngine.UI',
        '//tools/build/ksp:KSPUtil',
        '//tools/build/mono-4.5:mscorlib',
        '//tools/build/mono-4.5:System',
        '//tools/build/mono-4.5:System.Core',
        '//tools/build/mono-4.5:System.Runtime',
        '//tools/build/mono-4.5:System.IO',
        '//tools/build/mono-4.5:System.Xml',
        '//tools/build/mono-4.5:System.Xml.Linq'
    ],
    nowarn = ['1591']
)

service_definitions(
    name = 'ServiceDefinitions',
    assemblies = [':TestService'],
    service = 'TestService',
    out = 'TestService.json',
    visibility = ['//visibility:public']
)
