def _impl(ctx):
    output = ctx.outputs.out
    protoc_output = output.path + '.tmp-proto-py'

    sub_commands = [
        'rm -rf %s' % protoc_output,
        'mkdir -p %s' % protoc_output,
        '%s --python_out=%s %s' % (ctx.file._protoc.path, protoc_output, ctx.file.src.path),
        'cp %s/protobuf/*.py %s' % (protoc_output, output.path)
    ]

    ctx.action(
        inputs = [ctx.file.src, ctx.file._protoc],
        outputs = [output],
        command = ' && '.join(sub_commands),
        mnemonic = 'ProtobufPython',
        use_default_shell_env = True
    )

protobuf_py = rule(
    implementation = _impl,
    attrs = {
        'src': attr.label(allow_files=FileType(['.proto']), single_file=True),
        '_protoc': attr.label(default=Label('@protobuf//:protoc'), allow_files=True, single_file=True),
        'out': attr.output(mandatory=True)
    },
    output_to_genfiles = True
)
