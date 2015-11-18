# Copyright 2014 The Bazel Authors. All rights reserved.
#
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at
#
#    http://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.

# C++ protobuf support.
# Based on java support from Bazel source

proto_filetype = FileType([".proto"])

def gensrccpp_impl(ctx):
  out = ctx.outputs.headercpp
  proto_output = out.path + ".proto_output"
  proto_compiler = ctx.file._proto_compiler
  sub_commands = [
    "rm -rf " + proto_output,
    "mkdir " + proto_output,
    ' '.join([proto_compiler.path, "--cpp_out=" + proto_output,
              ctx.file.src.path]),
    "touch -t 198001010000 $(find " + proto_output + ")",
    "cp " + proto_output + "/protobuf/*.pb.h " + out.path, #TODO: don't use shell pattern matching - could be more than one file
  ]

  ctx.action(
    command=" && ".join(sub_commands),
    inputs=[ctx.file.src, proto_compiler],
    outputs=[out],
    mnemonic="GenProtoHeaderCpp",
    use_default_shell_env = True)

  out = ctx.outputs.srccpp
  proto_output = out.path + ".proto_output"
  proto_compiler = ctx.file._proto_compiler
  sub_commands = [
    "rm -rf " + proto_output,
    "mkdir " + proto_output,
    ' '.join([proto_compiler.path, "--cpp_out=" + proto_output,
              ctx.file.src.path]),
    "touch -t 198001010000 $(find " + proto_output + ")",
    "cp " + proto_output + "/protobuf/*.cc " + out.path, #TODO: don't use shell pattern matching - could be more than one file
  ]

  ctx.action(
    command=" && ".join(sub_commands),
    inputs=[ctx.file.src, proto_compiler],
    outputs=[out],
    mnemonic="GenProtoSrcCpp",
    use_default_shell_env = True)

gensrccpp = rule(
  gensrccpp_impl,
  attrs={
      "src": attr.label(allow_files=proto_filetype, single_file=True),
      # TODO(bazel-team): this should be a hidden attribute with a default
      # value, but Skylark needs to support select first.
      "_proto_compiler": attr.label(
          default=Label("//third_party:protoc"),
          allow_files=True,
          single_file=True),
  },
  outputs={"headercpp": "%{name}.pb.h", "srccpp": "%{name}.pb.cc"},
)

def proto_cpp_library(name, src):
  gensrccpp(name=name, src=src)
