def _impl(ctx):

    test_executable_runfiles = list(ctx.attr.test_executable.default_runfiles.files)
    server_executable_runfiles = list(ctx.attr.server_executable.default_runfiles.files)

    sub_commands = [
        'mkdir -p `dirname test-executable.runfiles/%s`' % ctx.executable.test_executable.short_path,
        'ln -f -r -s %s test-executable.runfiles/%s' % (ctx.executable.test_executable.short_path, ctx.executable.test_executable.short_path),
        'mkdir -p `dirname server-executable.runfiles/%s`' % ctx.executable.server_executable.short_path,
        'ln -f -r -s %s server-executable.runfiles/%s' % (ctx.executable.server_executable.short_path, ctx.executable.server_executable.short_path),
    ]

    test_runfiles_dir = 'test-executable.runfiles/' + ctx.executable.test_executable.short_path + '.runfiles'
    server_runfiles_dir = 'server-executable.runfiles/' + ctx.executable.server_executable.short_path + '.runfiles'

    for f in test_executable_runfiles:
        sub_commands.append('mkdir -p `dirname %s`' % (test_runfiles_dir + '/' + f.short_path))
        sub_commands.append('ln -f -r -s %s %s' % (f.short_path, test_runfiles_dir + '/' + f.short_path))

    for f in server_executable_runfiles:
        sub_commands.append('mkdir -p `dirname %s`' % (server_runfiles_dir + '/' + f.short_path))
        sub_commands.append('ln -f -r -s %s %s' % (f.short_path, server_runfiles_dir + '/' + f.short_path))

    sub_commands.extend([
        'pkill TestServer.exe',
        '(cd server-executable.runfiles; %s %s %s) &' % (ctx.executable.server_executable.short_path, ctx.attr.rpc_port, ctx.attr.stream_port),
        '(cd test-executable.runfiles/%s.runfiles; ../%s)' % (ctx.executable.test_executable.short_path, ctx.executable.test_executable.basename),
        'RESULT=$?',
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
        runfiles = ctx.runfiles(files = [ctx.executable.test_executable, ctx.executable.server_executable] + test_executable_runfiles + server_executable_runfiles)
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
