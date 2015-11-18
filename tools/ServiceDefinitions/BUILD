load('/tools/build/dotnet/csharp', 'csharp_binary')

csharp_binary(
    name = 'ServiceDefinitions',
    srcs = glob(['**/*.cs']),
    deps = [
       '//server:kRPC',
       '//lib:Google.Protobuf',
       '//lib:UnityEngine', '//lib:Assembly-CSharp', '//lib:Assembly-CSharp-firstpass', '//lib:TDx.TDxInput',
       '//lib:mscorlib', '//lib:System', '//lib:System.Core', '//lib:System.Runtime',
       '//lib:System.IO', '//lib:System.Xml', '//lib:System.Xml.Linq',
       '//lib:Newtonsoft.Json'
    ],
    csc = '/usr/bin/mcs',
    warn = 1, #TODO: relax this
    visibility = ['//visibility:public']
)
