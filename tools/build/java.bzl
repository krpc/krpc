" Java build tools "

load("@rules_python//python:defs.bzl", "py_test")

def _config_impl(ctx):
    config = ctx.actions.declare_file(ctx.label.name + ".json")
    ctx.actions.write(
        output = config,
        content = json.encode({
            "checkstyle": ctx.executable.checkstyle.short_path,
            "properties": ctx.file.properties.short_path,
            "srcs": [src.short_path for src in ctx.files.srcs],
        }),
    )
    return DefaultInfo(files = depset([config]))

# checkstyle is a java_binary, whose label names both a launcher and the jars it
# runs, so the path of the launcher within the runfiles tree is not something
# location expansion can resolve; only a rule can ask for it.
_java_checkstyle_config = rule(
    implementation = _config_impl,
    attrs = {
        "checkstyle": attr.label(
            default = Label("//tools/build/checkstyle"),
            executable = True,
            cfg = "target",
        ),
        "properties": attr.label(allow_single_file = True),
        "srcs": attr.label_list(allow_files = True),
    },
)

# buildifier: disable=function-docstring
def java_checkstyle_test(
        name,
        srcs,
        properties = Label("//tools/build/checkstyle:default.properties"),
        **kwargs):
    config = name + "-config"
    _java_checkstyle_config(
        name = config,
        properties = properties,
        srcs = srcs,
        testonly = True,
    )
    py_test(
        name = name,
        srcs = [Label("//tools/build:run_checkstyle.py")],
        main = Label("//tools/build:run_checkstyle.py"),
        args = ["$(rootpath :%s)" % config],
        data = [
            config,
            properties,
            Label("//tools/build/checkstyle"),
        ] + srcs,
        **kwargs
    )
