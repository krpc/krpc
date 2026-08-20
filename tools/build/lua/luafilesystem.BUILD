"""LuaFileSystem, built from source. Penlight declares it as a dependency."""

load("@rules_cc//cc:defs.bzl", "cc_library")

cc_library(
    name = "lfs",
    srcs = [
        "src/lfs.c",
        "src/lfs.h",
    ],
    visibility = ["//visibility:public"],
    deps = ["@lua//:lua"],
)
