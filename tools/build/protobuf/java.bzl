def _impl(ctx):
    output = ctx.outputs.out
    protoc_output = output.path + '.tmp-proto-java'

    sub_commands = [
        'rm -rf %s' % protoc_output,
        'mkdir -p %s' % protoc_output,
        '%s --java_out=%s %s' % (ctx.file._protoc.path, protoc_output, ctx.file.src.path),
        'cp `find %s -name *.java` %s' % (protoc_output, output.path)
    ]

    ctx.actions.run_shell(
        inputs = [ctx.file.src, ctx.file._protoc],
        outputs = [output],
        command = ' && '.join(sub_commands),
        mnemonic = 'ProtobufJava',
        use_default_shell_env = True
    )

protobuf_java = rule(
    implementation = _impl,
    attrs = {
        'src': attr.label(allow_single_file=['.proto']),
        '_protoc': attr.label(default=Label('//tools/build/protobuf:protoc'), allow_single_file=True),
        'out': attr.output(mandatory=True)
    },
    output_to_genfiles = True
)
