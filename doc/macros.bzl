" macros "

load("@rules_cc//cc:defs.bzl", "cc_binary")
load("@rules_java//java:java_binary.bzl", "java_binary")
load("//tools/build:csharp.bzl", "csharp_binary", "csharp_library")

# buildifier: disable=function-docstring
def csharp_binary_multiple(name, srcs, deps):
    names = []
    for src in srcs:
        subname = name + "/" + src
        names.append(subname)
        csharp_binary(
            name = subname,
            srcs = [src],
            deps = deps,
        )
    native.filegroup(name = name, srcs = names)

# buildifier: disable=function-docstring
def csharp_library_multiple(name, srcs, deps):
    names = []
    for src in srcs:
        subname = name + "/" + src
        names.append(subname)
        csharp_library(
            name = subname,
            srcs = [src],
            deps = deps,
        )
    native.filegroup(name = name, srcs = names)

# buildifier: disable=function-docstring
def cc_binary_multiple(name, srcs, deps):
    names = []
    for src in srcs:
        subname = name + "/" + src
        names.append(subname)
        cc_binary(
            name = subname,
            srcs = [src],
            deps = deps,
        )
    native.filegroup(name = name, srcs = names)

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
