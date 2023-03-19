load('//tools/build:csharp.bzl', 'csharp_library', 'csharp_assembly_info', 'nuget_package')
load('//tools/build:csharp.bzl', 'csharp_nunit_test', 'csharp_gendarme_test')
load('//tools/build:client_test.bzl', 'client_test')
load('//tools/krpctools:clientgen.bzl', 'clientgen_csharp')
load('//tools/build:pkg.bzl', 'pkg_zip')
load('//:config.bzl', 'version', 'assembly_version', 'nuget_version', 'author')

pkg_zip(
    name = 'csharp',
    out = 'krpc-csharp-%s.zip' % version,
    files = ['//:readme', '//:version', 'CHANGES.txt',
             'LICENSE', '//:COPYING', '//:COPYING.LESSER',
             ':KRPC.Client'],
    path_map = {
        'client/csharp/KRPC.Client.dll': 'net45/KRPC.Client.dll',
        'client/csharp/KRPC.Client.xml': 'net45/KRPC.Client.xml',
        'client/csharp/': ''
    },
    exclude = ['*.mdb'],
    visibility = ['//visibility:public']
)

nuget_package(
    name = 'nuget',
    id = 'KRPC.Client',
    assembly = ':KRPC.Client',
    version = nuget_version,
    author = author,
    description = 'C# client for kRPC',
    project_url = 'https://github.com/krpc/krpc',
    license = 'LGPL-3.0-only',
    deps = {
        'Google.Protobuf': '3.22.0'
    }
)

csharp_assembly_info(
    name = 'AssemblyInfo',
    title = 'KRPC.Client',
    description = 'C# client for kRPC',
    copyright = author,
    version = assembly_version,
    internals_visible_to = ['KRPC.Client.Test'],
    cls_compliant = False,
    visibility = [
        '//:__pkg__' # Make visible to //:csproj-deps so it can copy AssemblyInfo.cs to generated_deps
    ]
)

srcs = glob(['src/**/*.cs']) + [
    ':AssemblyInfo',
    '//protobuf:csharp',
    ':services-krpc',
    ':services-spacecenter',
    ':services-drawing',
    ':services-infernalrobotics',
    ':services-kerbalalarmclock',
    ':services-remotetech',
    ':services-ui',
    ':services-lidar',
    ':services-dockingcamera'
]
deps = [
    '//tools/build/mono-4.5:Google.Protobuf',
    '//tools/build/mono-4.5:mscorlib',
    '//tools/build/mono-4.5:System',
    '//tools/build/mono-4.5:System.Core',
    '//tools/build/mono-4.5:System.Runtime',
    '//tools/build/mono-4.5:System.IO',
    '//tools/build/mono-4.5:System.Xml',
    '//tools/build/mono-4.5:System.Memory',
    '//tools/build/mono-4.5:System.Runtime.CompilerServices.Unsafe'
]

csharp_library(
    name = 'KRPC.Client',
    srcs = srcs,
    deps = deps,
    visibility = ['//visibility:public'],
    nowarn = ['1685']
)

clientgen_csharp(
    name = 'services-krpc',
    service = 'KRPC',
    defs = '//core:ServiceDefinitions',
    out = 'Services/KRPC.cs',
    visibility = [
        '//:__pkg__'
    ]
)

clientgen_csharp(
    name = 'services-spacecenter',
    service = 'SpaceCenter',
    defs = '//service/SpaceCenter:ServiceDefinitions',
    out = 'Services/SpaceCenter.cs',
    visibility = [
        '//:__pkg__'
    ]
)

clientgen_csharp(
    name = 'services-drawing',
    service = 'Drawing',
    defs = '//service/Drawing:ServiceDefinitions',
    out = 'Services/Drawing.cs'
)

clientgen_csharp(
    name = 'services-infernalrobotics',
    service = 'InfernalRobotics',
    defs = '//service/InfernalRobotics:ServiceDefinitions',
    out = 'Services/InfernalRobotics.cs',
    visibility = [
        '//:__pkg__'
    ]
)

clientgen_csharp(
    name = 'services-kerbalalarmclock',
    service = 'KerbalAlarmClock',
    defs = '//service/KerbalAlarmClock:ServiceDefinitions',
    out = 'Services/KerbalAlarmClock.cs',
    visibility = [
        '//:__pkg__'
    ]
)

clientgen_csharp(
    name = 'services-remotetech',
    service = 'RemoteTech',
    defs = '//service/RemoteTech:ServiceDefinitions',
    out = 'Services/RemoteTech.cs'
)

clientgen_csharp(
    name = 'services-ui',
    service = 'UI',
    defs = '//service/UI:ServiceDefinitions',
    out = 'Services/UI.cs'
)

clientgen_csharp(
    name = 'services-lidar',
    service = 'LiDAR',
    defs = '//service/LiDAR:ServiceDefinitions',
    out = 'Services/LiDAR.cs'
)

clientgen_csharp(
    name = 'services-dockingcamera',
    service = 'DockingCamera',
    defs = '//service/DockingCamera:ServiceDefinitions',
    out = 'Services/DockingCamera.cs'
)

test_suite(
    name = 'test',
    tests = [':client', ':lint'],
    visibility = ['//:__pkg__']
)

test_suite(
    name = 'ci-test',
    tests = [':client']
)

test_suite(
    name = 'lint',
    tests = [':gendarme-KRPC.Client', ':gendarme-KRPC.Client.Test']
)

client_test(
    name = 'client',
    test_executable = ':test-KRPC.Client',
    server_executable = '//tools/TestServer',
    tags = ['local', 'requires-network'],
    size = 'small'
)

csharp_assembly_info(
    name = 'TestAssemblyInfo',
    title = 'KRPC.Client.Test',
    copyright = author,
    version = assembly_version,
    cls_compliant = False
)

test_deps = [
    ':KRPC.Client',
    '//tools/build/mono-4.5:Moq',
    '//tools/build/mono-4.5:System.Threading.Tasks.Extensions',
    '//tools/build/mono-4.5:Castle.Core'
] + deps

csharp_library(
    name = 'KRPC.Client.Test',
    srcs = glob(['test/**/*.cs']) + [':TestAssemblyInfo'] + [':services-testservice'],
    deps = test_deps,
    nunit_test = True,
    optimize = False,
    define = ['CODE_ANALYSIS'],
    nowarn = ['1591', '1573', '1685'],
    visibility = ['//:__pkg__']
)

csharp_nunit_test(
    name = 'test-KRPC.Client',
    lib = ':KRPC.Client.Test',
    deps = test_deps,
    size = 'small',
    # FIXME: need to run test outside the sandbox to work around nunit file accesses
    tags = ['local', 'requires-network']
)

clientgen_csharp(
    name = 'services-testservice',
    service = 'TestService',
    defs = '//tools/TestServer:ServiceDefinitions',
    out = 'Services/TestService.cs',
    visibility = [
        '//:__pkg__'
    ]
)

csharp_library(
    name = 'KRPC.Client.Debug',
    srcs = srcs,
    deps = deps,
    optimize = False,
    define = ['CODE_ANALYSIS'],
    nowarn = ['1685']
)

csharp_gendarme_test(
    name = 'gendarme-KRPC.Client',
    lib = ':KRPC.Client.Debug',
    ignores = 'src/ignores.txt',
    size = 'small'
)

csharp_gendarme_test(
    name = 'gendarme-KRPC.Client.Test',
    lib = ':KRPC.Client.Test',
    ignores = 'test/ignores.txt',
    size = 'small'
)
