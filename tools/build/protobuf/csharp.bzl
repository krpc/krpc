def _impl(ctx):
    output = ctx.outputs.out
    protoc_output = output.path + '.tmp-protoc-output'

    sub_commands = [
        'rm -rf %s' % protoc_output,
        'mkdir -p %s' % protoc_output,
        '%s --csharp_out=%s %s' % (ctx.file.protoc.path, protoc_output, ctx.file.src.path),
        'cp %s/*.cs %s' % (protoc_output, output.path)
    ]

    ctx.actions.run_shell(
        inputs = [ctx.file.src, ctx.file.protoc],
        outputs = [output],
        command = ' && '.join(sub_commands),
        mnemonic = 'ProtobufCSharp',
        use_default_shell_env = True
    )

protobuf_csharp = rule(
    implementation = _impl,
    attrs={
        'src': attr.label(allow_single_file=['.proto']),
        'out': attr.output(mandatory=True),
        'protoc': attr.label(default=Label('//tools/build/protobuf:protoc'), allow_single_file=True),
    },
    output_to_genfiles = True
)
