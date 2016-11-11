load('/tools/build/csharp', 'csharp_reference')

package(default_visibility = ['//visibility:public'])

csharp_reference(name = 'mscorlib', file = '//lib:mono-4.5/mscorlib.dll')
csharp_reference(name = 'System', file = '//lib:mono-4.5/System.dll')
csharp_reference(name = 'System.Core', file = '//lib:mono-4.5/System.Core.dll')
csharp_reference(name = 'System.Runtime', file = '//lib:mono-4.5/Facades/System.Runtime.dll')
csharp_reference(name = 'System.IO', file = '//lib:mono-4.5/Facades/System.IO.dll')
csharp_reference(name = 'System.Xml', file = '//lib:mono-4.5/System.Xml.dll')

csharp_reference(name = 'Google.Protobuf', file = '@csharp_protobuf//:lib/net45/Google.Protobuf.dll')
csharp_reference(name = 'Newtonsoft.Json', file = '@csharp_json//:lib/net45/Newtonsoft.Json.dll')
csharp_reference(name = 'NDesk.Options', file = '@csharp_options//:lib/NDesk.Options.dll')
csharp_reference(name = 'Moq', file = '@csharp_moq//:lib/net40/Moq.dll')
