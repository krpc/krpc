load('/tools/build/dotnet/csharp', 'csharp_library')

csharp_library(
    name = 'TestingTools',
    srcs = glob(['**/*.cs']),
    deps = [
        '//server:kRPC', '//services/kRPCSpaceCenter:kRPCSpaceCenter',
        '//lib:UnityEngine', '//lib:Assembly-CSharp', '//lib:Assembly-CSharp-firstpass', '//lib:TDx.TDxInput',
        '//lib:mscorlib', '//lib:System', '//lib:System.Core', '//lib:System.Runtime',
        '//lib:System.IO', '//lib:System.Xml', '//lib:System.Xml.Linq'
    ],
    csc = '/usr/bin/mcs',
    warn = 1, #TODO: relax this
    visibility = ['//visibility:public']
)
