load('/tools/build/csharp', 'csharp_library', 'csharp_nunit_test', 'csharp_assembly_info', 'nuget_package')
load('/tools/build/client_test', 'client_test')
load('/tools/clientgen/build', 'clientgen_csharp')
load('/config', 'version', 'author')

filegroup(
    name = 'csharp',
    srcs = [':KRPC.Client', 'CHANGES.txt'],
    visibility = ['//visibility:public']
)

nuget_package(
    name = 'nuget',
    assembly = ':KRPC.Client',
    id = 'KRPC.Client',
    version = version+'-beta1', #TODO: remove beta version number when Google.Protobuf is released
    author = author,
    description = 'C# client for kRPC',
    project_url = 'https://github.com/krpc/krpc',
    license_url = 'https://github.com/krpc/krpc/blob/master/LICENSE',
    deps = {
        'Google.Protobuf': '3.0.0-beta2'
    }
)

csharp_assembly_info(
    name = 'AssemblyInfo',
    title = 'KRPC.Client',
    description = 'C# client for kRPC',
    copyright = author,
    version = version,
    internals_visible_to = ['KRPC.Client.Test']
)

csharp_library(
    name = 'KRPC.Client',
    srcs = glob(['src/**/*.cs']) + [
        ':AssemblyInfo', '//protobuf:csharp',
        ':services-krpc', ':services-spacecenter', ':services-infernalrobotics', ':services-kerbalalarmclock'],
    deps = [
        '//tools/build/mono-4.5:Google.Protobuf',
        '//tools/build/mono-4.5:mscorlib',
        '//tools/build/mono-4.5:System',
        '//tools/build/mono-4.5:System.Core',
        '//tools/build/mono-4.5:System.Runtime',
        '//tools/build/mono-4.5:System.IO',
        '//tools/build/mono-4.5:System.Xml',
        '//tools/build/mono-4.5:System.Xml.Linq'
    ],
    visibility = ['//visibility:public']
)

clientgen_csharp(
    name = 'services-krpc',
    service = 'KRPC',
    defs = '//server:ServiceDefinitions',
    out = 'Services/KRPC.cs'
)

clientgen_csharp(
    name = 'services-spacecenter',
    service = 'SpaceCenter',
    defs = '//service/SpaceCenter:ServiceDefinitions',
    out = 'Services/SpaceCenter.cs'
)

clientgen_csharp(
    name = 'services-kerbalalarmclock',
    service = 'KerbalAlarmClock',
    defs = '//service/KerbalAlarmClock:ServiceDefinitions',
    out = 'Services/KerbalAlarmClock.cs'
)

clientgen_csharp(
    name = 'services-infernalrobotics',
    service = 'InfernalRobotics',
    defs = '//service/InfernalRobotics:ServiceDefinitions',
    out = 'Services/InfernalRobotics.cs'
)

client_test(
    name = 'test',
    test_executable = ':KRPC.Client.Test',
    server_executable = '//tools/TestServer',
    tags = ['requires-network'],
    size = 'small',
    visibility = ['//:__pkg__']
)

csharp_nunit_test(
    name = 'KRPC.Client.Test',
    srcs = glob(['test/**/*.cs']) + [':services-testservice'],
    deps = [
        ':KRPC.Client',
        '//tools/build/mono-4.5:Moq',
        '//tools/build/mono-4.5:Google.Protobuf',
        '//tools/build/mono-4.5:mscorlib',
        '//tools/build/mono-4.5:System',
        '//tools/build/mono-4.5:System.Core',
        '//tools/build/mono-4.5:System.Runtime',
        '//tools/build/mono-4.5:System.IO',
        '//tools/build/mono-4.5:System.Xml',
        '//tools/build/mono-4.5:System.Xml.Linq'
    ],
    nowarn = ['1591', '1573'],
    size = 'small',
    tags = ['requires-network']
)

clientgen_csharp(
    name = 'services-testservice',
    service = 'TestService',
    defs = '//tools/TestServer:ServiceDefinitions',
    out = 'Services/TestService.cs'
)
