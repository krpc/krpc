def _impl(ctx):

    server_executable_runfiles = \
        list(ctx.attr.server_executable.default_runfiles.files) + \
        list(ctx.attr.server_executable.files)
    runfiles = ctx.runfiles(
        files = [ctx.executable.test_executable, ctx.executable.server_executable] + server_executable_runfiles)

    sub_commands = []
    for f in server_executable_runfiles:
        sub_commands.append('mkdir -p `dirname %s`' % (ctx.executable.server_executable.short_path+'.runfiles/' + f.short_path))
        sub_commands.append('ln -f -r -s %s %s' % (f.short_path, ctx.executable.server_executable.short_path+'.runfiles/' + f.short_path))

    sub_commands.extend([
        'pkill TestServer.exe',
        '%s %s %s &' % (ctx.executable.server_executable.short_path, ctx.attr.rpc_port, ctx.attr.stream_port),
        '%s' % ctx.executable.test_executable.short_path,
        'RESULT=$?',
        'pgrep TestServer.exe',
        'pkill TestServer.exe',
        'exit $RESULT'
    ])
    ctx.file_action(
        output = ctx.outputs.executable,
        content = '\n'.join(sub_commands)+'\n',
        executable = True
    )

    return struct(
        name = ctx.label.name,
        out = ctx.outputs.executable,
        runfiles = runfiles
    )

client_test = rule(
    implementation = _impl,
    attrs = {
        'test_executable': attr.label(executable=True),
        'server_executable': attr.label(executable=True),
        'rpc_port': attr.string(mandatory=True),
        'stream_port': attr.string(mandatory=True)
    },
    test = True
)
