def _create_py_env(out, install):
    tmp = out+'.tmp-create-py-env'
    cmds = [
        'rm -rf %s' % tmp,
        'virtualenv %s --quiet --no-site-packages' % tmp
    ]
    for lib in install:
        cmds.append('%s/bin/python %s/bin/pip install --quiet --no-deps %s' % (tmp, tmp, lib.path))
    cmds.extend([
        '(CWD=`pwd`; cd %s; tar -c -f $CWD/%s *)' % (tmp, out)
    ])
    return cmds

def _extract_py_env(env, path):
    return [
        'rm -rf %s' % path,
        'mkdir -p %s' % path,
        '(CWD=`pwd`; cd %s; tar -xf $CWD/%s)' % (path, env)
    ]

def _impl(ctx):
    input = ctx.file.src
    output = ctx.outputs.out
    protoc = ctx.file._protoc
    protoc_lua_env = ctx.file._protoc_lua_env
    protoc_lua = ctx.files._protoc_lua
    protoc_lua_dir = protoc_lua[0].dirname

    protoc_output = output.path + '.tmp-proto-lua'
    pyenv = output.path + '.tmp-proto-lua-env'

    sub_commands = _extract_py_env(protoc_lua_env.path, pyenv)
    sub_commands.extend([
        'rm -rf %s' % protoc_output,
        'mkdir -p %s' % protoc_output,
        'PATH=%s/bin:%s:$PATH %s --lua_out=%s %s' % (pyenv, protoc_lua_dir, protoc.path, protoc_output, input.path),
        'cp %s/protobuf/*.lua %s' % (protoc_output, output.path)
    ])

    ctx.action(
        inputs = [input, protoc, protoc_lua_env] + protoc_lua,
        outputs = [output],
        mnemonic = 'ProtobufLua',
        command = ' && \\\n'.join(sub_commands)+'\n'
    )

protobuf_lua = rule(
    implementation = _impl,
    attrs = {
        'src': attr.label(allow_files=FileType(['.proto']), single_file=True),
        'out': attr.output(mandatory=True),
        '_protoc': attr.label(default=Label('//tools/build/protobuf:protoc'), allow_files=True, single_file=True),
        '_protoc_lua': attr.label(default=Label('@protoc_lua//:plugin'), allow_files=True),
        '_protoc_lua_env': attr.label(default=Label('//tools/build/protobuf:protoc-lua-env'), allow_files=True, single_file=True)
    },
    output_to_genfiles = True
)

def _env_impl(ctx):
    pylibs = [ctx.file._protobuf, ctx.file._six]
    setup = ctx.new_file(ctx.configuration.genfiles_dir, 'protoc-lua-setup')
    ctx.file_action(
        output = setup,
        content = ' && \\\n'.join(_create_py_env(ctx.outputs.out.path, pylibs))+'\n',
        executable = True
    )
    ctx.action(
        inputs = pylibs,
        outputs = [ctx.outputs.out],
        progress_message = 'Setting up protoc-lua python environment',
        executable = setup,
        use_default_shell_env = True
    )

protoc_lua_env = rule(
    implementation = _env_impl,
    attrs = {
        '_protobuf': attr.label(default=Label('@python_protobuf//file'), allow_files=True, single_file=True),
        '_six': attr.label(default=Label('@python_six//file'), allow_files=True, single_file=True)
    },
    outputs = {'out': '%{name}.tar'},
    output_to_genfiles = True
)
