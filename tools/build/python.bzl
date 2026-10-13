"python build tools"

load("@rules_python//python:defs.bzl", _native_py_test = "py_test")

# buildifier: disable=function-docstring-header
def _apply_path_map(path_map, path):
    """Apply the path mappings to a path.
    Replaces the longest prefix match from the mapping."""
    matchlen = 0
    match = path
    for x, y in path_map.items():
        if path.startswith(x):
            if len(x) > matchlen:
                match = y + path[len(x):]
                matchlen = len(x)
    return match

def _sdist_impl(ctx):
    output = ctx.outputs.out
    inputs = ctx.files.files
    path_map = ctx.attr.path_map

    # Symlink all the files to a staging directory
    # to get the required directory structure in the archive
    staging_dir = output.basename + ".py-sdist-tmp"
    staging_inputs = []
    for input in inputs:
        staging_path = staging_dir + "/" + _apply_path_map(path_map, input.short_path)
        staging_file = ctx.actions.declare_file(staging_path)
        ctx.actions.symlink(output = staging_file, target_file = input)
        staging_inputs.append(staging_file)

    # Build the sdist from the staging directory. build_sdist dereferences the
    # staged symlinks into the build dir, runs hatchling there and copies out the
    # tarball -- cross-platform, no shell.
    staging_dir_path = output.path + ".py-sdist-tmp"
    build_dir_path = staging_dir_path + ".deref"

    hatchling = ctx.executable._hatchling
    args = ctx.actions.args()
    args.add("--staging", staging_dir_path)
    args.add("--build", build_dir_path)
    args.add("--hatchling", hatchling.path)
    args.add("--out", output.path)

    ctx.actions.run(
        executable = ctx.executable._sdist_builder,
        arguments = [args],
        inputs = staging_inputs,
        outputs = [output],
        tools = [ctx.attr._hatchling[DefaultInfo].files_to_run],
        progress_message = "Packaging files into %s" % output.short_path,
        mnemonic = "PySdist",
    )

py_sdist = rule(
    implementation = _sdist_impl,
    attrs = {
        "files": attr.label_list(allow_files = True, mandatory = True, allow_empty = True),
        "path_map": attr.string_dict(),
        "out": attr.output(mandatory = True),
        "_hatchling": attr.label(
            default = Label("//tools/build/python:hatchling"),
            executable = True,
            cfg = "exec",
        ),
        "_sdist_builder": attr.label(
            default = Label("//tools/build/python:build_sdist"),
            executable = True,
            cfg = "exec",
        ),
    },
)

# buildifier: disable=function-docstring
def py_test(name, src, pkg, tests = [], deps = [], **kwargs):
    # The test parameters are baked into a generated test script, rather than
    # passed as arguments, as the test may be invoked via the client_test
    # harness which does not forward arguments. tests names the files to run,
    # relative to the package directory in the sdist; empty runs all of them.
    runner_template = Label("//tools/build/python:run_pytest.py.tmpl")
    native.genrule(
        name = name + "-main",
        srcs = [
            src,
            runner_template,
        ],
        outs = [name + "_main.py"],
        cmd = 'sed -e "s|@SDIST@|$(rootpath %s)|" -e "s|@PKG@|%s|" -e "s|@TESTS@|%s|" $(location %s) > $@' % (src, pkg, " ".join(tests), runner_template),
    )
    _native_py_test(
        name = name,
        srcs = [name + "_main.py"],
        main = name + "_main.py",
        data = [src],
        deps = deps + ["@pypi//pytest"],
        **kwargs
    )

# buildifier: disable=function-docstring
def py_ruff_test(name, srcs, config = Label("//:ruff.toml"), **kwargs):
    ruff_whl = "@pypi//ruff:whl"
    args = [
        "--ruff-whl=$(rootpath %s)" % ruff_whl,
        "--config=$(rootpath %s)" % config,
    ]
    args.extend(["$(rootpath %s)" % x for x in srcs])
    _native_py_test(
        name = name,
        srcs = [Label("//tools/build/python:run_ruff.py")],
        main = Label("//tools/build/python:run_ruff.py"),
        args = args,
        data = srcs + [ruff_whl, config],
        **kwargs
    )

# buildifier: disable=function-docstring
def py_pyright_test(name, srcs, deps = [], python_version = None, disable = [], config = Label("//:pyrightconfig.json"), **kwargs):
    args = ["--config=$(rootpath %s)" % config]
    if python_version:
        args.append("--python-version=%s" % python_version)
    args.extend(["--disable=%s" % x for x in disable])
    args.extend(["$(rootpath %s)" % x for x in srcs])
    _native_py_test(
        name = name,
        srcs = [Label("//tools/build/python:run_pyright.py")],
        main = Label("//tools/build/python:run_pyright.py"),
        args = args,
        data = srcs + [config],
        deps = deps + ["@pypi//basedpyright"],
        # Generated __init__.py files would make a directory of scripts a package
        legacy_create_init = False,
        **kwargs
    )
