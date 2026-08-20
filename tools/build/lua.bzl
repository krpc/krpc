" lua build tools "

load("@rules_python//python:defs.bzl", "py_binary", "py_test")
load("//tools/build:pkg.bzl", "apply_path_map")

LuaInfo = provider(
    doc = "A lua module tree, named by its path within the runfiles.",
    fields = {"root": "the directory require should search"},
)

def _repo_relative(file):
    """The path of a file within the repository it came from.

    A file from an external repository has a short_path of '../<canonical name>/<path>',
    and that canonical name changes when bazel or a MODULE.bazel dependency is upgraded,
    so it is no basis for a path mapping.
    """
    if file.short_path.startswith("../"):
        return file.short_path.split("/", 2)[2]
    return file.short_path

def _lua_library_impl(ctx):
    files = []
    for src in ctx.files.srcs:
        path = apply_path_map(ctx.attr.path_map, _repo_relative(src))

        # A symlink rather than a copy keeps staging cheap and OS-independent.
        out = ctx.actions.declare_file(ctx.label.name + "/" + path)
        ctx.actions.symlink(output = out, target_file = src)
        files.append(out)
    return [
        DefaultInfo(files = depset(files), runfiles = ctx.runfiles(files = files)),
        LuaInfo(root = ctx.label.package + "/" + ctx.label.name),
    ]

lua_library = rule(
    doc = """Lays a set of lua sources out as a module tree.

    require finds a module at the path its name spells, which is rarely the path the
    source sits at, so each file is mapped to where the module it holds belongs. These
    are the mappings a rockspec states in its build.modules table.""",
    implementation = _lua_library_impl,
    attrs = {
        "path_map": attr.string_dict(),
        "srcs": attr.label_list(allow_files = True),
    },
)

def _main_impl(ctx):
    # The launcher resolves its paths against its own location, so it needs to know how
    # deep in the runfiles it sits. The rest are paths only a rule can ask for: location
    # expansion resolves a label to the files it builds, and neither the interpreter nor
    # a module tree is a single file.
    depth = len(ctx.label.package.split("/")) if ctx.label.package else 0
    ctx.actions.expand_template(
        output = ctx.outputs.out,
        template = ctx.file._template,
        substitutions = {
            "@ARGS@": " ".join(ctx.attr.script_args),
            "@INTERPRETER@": ctx.executable._interpreter.short_path,
            "@ROOTS@": " ".join([dep[LuaInfo].root for dep in ctx.attr.deps]),
            "@RUNFILES@": "/".join([".."] * depth) or ".",
            "@SCRIPT@": ctx.file.script.short_path,
        },
    )

_main = rule(
    implementation = _main_impl,
    attrs = {
        "deps": attr.label_list(providers = [LuaInfo]),
        "out": attr.output(mandatory = True),
        "script": attr.label(allow_single_file = True),
        "script_args": attr.string_list(),
        "_interpreter": attr.label(
            cfg = "target",
            default = Label("//tools/build/lua:lua"),
            executable = True,
        ),
        "_template": attr.label(
            allow_single_file = True,
            default = Label("//tools/build/lua:run_lua.py.tmpl"),
        ),
    },
)

def _lua_program(build, name, main, deps, script_args = [], **kwargs):
    """A python program running a lua script against the module trees in deps."""
    generated = name + "_main.py"
    _main(
        name = name + "-main",
        out = generated,
        script = main,
        script_args = script_args,
        deps = deps,
    )
    build(
        name = name,
        srcs = [generated],
        data = deps + [main, Label("//tools/build/lua:lua")],
        main = generated,
        **kwargs
    )

# buildifier: disable=function-docstring
def lua_test(name, main, deps, **kwargs):
    # -v so a failure names the test that produced it and not just the assertion.
    _lua_program(
        py_test,
        name,
        main,
        deps + [Label("//tools/build/lua:luaunit")],
        script_args = ["-v"],
        **kwargs
    )

# buildifier: disable=function-docstring
def lua_binary(name, main, deps, **kwargs):
    # As lua_test, but runs the given script instead of a test suite.
    _lua_program(py_binary, name, main, deps, **kwargs)
