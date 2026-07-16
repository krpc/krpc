" C# protobuf tools "

def _impl(ctx):
    output = ctx.outputs.out
    protoc_output = output.path + ".tmp-protoc-output"

    args = ctx.actions.args()
    args.add("--protoc", ctx.file.protoc.path)
    args.add("--mkdir", protoc_output)
    args.add("--copy", protoc_output + "/*.cs=" + output.path)
    args.add("--")
    args.add("--csharp_out=" + protoc_output)
    args.add(ctx.file.src.path)

    ctx.actions.run(
        executable = ctx.executable._runner,
        arguments = [args],
        inputs = [ctx.file.src, ctx.file.protoc],
        outputs = [output],
        mnemonic = "ProtobufCSharp",
    )

protobuf_csharp = rule(
    implementation = _impl,
    attrs = {
        "src": attr.label(allow_single_file = [".proto"]),
        "out": attr.output(mandatory = True),
        "protoc": attr.label(default = Label("//tools/build/protobuf:protoc"), allow_single_file = True),
        "_runner": attr.label(
            default = Label("//tools/build/protobuf:run_protoc"),
            executable = True,
            cfg = "exec",
        ),
    },
)
