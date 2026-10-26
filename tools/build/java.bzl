" Java build tools "

load("@bazel_skylib//rules:build_test.bzl", "build_test")
load("@rules_java//java/common:java_common.bzl", "java_common")
load("@rules_java//java/common:java_info.bzl", "JavaInfo")
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

# Every check but the one for missing documentation, which asks for a comment, a @param, a
# @return and a @throws on every public member
_DOCLINT = "-Xdoclint:all,-missing"

def _javadoc_impl(ctx):
    runtime = ctx.attr._java_runtime[java_common.JavaRuntimeInfo]
    java = runtime.java_executable_exec_path
    javadoc = java[:java.rfind("/") + 1] + "javadoc" + (".exe" if java.endswith(".exe") else "")
    classpath = depset(transitive = [dep[JavaInfo].transitive_compile_time_jars for dep in ctx.attr.deps])
    stamp = ctx.actions.declare_file(ctx.label.name + ".stamp")
    html = ctx.actions.declare_directory(ctx.label.name + ".html")

    args = ctx.actions.args()
    args.add(stamp.path)
    args.add(javadoc)
    args.add_all(["-quiet", "-Werror", _DOCLINT])

    # Report every problem, rather than the first hundred
    args.add_all(["-Xmaxerrs", "100000", "-Xmaxwarns", "100000"])
    args.add("-d", html.path)
    args.add_joined("-classpath", classpath, join_with = ctx.configuration.host_path_separator)
    args.add_all(ctx.files.srcs)

    ctx.actions.run(
        executable = ctx.executable._runner,
        arguments = [args],
        inputs = depset(direct = ctx.files.srcs, transitive = [classpath, runtime.files]),
        outputs = [stamp, html],
        mnemonic = "Javadoc",
        progress_message = "Running javadoc on %s" % ctx.label,
    )
    return [DefaultInfo(files = depset([stamp]))]

# javadoc runs as a build action, from the JDK as its repository holds it. A JDK run from a
# test's runfiles tree on Windows cannot load its modules.
_javadoc = rule(
    implementation = _javadoc_impl,
    attrs = {
        "srcs": attr.label_list(allow_files = [".java"]),
        "deps": attr.label_list(providers = [JavaInfo]),
        "_java_runtime": attr.label(
            default = Label("@rules_java//toolchains:current_host_java_runtime"),
            cfg = "exec",
            providers = [java_common.JavaRuntimeInfo],
        ),
        "_runner": attr.label(
            default = Label("//tools/build:run_and_stamp"),
            executable = True,
            cfg = "exec",
        ),
    },
)

def javadoc_test(name, srcs, deps, **kwargs):
    """A test that javadoc documents the given sources without a warning.

    Args:
        name: the target name.
        srcs: the sources.
        deps: the libraries the sources use.
        **kwargs: passed to the test.
    """
    check = name + ".check"
    _javadoc(
        name = check,
        srcs = srcs,
        deps = deps,
        tags = ["manual"],
        testonly = True,
    )
    build_test(
        name = name,
        targets = [":" + check],
        **kwargs
    )
