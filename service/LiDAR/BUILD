load('//:config.bzl', 'assembly_version', 'author')
load('//tools/build:csharp.bzl', 'csharp_library', 'csharp_assembly_info', 'csharp_gendarme_test')
load('//tools/build:python.bzl', 'py_lint_test')
load('//tools/ServiceDefinitions:build.bzl', 'service_definitions')
load('//service:build.bzl', 'service_deps')

filegroup(
    name = 'LiDAR',
    srcs = [':KRPC.LiDAR', ':ServiceDefinitions'],
    visibility = ['//:__pkg__']
)

csharp_assembly_info(
    name = 'AssemblyInfo',
    title = 'KRPC.LiDAR',
    description = 'LiDAR API for kRPC',
    version = assembly_version,
    copyright = author,
    cls_compliant = False,
    visibility = [
        '//:__pkg__' # Make visible to //:csproj-deps so it can copy AssemblyInfo.cs to generated_deps
    ]
)

srcs = [':AssemblyInfo'] + glob(['src/**/*.cs'])
deps = service_deps + ['//service/SpaceCenter:KRPC.SpaceCenter']

csharp_library(
    name = 'KRPC.LiDAR',
    srcs = srcs,
    deps = deps,
    visibility = ['//visibility:public']
)

service_definitions(
    name = 'ServiceDefinitions',
    assemblies = [':KRPC.LiDAR', '//service/SpaceCenter:KRPC.SpaceCenter'],
    service = 'LiDAR',
    out = 'KRPC.LiDAR.json',
    visibility = ['//visibility:public']
)


csharp_library(
    name = 'KRPC.LiDAR.Debug',
    srcs = srcs,
    deps = deps,
    optimize = False,
    define = ['CODE_ANALYSIS']
)
