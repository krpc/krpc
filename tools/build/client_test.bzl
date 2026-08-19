" client test tools "

load("@rules_python//python:defs.bzl", "py_test")

def _config_impl(ctx):
    config = ctx.actions.declare_file(ctx.label.name + ".json")
    ctx.actions.write(
        output = config,
        content = json.encode({
            "server": ctx.executable.server_executable.short_path,
            "test": ctx.executable.test_executable.short_path,
            "server_type": ctx.attr.server_type,
        }),
    )
    return DefaultInfo(files = depset([config]))

# The harness needs the path of each executable within the runfiles tree, which
# only a rule can ask for; location expansion resolves a label to the files it
# builds, and neither the server nor a test executable is a single file.
_client_test_config = rule(
    implementation = _config_impl,
    attrs = {
        "test_executable": attr.label(executable = True, cfg = "target"),
        "server_executable": attr.label(executable = True, cfg = "target"),
        "server_type": attr.string(default = "protobuf"),
    },
)

# buildifier: disable=function-docstring
def client_test(
        name,
        test_executable,
        server_executable,
        server_type = "protobuf",
        target_compatible_with = None,
        **kwargs):
    # The serial transport is tested over a pair of pseudo-terminals that socat
    # links together, and socat has no Windows build. The other transports need
    # nothing beyond the server and the client, so they run anywhere.
    if server_type == "serialio":
        target_compatible_with = (target_compatible_with or []) + ["@platforms//os:linux"]

    config = name + "-config"
    _client_test_config(
        name = config,
        test_executable = test_executable,
        server_executable = server_executable,
        server_type = server_type,
        testonly = True,
    )
    py_test(
        name = name,
        srcs = [Label("//tools/build:run_client_test.py")],
        main = Label("//tools/build:run_client_test.py"),
        args = ["$(rootpath :%s)" % config],
        data = [
            config,
            test_executable,
            server_executable,
        ],
        target_compatible_with = target_compatible_with,
        **kwargs
    )
