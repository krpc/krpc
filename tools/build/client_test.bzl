def _impl(ctx):
    server_type = ctx.attr.server_type

    test_executable_runfiles = ctx.attr.test_executable.default_runfiles.files.to_list()
    server_executable_runfiles = ctx.attr.server_executable.default_runfiles.files.to_list()

    sub_commands = [
        'mkdir -p `dirname test-executable.runfiles/krpc/%s`' % ctx.executable.test_executable.short_path,
        'ln -f -s "`pwd`/%s" "`pwd`/test-executable.runfiles/krpc/%s"' % (ctx.executable.test_executable.short_path, ctx.executable.test_executable.short_path),
        'mkdir -p `dirname server-executable.runfiles/krpc/%s`' % ctx.executable.server_executable.short_path,
        'ln -f -s "`pwd`/%s" "`pwd`/server-executable.runfiles/krpc/%s"' % (ctx.executable.server_executable.short_path, ctx.executable.server_executable.short_path),
    ]

    test_runfiles_dir = 'test-executable.runfiles/krpc/' + ctx.executable.test_executable.short_path + '.runfiles/krpc'
    server_runfiles_dir = 'server-executable.runfiles/krpc/' + ctx.executable.server_executable.short_path + '.runfiles/krpc'

    for f in test_executable_runfiles:
        sub_commands.append('mkdir -p `dirname %s`' % (test_runfiles_dir + '/' + f.short_path))
        sub_commands.append('ln -f -s "`pwd`/%s" "`pwd`/%s"' % (f.short_path, test_runfiles_dir + '/' + f.short_path))

    for f in server_executable_runfiles:
        sub_commands.append('mkdir -p `dirname %s`' % (server_runfiles_dir + '/' + f.short_path))
        sub_commands.append('ln -f -s "`pwd`/%s" "`pwd`/%s"' % (f.short_path, server_runfiles_dir + '/' + f.short_path))

    stdout = 'server-executable.runfiles/krpc/stdout'
    if server_type != 'serialio':
        server_args = '--type=%s' % server_type
        get_server_settings = [
            'RPC_PORT=`awk \'/rpc_port = /{print $NF}\' %s`' % stdout,
            'STREAM_PORT=`awk \'/stream_port = /{print $NF}\' %s`' % stdout,
            'echo "Server started, rpc port = $RPC_PORT, stream port = $STREAM_PORT"'
        ]
        test_env = 'RPC_PORT=$RPC_PORT STREAM_PORT=$STREAM_PORT'
    else:
        socat_stdout = 'server-executable.runfiles/krpc/socat-stdout'
        server_port = 'server-executable.runfiles/krpc/server-port'
        client_port = 'server-executable.runfiles/krpc/client-port'
        sub_commands.extend([
            '(cd server-executable.runfiles/krpc; socat -d -d PTY,raw,echo=0,link=server-port PTY,raw,echo=0,link=client-port >socat-stdout 2>&1) &',
            'SOCAT_PID=$!',
            'while ! grep "starting data transfer loop" %s >/dev/null 2>&1; do sleep 0.1 ; done' % socat_stdout,
            'echo "Virtual ports established using socat"',
            'SERVER_PORT=`pwd`/server-executable.runfiles/krpc/server-port',
            'CLIENT_PORT=`pwd`/server-executable.runfiles/krpc/client-port',
            'echo "Server port = $SERVER_PORT"',
            'echo "Client port = $CLIENT_PORT"',
        ])
        server_args = '--type=serialio --port="$SERVER_PORT"'
        get_server_settings = [
            'sleep 1',  # FIXME: hack to ensure serial port is established fully before running tests
            'echo "Server started, port = $SERVER_PORT"'
        ]
        test_env = 'PORT=$CLIENT_PORT'

    sub_commands.extend([
        '(cd server-executable.runfiles/krpc; %s %s >stdout) &' % (ctx.executable.server_executable.short_path, server_args),
        'SERVER_PID=$!',
        'while ! grep "Server started successfully" %s >/dev/null 2>&1; do sleep 0.1 ; done' % stdout
    ] + get_server_settings + [
        '(cd test-executable.runfiles/krpc/%s.runfiles/krpc; %s ../../%s)' % (ctx.executable.test_executable.short_path, test_env, ctx.executable.test_executable.basename),
        'RESULT=$?',
        'kill $SERVER_PID',
        'exit $RESULT'
    ])
    ctx.actions.write(
        output = ctx.outputs.executable,
        content = '\n'.join(sub_commands)+'\n',
        is_executable = True
    )

    return struct(
        name = ctx.label.name,
        out = ctx.outputs.executable,
        runfiles = ctx.runfiles(files = [ctx.executable.test_executable, ctx.executable.server_executable] + test_executable_runfiles + server_executable_runfiles)
    )

client_test = rule(
    implementation = _impl,
    attrs = {
        'test_executable': attr.label(executable=True, cfg='host'),
        'server_executable': attr.label(executable=True, cfg='host'),
        'server_type': attr.string(default='protobuf'),
    },
    test = True
)
