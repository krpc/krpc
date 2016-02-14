load('/tools/build/protobuf/csharp', 'protobuf_csharp')
load('/tools/build/protobuf/python', 'protobuf_py')
load('/tools/build/protobuf/cpp', 'protobuf_cpp')
load('/tools/build/protobuf/lua', 'protobuf_lua')
load('/tools/build/protobuf/java', 'protobuf_java')

package(default_visibility = ['//visibility:public'])

exports_files(['krpc.proto', 'LICENSE'])

protobuf_csharp(
    name = 'csharp',
    out = 'KRPC.cs',
    src = 'krpc.proto'
)

protobuf_py(
    name = 'py',
    out = 'KRPC.py',
    src = 'krpc.proto'
)

protobuf_cpp(
    name = 'cpp',
    header = 'krpc.pb.hpp',
    source = 'krpc.pb.cpp',
    include = 'krpc.pb.hpp',
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
