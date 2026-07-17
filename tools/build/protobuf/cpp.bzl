" C++ protobuf tools "

def _impl(ctx):
    header = ctx.outputs.header
    source = ctx.outputs.source
    include = ctx.attr.include
    proto_output = source.path + ".tmp-proto-cpp"
    proto_path = ctx.attr.src.files.to_list()[0].path
    proto_include = proto_path.replace(".proto", ".pb.h")

    args = ctx.actions.args()
    args.add("--protoc", ctx.file._protoc.path)
    args.add("--mkdir", proto_output)
    args.add("--copy", proto_output + "/protobuf/*.pb.h=" + header.path)
    args.add("--copy", proto_output + "/protobuf/*.pb.cc=" + source.path)
    args.add("--rewrite", "%s=#include \"%s\"=#include \"%s\"" % (source.path, proto_include, include))
    args.add("--")
    args.add("--cpp_out=" + proto_output)
    args.add(ctx.file.src.path)

    ctx.actions.run(
        executable = ctx.executable._runner,
        arguments = [args],
        inputs = [ctx.file.src, ctx.file._protoc],
        outputs = [header, source],
        mnemonic = "ProtobufCpp",
    )

protobuf_cpp = rule(
    implementation = _impl,
    attrs = {
        "src": attr.label(allow_single_file = [".proto"]),
        "header": attr.output(mandatory = True),
        "source": attr.output(mandatory = True),
        "include": attr.string(mandatory = True),
        "_protoc": attr.label(default = Label("//tools/build/protobuf:protoc"), allow_single_file = True),
        "_runner": attr.label(
            default = Label("//tools/build/protobuf:run_protoc"),
            executable = True,
            cfg = "exec",
        ),
    },
)
