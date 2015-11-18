load('/tools/build/dotnet/csharp', 'csharp_nunit_test')
load('/tools/build/dotnet/genproto', 'proto_csharp_library')

proto_csharp_library(
    name = 'proto',
    src = '//protobuf:test.proto'
)

csharp_nunit_test(
    name = 'kRPCTest',
    srcs = glob(['**/*.cs']) + [':proto'],
    deps = [
        '//server:kRPC',
        '//lib:Moq',
        '//lib:Google.Protobuf',
        '//lib:UnityEngine', '//lib:Assembly-CSharp', '//lib:Assembly-CSharp-firstpass', '//lib:TDx.TDxInput',
        '//lib:mscorlib', '//lib:System', '//lib:System.Core', '//lib:System.Runtime',
        '//lib:System.IO', '//lib:System.Xml', '//lib:System.Xml.Linq'
    ],
    csc = '/usr/bin/mcs',
    warn = 1, #TODO: relax this
    visibility = ['//visibility:public'],
)
