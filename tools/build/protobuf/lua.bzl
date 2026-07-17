" Lua protobuf tools "

def _impl(ctx):
    input = ctx.file.src
    output = ctx.outputs.out
    protoc = ctx.file._protoc
    plugin = ctx.executable._plugin

    protoc_output = output.path + ".tmp-proto-lua"

    args = ctx.actions.args()
    args.add("--protoc", protoc.path)
    args.add("--mkdir", protoc_output)
    args.add("--copy", protoc_output + "/protobuf/*.lua=" + output.path)
    args.add("--")
    args.add("--plugin=protoc-gen-lua=" + plugin.path)
    args.add("--lua_out=" + protoc_output)
    args.add(input.path)

    ctx.actions.run(
        executable = ctx.executable._runner,
        arguments = [args],
        inputs = [input, protoc],
        outputs = [output],
        tools = [ctx.attr._plugin[DefaultInfo].files_to_run],
        mnemonic = "ProtobufLua",
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
        "_runner": attr.label(
            default = Label("//tools/build/protobuf:run_protoc"),
            executable = True,
            cfg = "exec",
        ),
    },
)
