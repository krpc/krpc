" C++ build tools "

load("@rules_cc//cc/common:cc_common.bzl", "cc_common")
load("@rules_cc//cc/common:cc_info.bzl", "CcInfo")
load("@rules_python//python:defs.bzl", _native_py_test = "py_test")

# buildifier: disable=function-docstring
def cpp_lint_test(
        name,
        srcs = [],
        hdrs = [],
        includes = [],
        extra_files = [],
        filters = ["+build/include_alpha"],
        **kwargs):
    args = ["--linelength=100"]
    if filters:
        args.append("--filter=%s" % ",".join(filters))
    args.extend(["$(rootpath %s)" % x for x in srcs + hdrs])

    _native_py_test(
        name = name,
        srcs = [Label("//tools/build/python:run_cpplint.py")],
        main = Label("//tools/build/python:run_cpplint.py"),
        args = args,
        data = srcs + hdrs + extra_files,
        deps = ["@pypi//cpplint"],
        **kwargs
    )

def _clang_tidy_test_impl(ctx):
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
    # than from the test's runfiles. The stamp ties the action into the test:
    # `bazel test` stages the stamp into the runfiles, which forces the action to
    # run, and a non-zero clang-tidy exit fails the build (hence the test).
    cmd = " ".join(
        [
            tidy.path,
            "--config-file=" + config.path,
            "--quiet",
            "--warnings-as-errors=*",
        ] +
        [s.path for s in srcs] +
        ["--"] + flags,
    ) + " && touch " + stamp.path

    ctx.actions.run_shell(
        inputs = depset(
            direct = srcs + [config, tidy],
            transitive = [cc.headers, depset(ctx.files._llvm_includes)],
        ),
        outputs = [stamp],
        command = cmd,
        mnemonic = "ClangTidy",
        progress_message = "Running clang-tidy on %s" % ctx.label,
    )

    launcher = ctx.actions.declare_file(ctx.label.name + ".sh")
    ctx.actions.write(launcher, "#!/bin/sh\nexit 0\n", is_executable = True)
    return [DefaultInfo(
        executable = launcher,
        runfiles = ctx.runfiles(files = [stamp]),
    )]

clang_tidy_test = rule(
    doc = "Runs the hermetic clang-tidy over srcs, using the compilation " +
          "context of deps to resolve includes/defines.",
    implementation = _clang_tidy_test_impl,
    test = True,
    attrs = {
        "srcs": attr.label_list(allow_files = [".c", ".cc", ".cpp"]),
        "deps": attr.label_list(providers = [CcInfo]),
        "copts": attr.string_list(),
        "config": attr.label(
            allow_single_file = True,
            default = Label("//:.clang-tidy"),
        ),
        "_clang_tidy": attr.label(
            default = Label("@llvm_toolchain_llvm//:clang-tidy"),
            allow_files = True,
        ),
        "_llvm_includes": attr.label(
            default = Label("@llvm_toolchain_llvm//:include"),
            allow_files = True,
        ),
    },
)
