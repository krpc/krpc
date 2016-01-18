def _impl(ctx):
    output = ctx.outputs.out
    protoc_output = output.path + '.tmp-proto-lua'

    path = ctx.files._protoc_lua[0].dirname
    sub_commands = [
        'virtualenv env --quiet --system-site-packages',
        'virtualenv env --quiet --relocatable',
        'env/bin/pip install --quiet --no-deps %s' % ctx.file._protobuf_library.path,
        'rm -rf %s' % protoc_output,
        'mkdir -p %s' % protoc_output,
        'PATH=$PATH:%s %s --lua_out=%s %s' % (path, ctx.file._protoc.path, protoc_output, ctx.file.src.path),
        'cp %s/protobuf/*.lua %s' % (protoc_output, output.path)
    ]

    ctx.action(
        inputs = [ctx.file.src, ctx.file._protoc, ctx.file._protobuf_library] + ctx.files._protoc_lua,
        outputs = [output],
        command = ' && \n'.join(sub_commands),
        mnemonic = 'ProtobufLua',
        use_default_shell_env = True
    )

protobuf_lua = rule(
    implementation = _impl,
    attrs = {
        'src': attr.label(allow_files=FileType(['.proto']), single_file=True),
        '_protoc': attr.label(default=Label('//tools/build/protobuf:protoc'), allow_files=True, single_file=True),
        '_protoc_lua': attr.label(default=Label('@protoc-lua//:plugin'), allow_files=True),
        '_protobuf_library': attr.label(default=Label('@python.protobuf//file'),
                                        allow_files=True, single_file=True),
        'out': attr.output(mandatory=True)
    }
)
