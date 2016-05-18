def _impl(ctx):
    output = ctx.outputs.out
    protoc_output = output.path + '.tmp-protoc-output'

    sub_commands = [
        'rm -rf %s' % protoc_output,
        'mkdir -p %s' % protoc_output,
        '%s --csharp_out=%s %s' % (ctx.file._protoc.path, protoc_output, ctx.file.src.path),
        'cp %s/*.cs %s' % (protoc_output, output.path)
    ]

    ctx.action(
        inputs = [ctx.file.src, ctx.file._protoc],
        outputs = [output],
        command = ' && '.join(sub_commands),
        mnemonic = 'ProtobufCSharp',
        use_default_shell_env = True
    )

protobuf_csharp = rule(
    implementation = _impl,
    attrs={
        'src': attr.label(allow_files=FileType(['.proto']), single_file=True),
        'out': attr.output(mandatory=True),
        '_protoc': attr.label(default=Label('@protobuf//:protoc'), allow_files=True, single_file=True),
    },
    output_to_genfiles = True
)
