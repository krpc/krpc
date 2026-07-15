" client test tools "

def _impl(ctx):
    server_type = ctx.attr.server_type

    test_executable_runfiles = ctx.attr.test_executable.default_runfiles.files.to_list()
    server_executable_runfiles = ctx.attr.server_executable.default_runfiles.files.to_list()

    sub_commands = [
        # rules_dotnet binaries locate their runfiles (the dotnet runtime and
        # assemblies) with the bash runfiles library, resolved via
        # RUNFILES_DIR, which must point at this test's runfiles tree
        'export RUNFILES_DIR="${RUNFILES_DIR:-$TEST_SRCDIR}"',
        "ORIG_DIR=`pwd`",
        # The test executable is staged into a nested runfiles tree, as the
        # executable may be a wrapper script that expects to find its runfiles
        # at $0.runfiles
        "mkdir -p `dirname test-executable.runfiles/_main/%s`" % ctx.executable.test_executable.short_path,
        'ln -f -s "`pwd`/%s" "`pwd`/test-executable.runfiles/_main/%s"' % (ctx.executable.test_executable.short_path, ctx.executable.test_executable.short_path),
    ]

    test_runfiles_dir = "test-executable.runfiles/_main/" + ctx.executable.test_executable.short_path + ".runfiles/_main"

    for f in test_executable_runfiles:
        sub_commands.append("mkdir -p `dirname %s`" % (test_runfiles_dir + "/" + f.short_path))
        sub_commands.append('ln -f -s "`pwd`/%s" "`pwd`/%s"' % (f.short_path, test_runfiles_dir + "/" + f.short_path))

    stdout = "server-stdout"
    if server_type != "serialio":
        server_args = "--type=%s" % server_type
        get_server_settings = [
            "RPC_PORT=`awk '/rpc_port = /{print $NF}' %s`" % stdout,
            "STREAM_PORT=`awk '/stream_port = /{print $NF}' %s`" % stdout,
            'echo "Server started, rpc port = $RPC_PORT, stream port = $STREAM_PORT"',
        ]
        test_env = "RPC_PORT=$RPC_PORT STREAM_PORT=$STREAM_PORT"
    else:
        socat_stdout = "socat-stdout"
        sub_commands.extend([
            "mkdir -p serial-ports",
            "(cd serial-ports; socat -d -d PTY,raw,echo=0,link=server-port PTY,raw,echo=0,link=client-port >../%s 2>&1) &" % socat_stdout,
            "SOCAT_PID=$!",
            'while ! grep "starting data transfer loop" %s >/dev/null 2>&1; do sleep 0.1 ; done' % socat_stdout,
            'echo "Virtual ports established using socat"',
            "SERVER_PORT=`pwd`/serial-ports/server-port",
            "CLIENT_PORT=`pwd`/serial-ports/client-port",
            'echo "Server port = $SERVER_PORT"',
            'echo "Client port = $CLIENT_PORT"',
        ])
        server_args = '--type=serialio --port="$SERVER_PORT"'
        get_server_settings = [
            "sleep 1",  # FIXME: hack to ensure serial port is established fully before running tests
            'echo "Server started, port = $SERVER_PORT"',
        ]
        test_env = "PORT=$CLIENT_PORT"

    sub_commands.extend([
        'echo "" > %s' % stdout,
        # Run the server directly from this test's runfiles tree; its
        # launcher finds the dotnet runtime and assemblies via RUNFILES_DIR,
        # and the dotnet host's relative probing paths resolve the
        # runfiles-root-relative assembly paths in its deps.json from the
        # working directory $RUNFILES_DIR/_main
        '(cd "$RUNFILES_DIR/_main"; %s %s >> "$ORIG_DIR/%s") &' % (ctx.executable.server_executable.short_path, server_args, stdout),
        "SERVER_PID=$!",
        'tail -n0 -f %s | sed "/Server started successfully/ q"' % stdout,
    ] + get_server_settings + [
        "(cd test-executable.runfiles/_main/%s.runfiles/_main; %s ../../%s)" % (ctx.executable.test_executable.short_path, test_env, ctx.executable.test_executable.basename),
        "RESULT=$?",
        "kill $SERVER_PID",
        "exit $RESULT",
    ])
    ctx.actions.write(
        output = ctx.outputs.executable,
        content = "\n".join(sub_commands) + "\n",
        is_executable = True,
    )

    return DefaultInfo(
        executable = ctx.outputs.executable,
        runfiles = ctx.runfiles(files = [ctx.executable.test_executable, ctx.executable.server_executable] + test_executable_runfiles + server_executable_runfiles).merge(ctx.attr._bash_runfiles[DefaultInfo].default_runfiles),
    )

_client_test = rule(
    implementation = _impl,
    attrs = {
        "test_executable": attr.label(executable = True, cfg = "exec"),
        "server_executable": attr.label(executable = True, cfg = "exec"),
        "server_type": attr.string(default = "protobuf"),
        "_bash_runfiles": attr.label(default = Label("@bazel_tools//tools/bash/runfiles")),
    },
    test = True,
)

# buildifier: disable=function-docstring
def client_test(**kwargs):
    # The integration harness is a generated bash script that drives TestServer
    # (and socat for the serial transport), so it is Linux-only by design.
    _client_test(
        target_compatible_with = ["@platforms//os:linux"],
        **kwargs
    )
