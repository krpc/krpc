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
    protoc_nanopb_opts = '-Q \\"#include \\\\\\"' + include + '/%s\\\\\\"\\" ' + \
                         '-L \\"#include <' + include + '/%s>\\"'

    include += "/" + proto_name + ".pb.h"

    protoc_input = source.path + ".src-proto-nanopb"
    protoc_output = source.path + ".tmp-proto-nanopb"

    sub_commands = [
        "rm -rf %s %s" % (protoc_input, protoc_output),
        "mkdir -p %s" % protoc_input,
        "mkdir -p %s" % protoc_output,
        "cp %s %s" % (input.path, protoc_input),
        "cp %s %s" % (options.path, protoc_input),
        '%s "--plugin=protoc-gen-nanopb=$PWD/%s" "--nanopb_out=%s:%s" %s/%s' % (protoc.path, plugin.path, protoc_nanopb_opts, protoc_output, protoc_input, input.basename),
        "cp %s/%s/*.pb.h %s" % (protoc_output, protoc_input, header.path),
        "cp %s/%s/*.pb.c %s" % (protoc_output, protoc_input, source.path),
        'sed -i \'s/#include ".\\+"/#include "%s"/g\' %s' % (include.replace("/", "\\/"), source.path),
    ]

    ctx.actions.run_shell(
        inputs = [input, options, protoc],
        outputs = [header, source],
        tools = [plugin, ctx.attr._plugin[DefaultInfo].files_to_run],
        mnemonic = "ProtobufNanopb",
        command = " && \\\n".join(sub_commands) + "\n",
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
    },
)
