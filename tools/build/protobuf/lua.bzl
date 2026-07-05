" Lua protobuf tools "

def _impl(ctx):
    input = ctx.file.src
    output = ctx.outputs.out
    protoc = ctx.file._protoc
    plugin = ctx.executable._plugin

    protoc_output = output.path + ".tmp-proto-lua"

    sub_commands = [
        "rm -rf %s" % protoc_output,
        "mkdir -p %s" % protoc_output,
        '%s "--plugin=protoc-gen-lua=$PWD/%s" --lua_out=%s %s' % (protoc.path, plugin.path, protoc_output, input.path),
        "cp %s/protobuf/*.lua %s" % (protoc_output, output.path),
    ]

    ctx.actions.run_shell(
        inputs = [input, protoc],
        outputs = [output],
        tools = [plugin, ctx.attr._plugin[DefaultInfo].files_to_run],
        mnemonic = "ProtobufLua",
        command = " && \\\n".join(sub_commands) + "\n",
    )

protobuf_lua = rule(
    implementation = _impl,
    attrs = {
        "src": attr.label(allow_single_file = [".proto"]),
        "out": attr.output(mandatory = True),
        "_protoc": attr.label(default = Label("//tools/build/protobuf:protoc"), allow_single_file = True),
        "_plugin": attr.label(
            default = Label("//tools/build/protobuf:protoc-gen-lua"),
            executable = True,
            cfg = "exec",
        ),
    },
)
