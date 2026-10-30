" conformance test tools "

load("@rules_python//python:defs.bzl", "py_test")

def _config_impl(ctx):
    config = ctx.actions.declare_file(ctx.label.name + ".json")
    ctx.actions.write(
        output = config,
        content = json.encode({
            "testee": ctx.executable.testee.short_path,
            "server": ctx.executable.server.short_path,
            "protocol": ctx.attr.protocol,
            "expected_failures": ctx.file.expected_failures.short_path,
            "unsupported": ctx.file.unsupported.short_path if ctx.file.unsupported else "",
            "profiles": ctx.file.profiles.short_path,
            "conventions": ctx.file.conventions.short_path if ctx.file.conventions else "",
        }),
    )
    return DefaultInfo(files = depset([config]))

def _profiles_impl(ctx):
    profiles = ctx.actions.declare_file(ctx.label.name + ".txt")
    ctx.actions.write(output = profiles, content = "".join([x + "\n" for x in ctx.attr.names]))
    return DefaultInfo(files = depset([profiles]))

# The profiles and protocols a client implements, one per line, for the driver and the coverage
# table
_conformance_profiles = rule(
    implementation = _profiles_impl,
    attrs = {"names": attr.string_list()},
)

# The driver needs the path of each executable within the runfiles tree, as for client_test
_conformance_test_config = rule(
    implementation = _config_impl,
    attrs = {
        "testee": attr.label(executable = True, cfg = "target"),
        "server": attr.label(executable = True, cfg = "target"),
        "protocol": attr.string(),
        "expected_failures": attr.label(allow_single_file = True),
        "unsupported": attr.label(allow_single_file = True),
        "profiles": attr.label(allow_single_file = True),
        "conventions": attr.label(allow_single_file = True),
    },
)

def conformance_test(
        name,
        testee,
        expected_failures,
        profiles,
        unsupported = None,
        conventions = None,
        protocols = ["tcpip"],
        protocol_testees = {},
        **kwargs):
    """Runs the conformance tests against a client's testee, once per protocol.

    Args:
      name: The prefix of the test names, which end with the protocol.
      testee: The testee executable.
      expected_failures: The file listing the tests the client fails.
      profiles: The profiles the client implements. The tests' protocols are added to them,
        as the target <name>-profiles.
      unsupported: The file listing the tests the client's language has no way to carry out.
      conventions: The file of the client's native error types and naming, for a client with
        the error-types or dynamic profile.
      protocols: The protocols to test over.
      protocol_testees: The testee for a protocol whose library is built for it alone, by
        protocol.
      **kwargs: Passed to each test.
    """
    server = Label("//tools/TestServer")
    _conformance_profiles(
        name = name + "-profiles",
        names = profiles + protocols,
        visibility = [Label("//client/conformance:__pkg__")],
    )
    profiles = ":%s-profiles" % name
    tests = []
    for protocol in protocols:
        test = name + "-" + protocol
        config = test + "-config"
        protocol_testee = protocol_testees.get(protocol, testee)
        test_kwargs = dict(kwargs)

        # The serial line is a pair of pseudo-terminals that socat links, as for client_test
        if protocol == "serialio":
            test_kwargs["target_compatible_with"] = ["@platforms//os:linux"]
        _conformance_test_config(
            name = config,
            testee = protocol_testee,
            server = server,
            protocol = protocol,
            expected_failures = expected_failures,
            unsupported = unsupported,
            profiles = profiles,
            conventions = conventions,
            testonly = True,
        )
        py_test(
            name = test,
            srcs = [Label("//client/conformance:driver/main.py")],
            main = Label("//client/conformance:driver/main.py"),
            args = ["--config=$(rootpath :%s)" % config],
            data = [
                config,
                expected_failures,
                profiles,
                server,
                protocol_testee,
            ] + ([unsupported] if unsupported else []) + ([conventions] if conventions else []),
            deps = [Label("//client/conformance:driver")],
            **test_kwargs
        )
        tests.append(test)
    native.test_suite(name = name, tests = tests)
