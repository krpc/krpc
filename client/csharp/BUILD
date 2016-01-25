load('/tools/build/csharp', 'csharp_library', 'csharp_nunit_test', 'csharp_assembly_info', 'nuget_package')
load('/tools/build/client_test', 'client_test')
load('/config', 'version', 'copyright')
load('generate', 'generate')

filegroup(
    name = 'csharp',
    srcs = [':KRPC.Client', 'CHANGES.txt'],
    visibility = ['//visibility:public']
)

nuget_package(
    name = 'nuget',
    assembly = ':KRPC.Client',
    id = 'KRPC.Client',
    version = version+'-beta2', #TODO: remove beta version number when Google.Protobuf is released
    copyright = copyright,
    description = 'C# client for kRPC',
    project_url = 'https://github.com/djungelorm/krpc',
    license_url = 'https://github.com/djungelorm/krpc/blob/master/LICENSE',
    framework_deps = [
        'System',
        'System.Core',
        'System.Runtime',
        'System.IO',
        'System.Xml',
        'System.Xml.Linq'
    ],
    deps = {
        'Google.Protobuf': '3.0.0-beta2'
    }
)

csharp_assembly_info(
    name = 'AssemblyInfo',
    title = 'KRPC.Client',
    description = 'C# client for kRPC',
    copyright = copyright,
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

generate(
    name = 'services-krpc',
    service = 'KRPC',
    srcs = ['//server:ServiceDefinitions'],
    out = 'Services/KRPC.cs'
)

generate(
    name = 'services-spacecenter',
    service = 'SpaceCenter',
    srcs = ['//service/SpaceCenter:ServiceDefinitions'],
    out = 'Services/SpaceCenter.cs'
)

generate(
    name = 'services-kerbalalarmclock',
    service = 'KerbalAlarmClock',
    srcs = ['//service/KerbalAlarmClock:ServiceDefinitions'],
    out = 'Services/KerbalAlarmClock.cs'
)

generate(
    name = 'services-infernalrobotics',
    service = 'InfernalRobotics',
    srcs = ['//service/InfernalRobotics:ServiceDefinitions'],
    out = 'Services/InfernalRobotics.cs'
)

client_test(
    name = 'test',
    test_executable = ':KRPC.Client.Test',
    server_executable = '//tools/TestServer',
    rpc_port = '50018',
    stream_port = '50019',
    tags = ['requires-network'],
    size = 'small',
    visibility = ['//:__pkg__']
)

csharp_nunit_test(
    name = 'KRPC.Client.Test',
    srcs = glob(['test/**/*.cs']) + [':services-testservice', '//protobuf:csharp-test'],
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
    size = 'small',
    tags = ['requires-network']
)

generate(
    name = 'services-testservice',
    service = 'TestService',
    srcs = ['//tools/TestServer:ServiceDefinitions'],
    out = 'Services/TestService.cs'
)
