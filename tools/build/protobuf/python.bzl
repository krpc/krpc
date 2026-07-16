" python protobuf tools "

def _impl(ctx):
    output = ctx.outputs.out
    output_pyi = ctx.outputs.out_pyi
    protoc_output = output.path + ".tmp-proto-py"

    if (not output.path.endswith("_pb2.py")):
        fail("protoc output path must end with _pb2.py")
    if (not output_pyi.path.endswith("_pb2.pyi")):
        fail("protoc pyi output path must end with _pb2.pyi")

    args = ctx.actions.args()
    args.add("--protoc", ctx.file._protoc.path)
    args.add("--mkdir", protoc_output)
    args.add("--copy", protoc_output + "/protobuf/*.py=" + output.path)
    args.add("--copy", protoc_output + "/protobuf/*.pyi=" + output_pyi.path)
    args.add("--")
    args.add("--python_out=" + protoc_output)
    args.add("--pyi_out=" + protoc_output)
    args.add(ctx.file.src.path)

    ctx.actions.run(
        executable = ctx.executable._runner,
        arguments = [args],
        inputs = [ctx.file.src, ctx.file._protoc],
        outputs = [output, output_pyi],
        mnemonic = "ProtobufPython",
    )

protobuf_py = rule(
    implementation = _impl,
    attrs = {
        "src": attr.label(allow_single_file = [".proto"]),
        "_protoc": attr.label(
            default = Label("//tools/build/protobuf:protoc"),
            allow_single_file = True,
        ),
        "out": attr.output(mandatory = True),
        "out_pyi": attr.output(mandatory = True),
        "_runner": attr.label(
            default = Label("//tools/build/protobuf:run_protoc"),
            executable = True,
            cfg = "exec",
        ),
    },
)
