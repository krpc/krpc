"""Building a benchmark's client program the way a program that uses the client is built."""

# The settings the build is running under and the target being built are both beside the point:
# a benchmark is compiled optimized whatever else is going on, which is what the rule is for.
def _optimized_impl(_settings, _attr):
    return {"//command_line_option:compilation_mode": "opt"}

_optimized = transition(
    implementation = _optimized_impl,
    inputs = [],
    outputs = ["//command_line_option:compilation_mode"],
)

def _optimized_binary_impl(ctx):
    binary = ctx.attr.binary[0] if type(ctx.attr.binary) == "list" else ctx.attr.binary
    executable = binary[DefaultInfo].files_to_run.executable
    out = ctx.actions.declare_file(ctx.label.name)
    ctx.actions.symlink(output = out, target_file = executable, is_executable = True)
    return [DefaultInfo(
        executable = out,
        runfiles = ctx.runfiles([executable]).merge(binary[DefaultInfo].default_runfiles),
    )]

optimized_binary = rule(
    implementation = _optimized_binary_impl,
    doc = """A binary, built optimized whatever mode the build itself is in.

    A client benchmark measures what a client costs the program that uses it, and such a
    program is compiled with optimization. Bazel builds in fastbuild unless told otherwise,
    which for C and C++ means no optimization at all, so a benchmark left to the default would
    report what a build nobody runs costs - several times what the client actually spends per
    value encoded or decoded, which is the part of it a benchmark is there to see.
    """,
    attrs = {
        "binary": attr.label(
            executable = True,
            cfg = _optimized,
            mandatory = True,
            doc = "The program to build optimized.",
        ),
    },
    executable = True,
)
