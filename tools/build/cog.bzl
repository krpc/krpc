"""Test that cog-generated sources are up to date.

Some C/C++ client sources embed cog (https://cog.readthedocs.io) generator blocks
that expand into repetitive code committed alongside the generators. This test
regenerates each source and reformats it with clang-format -- the same steps that
produced the committed output -- and fails if the result differs, so a generator
block edited without regenerating is caught in CI. cog itself is run manually.
"""

load("@bazel_skylib//rules:build_test.bzl", "build_test")

_LINT_ONLY_ON = ["@platforms//os:linux"]

def _cog_impl(ctx):
    fmt = ctx.files._clang_format[0]
    config = ctx.file.config
    srcs = ctx.files.srcs
    stamp = ctx.actions.declare_file(ctx.label.name + ".stamp")

    args = ctx.actions.args()
    args.add(stamp.path)
    args.add(fmt.path)
    args.add(config.path)
    args.add_all([s.path for s in srcs])

    ctx.actions.run(
        executable = ctx.executable._runner,
        arguments = [args],
        inputs = depset(direct = srcs + [config] + ctx.files._clang_format),
        outputs = [stamp],
        mnemonic = "CogCheck",
        progress_message = "Checking cog output is up to date on %s" % ctx.label,
    )
    return [DefaultInfo(files = depset([stamp]))]

_cog = rule(
    implementation = _cog_impl,
    attrs = {
        "srcs": attr.label_list(allow_files = [".c", ".cc", ".cpp", ".h", ".hpp"]),
        "config": attr.label(
            allow_single_file = True,
            default = Label("//:.clang-format"),
        ),
        "_runner": attr.label(
            default = Label("//tools/build:run_cog_check"),
            executable = True,
            cfg = "exec",
        ),
        "_clang_format": attr.label(
            default = Label("//tools/build/llvm:clang-format"),
            allow_files = True,
        ),
    },
)

# buildifier: disable=function-docstring
def cog_test(name, srcs, config = None, **kwargs):
    check = name + ".check"
    _cog(
        name = check,
        srcs = srcs,
        config = config or Label("//:.clang-format"),
        tags = ["manual"],
        testonly = True,
        target_compatible_with = _LINT_ONLY_ON,
    )
    build_test(
        name = name,
        targets = [":" + check],
        target_compatible_with = _LINT_ONLY_ON,
        **kwargs
    )
