load("//tools/build/ksp:build.bzl", "ksp_libs")

# Default dependencies for service assemblies
service_deps = [
    "//core:KRPC.Core",
    "//server:KRPC",
    "//tools/build/ksp:Google.Protobuf",
] + ksp_libs
