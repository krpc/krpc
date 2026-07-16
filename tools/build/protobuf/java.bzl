" Java protobuf tools "

def _impl(ctx):
    output = ctx.outputs.out
    protoc_output = output.path + ".tmp-proto-java"

    args = ctx.actions.args()
    args.add("--protoc", ctx.file._protoc.path)
    args.add("--mkdir", protoc_output)
    args.add("--copy", protoc_output + "/**/*.java=" + output.path)
    args.add("--")
    args.add("--java_out=" + protoc_output)
    args.add(ctx.file.src.path)

    ctx.actions.run(
        executable = ctx.executable._runner,
        arguments = [args],
        inputs = [ctx.file.src, ctx.file._protoc],
        outputs = [output],
        mnemonic = "ProtobufJava",
    )

protobuf_java = rule(
    implementation = _impl,
    attrs = {
        "src": attr.label(allow_single_file = [".proto"]),
        "_protoc": attr.label(default = Label("//tools/build/protobuf:protoc"), allow_single_file = True),
        "out": attr.output(mandatory = True),
        "_runner": attr.label(
            default = Label("//tools/build/protobuf:run_protoc"),
            executable = True,
            cfg = "exec",
        ),
    },
)
