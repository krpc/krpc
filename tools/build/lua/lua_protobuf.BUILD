"""lua-protobuf, the protocol buffer runtime the client encodes and decodes with."""

load("@rules_cc//cc:defs.bzl", "cc_library")

cc_library(
    name = "pb",
    srcs = ["pb.c"],
    hdrs = ["pb.h"],
    # Upstream sources, compiled unmodified
    copts = select({
        "@platforms//os:windows": ["/w"],
        "//conditions:default": ["-w"],
    }),
    visibility = ["//visibility:public"],
    deps = ["@lua"],
)
