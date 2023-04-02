" client test tools "

def _impl(ctx):
    server_type = ctx.attr.server_type

    script = []
    server_log = "server_log.txt"

    if server_type != "serialio":
        server_args = "--type=%s" % server_type
        get_server_settings = [
            "RPC_PORT=`awk '/rpc_port = /{print $NF}' %s`" % server_log,
            "STREAM_PORT=`awk '/stream_port = /{print $NF}' %s`" % server_log,
            'echo "Server started, rpc port = $RPC_PORT, stream port = $STREAM_PORT"',
        ]
        test_env = "RPC_PORT=$RPC_PORT STREAM_PORT=$STREAM_PORT"
    else:
        script.extend([
            "socat -d -d PTY,raw,echo=0,link=server-port PTY,raw,echo=0,link=client-port >socat_log.txt 2>&1) &",
            "SOCAT_PID=$!",
            'while ! grep "starting data transfer loop" socat_log.txt >/dev/null 2>&1; do sleep 0.1 ; done',
            'echo "Virtual ports established using socat"',
            "SERVER_PORT=server-port",
            "CLIENT_PORT=client-port",
            'echo "Server port = $SERVER_PORT"',
            'echo "Client port = $CLIENT_PORT"',
        ])
        server_args = '--type=serialio --port="$SERVER_PORT"'
        get_server_settings = [
            "sleep 1",  # FIXME: hack to ensure serial port is established fully before running tests
            'echo "Server started, port = $SERVER_PORT"',
        ]
        test_env = "PORT=$CLIENT_PORT"

    script.extend([
        "set -e",
        'echo "" > %s' % server_log,
        "%s %s >> %s &" % (ctx.executable.server_executable.short_path, server_args, server_log),
        "SERVER_PID=$!",
        'tail -n0 -f %s | sed "/Server started successfully/ q"' % server_log,
    ] + get_server_settings + [
        "%s %s" % (test_env, ctx.executable.test_executable.short_path),
        "RESULT=$?",
        "kill $SERVER_PID",
        "exit $RESULT",
    ])
    script = "\n".join(script)

    ctx.actions.write(
        output = ctx.outputs.executable,
        content = script,
    )

    runfiles = ctx.runfiles(files = [
        ctx.executable.server_executable,
        ctx.executable.test_executable,
    ])
    runfiles = runfiles.merge(ctx.attr.server_executable[DefaultInfo].default_runfiles)
    runfiles = runfiles.merge(ctx.attr.test_executable[DefaultInfo].default_runfiles)

    return [DefaultInfo(runfiles = runfiles)]

client_test = rule(
    implementation = _impl,
    attrs = {
        "test_executable": attr.label(executable = True, cfg = "exec"),
        "server_executable": attr.label(executable = True, cfg = "exec"),
        "server_type": attr.string(default = "protobuf"),
    },
    test = True,
)
