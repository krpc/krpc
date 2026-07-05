" C++ build tools "

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
