" sphinx documentation tools "

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
        use_default_shell_env = True,
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

def _spelling_impl(ctx):
    srcs = ctx.files.srcs
    src_dir = _get_src_dir(srcs, short_path = True)
    out = ctx.outputs.executable
    sphinx_build = ctx.executable.sphinx_build
    opts = " ".join(["-D%s=%s" % x for x in ctx.attr.opts.items()])
    sub_commands = []

    sphinx_commands = [
        # FIXME: following copy is a hack to fix sphinx not being able to read the
        #        dictionary from a symlink and requiring the file to be writable
        'cp "`pwd`/doc/srcs/dictionary.txt" "`pwd`/doc/srcs/dictionary.txt.tmp"',
        'rm "`pwd`/doc/srcs/dictionary.txt"',
        'mv "`pwd`/doc/srcs/dictionary.txt.tmp" "`pwd`/doc/srcs/dictionary.txt"',
        # end of hack
        "chmod 644 `pwd`/doc/srcs/dictionary.txt",
        # FIXME: re-add -W flag. Fails currently as it gets a warning looking for contributors
        "%s -b spelling -E -N -T %s ./out %s 2>&1 | tee stdout" % (sphinx_build.short_path, src_dir, opts),
        'grep "misspelled words" stdout',
        "ret=$?",
        'echo "ret=$ret"',
        "if [ $ret -eq 0 ]; then exit 1; fi",
    ]
    sub_commands.append("(" + "; ".join(sphinx_commands) + ")")

    ctx.actions.write(
        output = out,
        content = " &&\n".join(sub_commands) + "\n",
        is_executable = True,
    )

    return DefaultInfo(
        executable = out,
        runfiles = ctx.runfiles(files = [sphinx_build] + srcs).merge(ctx.attr.sphinx_build[DefaultInfo].default_runfiles),
    )

sphinx_spelling_test = rule(
    implementation = _spelling_impl,
    attrs = {
        "srcs": attr.label_list(allow_files = True),
        "sphinx_build": attr.label(
            executable = True,
            mandatory = True,
            cfg = "exec",
        ),
        "opts": attr.string_dict(),
    },
    test = True,
)

def _linkcheck_impl(ctx):
    srcs = ctx.files.srcs
    src_dir = _get_src_dir(srcs, short_path = True)
    out = ctx.outputs.executable
    sphinx_build = ctx.executable.sphinx_build
    opts = " ".join(["-D%s=%s" % x for x in ctx.attr.opts.items()])
    sub_commands = []

    sphinx_commands = [
        "%s -b linkcheck -E -N -T %s ./out %s" % (sphinx_build.short_path, src_dir, opts),
        "ret=$?",
        "lines=`cat ./out/output.txt | wc -l`",
        'echo "Link checker messages ($lines lines):"',
        "cat ./out/output.txt",
        "if [ $ret -ne 0 ]; then exit 1; fi",
    ]
    sub_commands.append("(" + "; ".join(sphinx_commands) + ")")

    ctx.actions.write(
        output = out,
        content = " &&\n".join(sub_commands) + "\n",
        is_executable = True,
    )

    return DefaultInfo(
        executable = out,
        runfiles = ctx.runfiles(files = [sphinx_build] + srcs).merge(ctx.attr.sphinx_build[DefaultInfo].default_runfiles),
    )

sphinx_linkcheck_test = rule(
    implementation = _linkcheck_impl,
    attrs = {
        "srcs": attr.label_list(allow_files = True),
        "sphinx_build": attr.label(
            executable = True,
            mandatory = True,
            cfg = "exec",
        ),
        "opts": attr.string_dict(),
    },
    test = True,
)
