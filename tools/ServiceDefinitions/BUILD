load('/tools/build/csharp', 'csharp_binary', 'csharp_library', 'csharp_assembly_info')
load('/config', 'version', 'author')

csharp_binary(
    name = 'ServiceDefinitions',
    srcs = glob(['**/*.cs']) + [':AssemblyInfo'],
    deps = [
        ':KRPC',
        '//tools/build/ksp:Google.Protobuf',
        '//tools/build/ksp:Assembly-CSharp',
        '//tools/build/ksp:Assembly-CSharp-firstpass',
        '//tools/build/ksp:UnityEngine',
        '//tools/build/ksp:TDx.TDxInput',
        '//tools/build/mono-4.5:mscorlib',
        '//tools/build/mono-4.5:System',
        '//tools/build/mono-4.5:System.Core',
        '//tools/build/mono-4.5:System.Runtime',
        '//tools/build/mono-4.5:System.IO',
        '//tools/build/mono-4.5:System.Xml',
        '//tools/build/mono-4.5:System.Xml.Linq',
        '//tools/build/mono-4.5:Newtonsoft.Json',
        '//tools/build/mono-4.5:NDesk.Options'
    ],
    visibility = ['//visibility:public']
)

csharp_assembly_info(
    name = 'AssemblyInfo',
    title = 'ServiceDefinitions',
    version = version,
    copyright = author
)

# Build KRPC.dll using .Net 4.5 libraries as references instead of KSP libraries
# to work around issue with KSP libraries failing strong name validation
csharp_library(
    name = 'KRPC',
    srcs = ['//server:KRPC-src', '//server:AssemblyInfo', '//protobuf:csharp'],
    deps = [
        '//tools/build/mono-4.5:Google.Protobuf',
        '//tools/build/ksp:Assembly-CSharp',
        '//tools/build/ksp:Assembly-CSharp-firstpass',
        '//tools/build/ksp:UnityEngine',
        '//tools/build/ksp:TDx.TDxInput',
        '//tools/build/mono-4.5:mscorlib',
        '//tools/build/mono-4.5:System',
        '//tools/build/mono-4.5:System.Core',
        '//tools/build/mono-4.5:System.Xml',
        '//tools/build/mono-4.5:System.Xml.Linq',
        '//tools/build/mono-4.5:System.Runtime',
        '//tools/build/mono-4.5:System.IO'
    ],
    visibility = ['//tools/clientgen:__pkg__', '//tools/TestServer:__pkg__']
)
