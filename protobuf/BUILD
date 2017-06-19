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
    name = 'python',
    out = 'KRPC_pb2.py',
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

# Workaround to fix redundant casts in protobuf v3.1.0 generated output
genrule(
    name = 'java',
    srcs = [':java_gen'],
    outs = ['KRPC.java'],
    cmd = 'sed \'s/(Builder) super/super/g\' $(location :java_gen) > "$@"'
)

protobuf_java(
    name = 'java_gen',
    out = 'KRPC.java.tmp',
    src = 'krpc.proto'
)
