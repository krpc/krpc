"""protobuf-lua: the protoc plugin that generates the schema, and the runtime it generates for."""

load("@rules_cc//cc:defs.bzl", "cc_library")

filegroup(
    name = "plugin",
    srcs = ["protoc-plugin/protoc-gen-lua"],
    visibility = ["//visibility:public"],
)

filegroup(
    name = "lua_srcs",
    srcs = glob(["protobuf/*.lua"]),
    visibility = ["//visibility:public"],
)

cc_library(
    name = "pb",
    srcs = ["protobuf/pb.c"],
    visibility = ["//visibility:public"],
    deps = ["@lua//:lua"],
)
