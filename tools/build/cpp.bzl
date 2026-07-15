" C++ build tools "

load("@bazel_skylib//rules:build_test.bzl", "build_test")
load("@rules_cc//cc/common:cc_common.bzl", "cc_common")
load("@rules_cc//cc/common:cc_info.bzl", "CcInfo")

# The lint rules below run a hermetic LLVM tool (clang-tidy/clang-format) as a
# build action and emit a stamp file on success; a build_test wrapper turns
# "the stamp builds" into a test. The tool is invoked through the run_and_stamp
# python helper (not a shell `&& touch`) so the rules work on any host OS.

def _clang_tidy_impl(ctx):
    cc = cc_common.merge_cc_infos(
        cc_infos = [d[CcInfo] for d in ctx.attr.deps],
    ).compilation_context

    tidy = ctx.files._clang_tidy[0]
    config = ctx.file.config

    flags = list(ctx.attr.copts)
    flags += ["-I" + d for d in cc.includes.to_list()]
    flags += ["-iquote" + d for d in cc.quote_includes.to_list()]
    flags += ["-isystem" + d for d in cc.system_includes.to_list()]
    flags += ["-D" + d for d in cc.defines.to_list()]

    srcs = ctx.files.srcs
    stamp = ctx.actions.declare_file(ctx.label.name + ".stamp")

    # clang-tidy resolves the CcInfo include dirs (exec-root relative) and the
    # source paths from the exec root, so this must run as a build action rather
    # than from a test's runfiles.
    args = ctx.actions.args()
    args.add(stamp.path)
    args.add(tidy.path)
    args.add("--config-file=" + config.path)
    args.add("--quiet")
    args.add("--warnings-as-errors=*")
    args.add_all([s.path for s in srcs])
    args.add("--")
    args.add_all(flags)

    ctx.actions.run(
        executable = ctx.executable._runner,
        arguments = [args],
        inputs = depset(
            direct = srcs + [config],
            transitive = [cc.headers, depset(ctx.files._clang_tidy + ctx.files._llvm_includes)],
        ),
        outputs = [stamp],
        mnemonic = "ClangTidy",
        progress_message = "Running clang-tidy on %s" % ctx.label,
    )
    return [DefaultInfo(files = depset([stamp]))]

_clang_tidy = rule(
    implementation = _clang_tidy_impl,
    attrs = {
        "srcs": attr.label_list(allow_files = [".c", ".cc", ".cpp"]),
        "deps": attr.label_list(providers = [CcInfo]),
        "copts": attr.string_list(),
        "config": attr.label(
            allow_single_file = True,
            default = Label("//:.clang-tidy"),
        ),
        "_runner": attr.label(
            default = Label("//tools/build:run_and_stamp"),
            executable = True,
            cfg = "exec",
        ),
        "_clang_tidy": attr.label(
            default = Label("//tools/build/llvm:clang-tidy"),
            allow_files = True,
        ),
        "_llvm_includes": attr.label(
            default = Label("//tools/build/llvm:include"),
            allow_files = True,
        ),
    },
)

# The hermetic clang-tidy/clang-format tools and libc++ :include come from the
# LLVM toolchain's Bazel targets, which toolchains_llvm only declares on Linux;
# there is no system-llvm fallback on Windows. So the lint rules are Linux-only,
# and a Windows `bazel build //...` skips them.
_LINT_ONLY_ON = ["@platforms//os:linux"]

# buildifier: disable=function-docstring
def clang_tidy_test(name, srcs, deps, copts = [], config = None, **kwargs):
    check = name + ".check"
    _clang_tidy(
        name = check,
        srcs = srcs,
        deps = deps,
        copts = copts,
        config = config or Label("//:.clang-tidy"),
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

def _clang_format_impl(ctx):
    fmt = ctx.files._clang_format[0]
    config = ctx.file.config
    srcs = ctx.files.srcs
    stamp = ctx.actions.declare_file(ctx.label.name + ".stamp")

    # --dry-run --Werror exits non-zero (and prints the diff) if any file is not
    # already formatted.
    args = ctx.actions.args()
    args.add(stamp.path)
    args.add(fmt.path)
    args.add("--dry-run")
    args.add("--Werror")
    args.add("--style=file:" + config.path)
    args.add_all([s.path for s in srcs])

    ctx.actions.run(
        executable = ctx.executable._runner,
        arguments = [args],
        inputs = depset(direct = srcs + [config] + ctx.files._clang_format),
        outputs = [stamp],
        mnemonic = "ClangFormat",
        progress_message = "Checking clang-format on %s" % ctx.label,
    )
    return [DefaultInfo(files = depset([stamp]))]

_clang_format = rule(
    implementation = _clang_format_impl,
    attrs = {
        "srcs": attr.label_list(allow_files = [".c", ".cc", ".cpp", ".h", ".hpp"]),
        "config": attr.label(
            allow_single_file = True,
            default = Label("//:.clang-format"),
        ),
        "_runner": attr.label(
            default = Label("//tools/build:run_and_stamp"),
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
def clang_format_test(name, srcs, config = None, **kwargs):
    check = name + ".check"
    _clang_format(
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
