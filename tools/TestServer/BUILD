load('/tools/build/csharp', 'csharp_binary', 'csharp_library', 'csharp_assembly_info')
load('/tools/build/protobuf/csharp', 'protobuf_csharp')
load('/tools/ServiceDefinitions/build', 'service_definitions')
load('/config', 'version', 'copyright')

csharp_assembly_info(
    name = 'AssemblyInfo',
    title = 'TestServer',
    version = version,
    copyright = copyright
)

csharp_binary(
    name = 'TestServer',
    srcs = glob(['**/*.cs']) + [':AssemblyInfo', '//protobuf:csharp-test'],
    deps = [
        '//server:kRPC',
        '//tools/build/protobuf:Google.Protobuf',
        '//lib:UnityEngine', '//lib:Assembly-CSharp', '//lib:Assembly-CSharp-firstpass', '//lib:TDx.TDxInput',
        '//lib:mscorlib', '//lib:System', '//lib:System.Core', '//lib:System.Runtime',
        '//lib:System.IO', '//lib:System.Xml', '//lib:System.Xml.Linq',
        '//lib:Newtonsoft.Json'
    ],
    warn = 1, #TODO: relax this
    runargs = ['50000', '50001', '--debug'],
    visibility = ['//visibility:public']
)

csharp_library(
    name = 'TestService',
    srcs = glob(['Services/*.cs', 'Properties/*.cs']) + ['//protobuf:csharp-test'],
    deps = [
        '//server:kRPC',
        '//tools/build/protobuf:Google.Protobuf',
        '//lib:UnityEngine', '//lib:Assembly-CSharp', '//lib:Assembly-CSharp-firstpass', '//lib:TDx.TDxInput',
        '//lib:mscorlib', '//lib:System', '//lib:System.Core', '//lib:System.Runtime',
        '//lib:System.IO', '//lib:System.Xml', '//lib:System.Xml.Linq'
    ],
    warn = 1, #TODO: relax this
)

service_definitions(
    name = 'ServiceDefinitions',
    assemblies = [':TestService'],
    service = 'TestService',
    out = 'TestService.json',
    visibility = ['//visibility:public']
)
