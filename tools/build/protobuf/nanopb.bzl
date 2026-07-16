" nanopb protobuf tools "

def _impl(ctx):
    input = ctx.file.src
    options = ctx.file.options
    header = ctx.outputs.header
    source = ctx.outputs.source
    include = ctx.attr.include
    proto_path = ctx.attr.src.files.to_list()[0].path
    proto_name = proto_path.rpartition("/")[2].replace(".proto", "")
    protoc = ctx.file._protoc
    plugin = ctx.executable._plugin

    # nanopb generator options controlling the generated #include line. The %s is
    # substituted by nanopb with the proto name; the inner \" survive protoc so
    # nanopb's shlex parser sees quoted values. No shell layer, so no extra
    # escaping beyond nanopb's own.
    nanopb_opts = '-Q "#include \\"' + include + '/%s\\"" -L "#include <' + include + '/%s>"'
    include += "/" + proto_name + ".pb.h"

    protoc_input = source.path + ".src-proto-nanopb"
    protoc_output = source.path + ".tmp-proto-nanopb"

    args = ctx.actions.args()
    args.add("--protoc", protoc.path)
    args.add("--mkdir", protoc_input)
    args.add("--mkdir", protoc_output)
    args.add("--stage", input.path + "=" + protoc_input)
    args.add("--stage", options.path + "=" + protoc_input)
    args.add("--copy", protoc_output + "/" + protoc_input + "/*.pb.h=" + header.path)
    args.add("--copy", protoc_output + "/" + protoc_input + "/*.pb.c=" + source.path)
    args.add("--rewrite", source.path + "=#include \".+\"=#include \"" + include + "\"")
    args.add("--")
    args.add("--plugin=protoc-gen-nanopb=" + plugin.path)
    args.add("--nanopb_out=" + nanopb_opts + ":" + protoc_output)
    args.add(protoc_input + "/" + input.basename)

    ctx.actions.run(
        executable = ctx.executable._runner,
        arguments = [args],
        inputs = [input, options, protoc],
        outputs = [header, source],
        tools = [ctx.attr._plugin[DefaultInfo].files_to_run],
        mnemonic = "ProtobufNanopb",
    )

protobuf_nanopb = rule(
    implementation = _impl,
    attrs = {
        "src": attr.label(allow_single_file = [".proto"]),
        "options": attr.label(allow_single_file = [".options"]),
        "header": attr.output(mandatory = True),
        "source": attr.output(mandatory = True),
        "include": attr.string(mandatory = True),
        "_protoc": attr.label(default = Label("//tools/build/protobuf:protoc"), allow_single_file = True),
        "_plugin": attr.label(
            default = Label("//tools/build/protobuf:protoc-gen-nanopb"),
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
