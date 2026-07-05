" C++ build tools "

load("@rules_python//python:defs.bzl", _native_py_test = "py_test")

def _check_impl(ctx):
    srcs = ctx.files.srcs
    hdrs = ctx.files.hdrs
    includes = ctx.files.includes
    runfiles = srcs + hdrs

    args = ["--enable=all", "--suppress=missingIncludeSystem", "--inline-suppr", "--error-exitcode=1", "--check-config"]
    args.extend(["-I%s" % x.short_path for x in includes])
    args.extend([x.short_path for x in srcs])

    ctx.actions.write(
        ctx.outputs.executable,
        "/usr/bin/cppcheck %s\n" % " ".join(args),
    )

    return DefaultInfo(
        executable = ctx.outputs.executable,
        runfiles = ctx.runfiles(files = runfiles),
    )

cpp_check_test = rule(
    implementation = _check_impl,
    attrs = {
        "srcs": attr.label_list(allow_files = True),
        "hdrs": attr.label_list(allow_files = True),
        "includes": attr.label_list(allow_files = True),
    },
    test = True,
)

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
