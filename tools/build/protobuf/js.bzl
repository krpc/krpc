" JavaScript protobuf tools "

def _impl(ctx):
    output = ctx.outputs.out
    protoc_output = output.path + ".tmp-proto-es"

    args = ctx.actions.args()
    args.add("--protoc", ctx.file._protoc.path)
    args.add("--mkdir", protoc_output)
    args.add("--copy", protoc_output + "/**/*_pb.ts=" + output.path)
    args.add("--")
    args.add("--plugin=protoc-gen-es=" + ctx.executable._plugin.path)
    args.add("--es_out=" + protoc_output)
    args.add("--es_opt=target=ts,import_extension=js")
    args.add(ctx.file.src.path)

    ctx.actions.run(
        executable = ctx.executable._runner,
        arguments = [args],
        inputs = [ctx.file.src, ctx.file._protoc],
        tools = [ctx.attr._plugin[DefaultInfo].files_to_run],
        outputs = [output],
        # The plugin is a js_binary, whose launcher finds its files from the output directory
        env = {"BAZEL_BINDIR": ctx.bin_dir.path},
        mnemonic = "ProtobufEs",
    )

protobuf_es = rule(
    doc = "The TypeScript module protoc-gen-es generates for the messages of a .proto file.",
    implementation = _impl,
    attrs = {
        "src": attr.label(allow_single_file = [".proto"]),
        "out": attr.output(mandatory = True),
        "_protoc": attr.label(default = Label("//tools/build/protobuf:protoc"), allow_single_file = True),
        "_plugin": attr.label(
            default = Label("//client/js:protoc-gen-es"),
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
