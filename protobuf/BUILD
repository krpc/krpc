load('//tools/build/protobuf:cpp.bzl', 'protobuf_cpp')
load('//tools/build/protobuf:csharp.bzl', 'protobuf_csharp')
load('//tools/build/protobuf:java.bzl', 'protobuf_java')
load('//tools/build/protobuf:lua.bzl', 'protobuf_lua')
load('//tools/build/protobuf:nanopb.bzl', 'protobuf_nanopb')
load('//tools/build/protobuf:python.bzl', 'protobuf_py')

package(default_visibility = ['//visibility:public'])

exports_files(['krpc.proto', 'LICENSE'])

protobuf_csharp(
    name = 'csharp',
    out = 'KRPC.cs',
    src = 'krpc.proto'
)

protobuf_csharp(
    name = 'csharp_unity',
    out = 'KRPC_unity.cs',
    src = 'krpc.proto',
    protoc = '//tools/build/protobuf:protoc_unity'
)

protobuf_py(
    name = 'python',
    out = 'KRPC_pb2.py',
    out_pyi = 'KRPC_pb2.pyi',
    src = 'krpc.proto'
)

protobuf_cpp(
    name = 'cpp',
    header = 'krpc.pb.hpp',
    source = 'krpc.pb.cpp',
    include = 'krpc/krpc.pb.hpp',
    src = 'krpc.proto'
)

protobuf_lua(
    name = 'lua',
    out = 'KRPC.lua',
    src = 'krpc.proto'
)

protobuf_java(
    name = 'java',
    out = 'KRPC.java',
    src = 'krpc.proto'
)

protobuf_nanopb(
    name = 'cnano',
    header = 'krpc.pb.h',
    source = 'krpc.pb.c',
    include = 'krpc.pb.h',
    src = 'krpc.proto',
    options = '//client/cnano:src/krpc.options'
)
