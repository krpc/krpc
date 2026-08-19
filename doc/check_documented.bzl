" Test that the documented API members exactly match an expected list "

load("@rules_python//python:defs.bzl", "py_test")

def _config_impl(ctx):
    # The API filegroups carry more than the member lists; only the
    # .documented.txt files list the members that were actually documented.
    documented = [
        src
        for src in ctx.files.srcs
        if src.short_path.endswith(".documented.txt")
    ]
    config = ctx.actions.declare_file(ctx.label.name + ".json")
    ctx.actions.write(
        output = config,
        content = json.encode({
            "expected": ctx.file.members.short_path,
            "actual": [src.short_path for src in documented],
        }),
    )
    return DefaultInfo(files = depset([config]))

# Which of the files the documentation build emits hold the documented members
# is settled when the graph is built rather than when the test runs, so the
# paths are written down for it to read.
_check_documented_config = rule(
    implementation = _config_impl,
    attrs = {
        "members": attr.label(allow_single_file = True),
        "srcs": attr.label_list(allow_files = True),
    },
)

# buildifier: disable=function-docstring
def check_documented_test(name, members, srcs, **kwargs):
    config = name + "-config"
    _check_documented_config(
        name = config,
        members = members,
        srcs = srcs,
        testonly = True,
    )
    py_test(
        name = name,
        srcs = [Label("//doc:check_documented.py")],
        main = Label("//doc:check_documented.py"),
        args = ["$(rootpath :%s)" % config],
        data = [config, members] + srcs,
        # What is documented is a property of the sources rather than of the
        # platform a build runs on, so checking it once is enough.
        target_compatible_with = ["@platforms//os:linux"],
        **kwargs
    )
