load('/tools/build/csharp', 'csharp_reference')

package(default_visibility = ['//visibility:public'])

csharp_reference(name = 'mscorlib', file = ':mono-4.5/mscorlib.dll')
csharp_reference(name = 'System', file = ':mono-4.5/System.dll')
csharp_reference(name = 'System.Core', file = ':mono-4.5/System.Core.dll')
csharp_reference(name = 'System.Runtime', file = ':mono-4.5/Facades/System.Runtime.dll')
csharp_reference(name = 'System.IO', file = ':mono-4.5/Facades/System.IO.dll')
csharp_reference(name = 'System.Xml', file = ':mono-4.5/System.Xml.dll')
csharp_reference(name = 'System.Xml.Linq', file = ':mono-4.5/System.Xml.Linq.dll')

csharp_reference(name = 'Newtonsoft.Json', file = '@csharp.json//:Bin/Net40/Newtonsoft.Json.dll')
csharp_reference(name = 'Moq', file = '@csharp.moq//:lib/net40/Moq.dll')
