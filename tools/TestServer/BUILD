load('//tools/build:csharp.bzl', 'csharp_binary', 'csharp_library', 'csharp_assembly_info', 'csharp_gendarme_test')
load('//tools/build/protobuf:csharp.bzl', 'protobuf_csharp')
load('//tools/build:pkg.bzl', 'pkg_zip')
load('//tools/ServiceDefinitions:build.bzl', 'service_definitions')
load('//:config.bzl', 'version', 'assembly_version', 'author')

csharp_assembly_info(
    name = 'AssemblyInfo',
    title = 'TestServer',
    version = assembly_version,
    copyright = author,
    cls_compliant = False
)

srcs = ['src/TestService.cs', ':AssemblyInfo']
deps = [
    '//tools/ServiceDefinitions:KRPC',
    '//tools/build/mono-4.5:Google.Protobuf',
    '//tools/build/mono-4.5:mscorlib',
    '//tools/build/mono-4.5:System',
    '//tools/build/mono-4.5:System.Core',
    '//tools/build/mono-4.5:System.Runtime',
    '//tools/build/mono-4.5:System.IO',
    '//tools/build/mono-4.5:System.Xml',
    '//tools/build/ksp:KRPC.IO.Ports'
]

exe_srcs = srcs + ['src/Program.cs']
exe_deps = deps + ['//tools/build/mono-4.5:NDesk.Options']

pkg_zip(
    name = 'archive',
    out = 'TestServer-%s.zip' % version,
    files = [
        ':README.txt',
        ':TestServer',
        '//:version',
        '//tools/build/protobuf:LICENSE'
    ] + exe_deps,
    path_map = {
        'tools/TestServer/': '',
        'tools/build/protobuf/LICENSE': 'LICENSE.protobuf',
        'tools/build/mono-4.5/': '',
        'tools/build/ksp/': '',
        'tools/ServiceDefinitions/': ''
    },
    exclude = ['*.mdb', 'tools/TestServer/TestServer']
)

csharp_binary(
    name = 'TestServer',
    srcs = exe_srcs,
    deps = exe_deps,
    nowarn = ['1591'],
    visibility = ['//visibility:public']
)

csharp_library(
    name = 'TestService',
    srcs = srcs,
    deps = deps,
    nowarn = ['1591']
)

service_definitions(
    name = 'ServiceDefinitions',
    assemblies = [':TestService'],
    service = 'TestService',
    out = 'TestService.json',
    visibility = ['//visibility:public']
)

csharp_binary(
    name = 'TestServer.Debug',
    srcs = exe_srcs,
    deps = exe_deps,
    optimize = False,
    nowarn = ['1591'],
    define = ['CODE_ANALYSIS'],
    visibility = ['//visibility:public']
)

csharp_gendarme_test(
    name = 'test',
    exe = ':TestServer.Debug',
    ignores = 'src/ignores.txt',
    size = 'small'
)
