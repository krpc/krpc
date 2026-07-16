" macros "

load("@rules_cc//cc:defs.bzl", "cc_binary")
load("@rules_dotnet//dotnet:defs.bzl", "csharp_library")
load("@rules_java//java:java_binary.bzl", "java_binary")

# buildifier: disable=function-docstring
def csharp_library_multiple(name, srcs, deps):
    names = []
    for src in srcs:
        subname = name + "/" + src
        names.append(subname)
        csharp_library(
            name = subname,
            srcs = [src],
            target_frameworks = ["net472"],
            deps = deps,
        )
    native.filegroup(name = name, srcs = names)

# buildifier: disable=function-docstring
def cc_binary_multiple(name, srcs, deps, target_compatible_with = None):
    names = []
    for src in srcs:
        subname = name + "/" + src
        names.append(subname)
        cc_binary(
            name = subname,
            srcs = [src],
            deps = deps,
            target_compatible_with = target_compatible_with,
        )
    native.filegroup(
        name = name,
        srcs = names,
        target_compatible_with = target_compatible_with,
    )

# buildifier: disable=function-docstring
def java_binary_multiple(name, srcs, deps, copts):
    names = []
    for src in srcs:
        subname = name + "/" + src
        names.append(subname)
        java_binary(
            name = subname,
            main_class = src.rpartition("/")[2].rpartition(".")[0],
            srcs = [src],
            deps = deps,
            javacopts = copts,
        )
    native.filegroup(name = name, srcs = names)
