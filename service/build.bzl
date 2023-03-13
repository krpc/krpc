load('//tools/build/ksp:build.bzl', 'ksp_libs')

# Default dependencies for service assemblies
service_deps = [
    '//server:KRPC',
    '//core:KRPC.Core',
    '//tools/build/ksp:Google.Protobuf',
] + ksp_libs
