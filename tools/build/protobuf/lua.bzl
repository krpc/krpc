" Lua protobuf tools "

def _impl(ctx):
    output = ctx.outputs.out
    descriptor = ctx.actions.declare_file(ctx.label.name + ".pb")

    args = ctx.actions.args()
    args.add("--protoc", ctx.file._protoc.path)
    args.add("--")
    args.add("--descriptor_set_out=" + descriptor.path)
    args.add(ctx.file.src.path)
    ctx.actions.run(
        executable = ctx.executable._runner,
        arguments = [args],
        inputs = [ctx.file.src, ctx.file._protoc],
        outputs = [descriptor],
        mnemonic = "ProtobufDescriptor",
    )

    ctx.actions.run(
        executable = ctx.executable._wrapper,
        arguments = [descriptor.path, output.path],
        inputs = [descriptor],
        outputs = [output],
        mnemonic = "ProtobufLua",
    )

protobuf_lua = rule(
    doc = """A lua module returning the descriptor set of a .proto file, as a string.

    lua-protobuf loads a schema at run time with pb.load, from these bytes.""",
    implementation = _impl,
    attrs = {
        "src": attr.label(allow_single_file = [".proto"]),
        "out": attr.output(mandatory = True),
        "_protoc": attr.label(default = Label("//tools/build/protobuf:protoc"), allow_single_file = True),
        "_runner": attr.label(
            default = Label("//tools/build/protobuf:run_protoc"),
            executable = True,
            cfg = "exec",
        ),
        "_wrapper": attr.label(
            default = Label("//tools/build/protobuf:lua_descriptor"),
            executable = True,
            cfg = "exec",
        ),
    },
)
