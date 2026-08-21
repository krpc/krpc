" sphinx documentation tools "

load("@rules_python//python:defs.bzl", "py_test")

# buildifier: disable=function-docstring-header
def _get_src_dir(srcs, short_path = False):
    """ Given a list of input files, get the path of the src dir,
    based on the location of conf.py """
    for src in srcs:
        if src.basename == "conf.py":
            if short_path:
                return src.short_path.rpartition("/")[0]
            else:
                return src.dirname
    return None

def _build_impl(ctx):
    srcs = ctx.files.srcs

    # sphinx-build is given the source directory, never the list of .rst files. Sphinx resolves
    # symlinks in filenames passed on the command line and then skips any that do not resolve
    # inside the source dir. The sources here are Bazel symlinks pointing outside the staged
    # tree, so passing them individually would silently build an empty doctree rather than fail.
    src_dir = _get_src_dir(srcs)
    out = ctx.outputs.out
    sphinx_build = ctx.executable.sphinx_build
    builder = ctx.attr.builder
    opts = ["-D%s=%s" % x for x in ctx.attr.opts.items()]

    if builder == "html":
        # OS-independent: sphinx-build (hermetic python) into a tree artifact,
        # then archived by write_zip. No system zip, no /tmp doctree cache.
        html_dir = ctx.actions.declare_directory(out.basename + ".sphinx-html")
        build_args = ctx.actions.args()
        build_args.add_all(["-b", "html", "-E", "-d", html_dir.path + ".doctrees"])
        build_args.add_all(["-W", "-n", "-N", "-T", "-q", src_dir, html_dir.path])
        build_args.add_all(opts)
        ctx.actions.run(
            executable = sphinx_build,
            arguments = [build_args],
            inputs = srcs,
            outputs = [html_dir],
            progress_message = "Generating html documentation",
            mnemonic = "SphinxHtml",
        )

        zip_args = ctx.actions.args()
        zip_args.add("--out", out.path)
        zip_args.add("--tree", html_dir.path)
        ctx.actions.run(
            executable = ctx.executable._write_zip,
            arguments = [zip_args],
            inputs = [html_dir],
            outputs = [out],
            progress_message = "Archiving html documentation",
            mnemonic = "SphinxHtmlZip",
        )
        return

    # latex -> pdf needs `make` and a LaTeX toolchain (texlive); Linux-only, so
    # kept on the shell. The pdf target is tagged target_compatible_with linux.
    out_dir = out.path + ".sphinx-build-out"
    opts_str = " ".join(opts)
    sub_commands = [
        "%s -b %s -E -d /tmp/bazel-sphinx-build-%s -W -n -N -T -q %s %s %s" %
        (sphinx_build.path, builder, builder, src_dir, out_dir, opts_str),
        "make -e -C %s 1>/dev/null 2>/dev/null" % out_dir,
        "find %s -name *.pdf -exec cp {} %s \\;" % (out_dir, out.path),
        "rm -rf %s" % out_dir,
    ]
    ctx.actions.run_shell(
        tools = [sphinx_build],
        inputs = srcs,
        outputs = [out],
        progress_message = "Generating %s documentation" % builder,
        command = " && \\\n".join(sub_commands),
        # The default shell environment supplies PATH, needed to find make and the
        # LaTeX toolchain. LC_ALL is set on top of it so that hyphenation, sorting
        # and date formatting do not vary with whoever runs the build; unlike the
        # inherited variables it also forms part of the action key.
        use_default_shell_env = True,
        env = {"LC_ALL": "C.UTF-8"},
    )

sphinx_build = rule(
    implementation = _build_impl,
    attrs = {
        "srcs": attr.label_list(allow_files = True),
        "sphinx_build": attr.label(executable = True, mandatory = True, cfg = "exec"),
        "builder": attr.string(mandatory = True),
        "opts": attr.string_dict(),
        "out": attr.output(mandatory = True),
        "_write_zip": attr.label(
            default = Label("//tools/build:write_zip"),
            executable = True,
            cfg = "exec",
        ),
    },
)

def _test_config_impl(ctx):
    config = ctx.actions.declare_file(ctx.label.name + ".json")
    ctx.actions.write(
        output = config,
        content = json.encode({
            "sphinx_build": ctx.executable.sphinx_build.short_path,
            "builder": ctx.attr.builder,
            "src_dir": _get_src_dir(ctx.files.srcs, short_path = True),
            "opts": ctx.attr.opts,
        }),
    )
    return DefaultInfo(files = depset([config]))

# sphinx-build is a py_binary, whose label names both a launcher and the sources
# it runs, so the path of the launcher within the runfiles tree is not something
# location expansion can resolve; only a rule can ask for it. Which of the staged
# files sits beside conf.py, and so what to give sphinx as its source directory,
# is likewise settled when the graph is built rather than when the test runs.
_sphinx_test_config = rule(
    implementation = _test_config_impl,
    attrs = {
        "builder": attr.string(mandatory = True),
        "opts": attr.string_dict(),
        "sphinx_build": attr.label(executable = True, mandatory = True, cfg = "target"),
        "srcs": attr.label_list(allow_files = True),
    },
)

def _sphinx_test(name, builder, srcs, sphinx_build, opts, kwargs):
    config = name + "-config"
    _sphinx_test_config(
        name = config,
        builder = builder,
        opts = opts,
        sphinx_build = sphinx_build,
        srcs = srcs,
        testonly = True,
    )
    py_test(
        name = name,
        srcs = [Label("//tools/build:run_sphinx_test.py")],
        main = Label("//tools/build:run_sphinx_test.py"),
        args = ["$(rootpath :%s)" % config],
        data = [config, sphinx_build] + srcs,
        **kwargs
    )

# Check the prose for misspellings.
# buildifier: disable=function-docstring
def sphinx_spelling_test(name, srcs, sphinx_build, opts = {}, **kwargs):
    _sphinx_test(name, "spelling", srcs, sphinx_build, opts, kwargs)

# Check that every link the documentation makes can still be followed.
# buildifier: disable=function-docstring
def sphinx_linkcheck_test(name, srcs, sphinx_build, opts = {}, **kwargs):
    _sphinx_test(name, "linkcheck", srcs, sphinx_build, opts, kwargs)
