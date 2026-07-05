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

        ctx.actions.run_shell(
            mnemonic = "PackageFile",
            inputs = [input],
            outputs = [staging_file],
            command = 'cp "%s" "%s"' % (input.path, staging_file.path),
        )
        staging_inputs.append(staging_file)

    # Build sdist from the staging directory
    # Note: staging dir can contain symlinks, so copy dereference them first
    # by copying to a build directory
    staging_dir_path = output.path + ".py-sdist-tmp"
    build_dir_path = staging_dir_path + ".deref"
    build_log_path = build_dir_path + ".log"

    hatchling = ctx.executable._hatchling
    build_cmd = (
        "rm -rf {deref} && cp -rL {stage} {deref} && " +
        "( BASEDIR=$(pwd) && cd {deref} && $BASEDIR/{hatchling} build -t sdist ) > {log} 2>&1 || " +
        "( cat {log} >&2 2>/dev/null ; exit 1 )"
    ).format(
        deref = build_dir_path,
        stage = staging_dir_path,
        hatchling = hatchling.path,
        log = build_log_path,
    )
    sub_commands = [
        build_cmd,
        "cp %s/dist/*.tar.gz %s" % (build_dir_path, output.path),
    ]
    ctx.actions.run_shell(
        inputs = staging_inputs,
        outputs = [output],
        tools = [hatchling, ctx.attr._hatchling[DefaultInfo].files_to_run],
        progress_message = "Packaging files into %s" % output.short_path,
        command = " && ".join(sub_commands),
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
    },
)

# buildifier: disable=function-docstring
def py_test(name, src, pkg, deps = [], **kwargs):
    # The test parameters are baked into a generated test script, rather than
    # passed as arguments, as the test may be invoked via the client_test
    # harness which does not forward arguments
    runner_template = Label("//tools/build/python:run_pytest.py.tmpl")
    native.genrule(
        name = name + "-main",
        srcs = [
            src,
            runner_template,
        ],
        outs = [name + "_main.py"],
        cmd = 'sed -e "s|@SDIST@|$(rootpath %s)|" -e "s|@PKG@|%s|" $(location %s) > $@' % (src, pkg, runner_template),
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
def py_lint_test(
        name,
        pkg = None,
        pkg_name = None,
        srcs = [],
        deps = [],
        black_exclude = None,
        pylint_config = None,
        **kwargs):
    args = []
    data = []
    if black_exclude:
        args.append("'--black-exclude=%s'" % black_exclude)
    if pylint_config:
        args.append("--pylint-rcfile=$(rootpath %s)" % pylint_config)
        data.append(pylint_config)
    if pkg:
        args.extend([
            "--sdist=$(rootpath %s)" % pkg,
            "--pkg=%s" % pkg_name,
        ])
        data.append(pkg)
    else:
        args.extend(["$(rootpath %s)" % x for x in srcs])
        data.extend(srcs)

    _native_py_test(
        name = name,
        srcs = [Label("//tools/build/python:run_lint.py")],
        main = Label("//tools/build/python:run_lint.py"),
        args = args,
        data = data,
        deps = deps + [
            "@pypi//black",
            "@pypi//pylint",
        ],
        **kwargs
    )
