def _impl(ctx):
    header = ctx.outputs.header
    source = ctx.outputs.source
    include = ctx.attr.include
    proto_output = source.path + '.tmp-proto-cpp'
    proto_path = ctx.attr.src.files.to_list()[0].path
    proto_include = proto_path.replace('.proto', '.pb.h')

    sub_commands = [
        'rm -rf %s' % proto_output,
        'mkdir -p %s' % proto_output,
        '%s --cpp_out=%s %s' % (ctx.file._protoc.path, proto_output, ctx.file.src.path),
        'cp %s/protobuf/*.pb.h %s' % (proto_output, header.path),
        'cp %s/protobuf/*.pb.cc %s' % (proto_output, source.path),
        'sed -i \'s/#include "%s"/#include "%s"/g\' %s' % (proto_include.replace('/', '\\/'), include.replace('/', '\\/'), source.path)
    ]

    ctx.actions.run_shell(
        inputs = [ctx.file.src, ctx.file._protoc],
        command = ' && '.join(sub_commands),
        outputs = [header, source],
        mnemonic = 'ProtobufCpp',
        use_default_shell_env = True
    )

protobuf_cpp = rule(
    implementation = _impl,
    attrs = {
        'src': attr.label(allow_single_file=['.proto']),
        'header': attr.output(mandatory=True),
        'source': attr.output(mandatory=True),
        'include': attr.string(mandatory=True),
        '_protoc': attr.label(default=Label('//tools/build/protobuf:protoc'), allow_single_file=True)
    },
    output_to_genfiles = True
)
