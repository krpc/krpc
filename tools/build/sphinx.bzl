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

def _add_runfile(sub_commands, path, runfile_path):
    sub_commands.extend([
        "mkdir -p `dirname %s`" % runfile_path,
        'ln -f -s "`pwd`/%s" "`pwd`/%s"' % (path, runfile_path),
    ])

def _build_impl(ctx):
    srcs = ctx.files.srcs
    src_dir = _get_src_dir(srcs)
    out = ctx.outputs.out
    out_dir = out.path + ".sphinx-build-out"
    sphinx_build = ctx.executable.sphinx_build
    builder = ctx.attr.builder
    opts = " ".join(["-D%s=%s" % x for x in ctx.attr.opts.items()])
    exec_reqs = {}
    sub_commands = []

    srcs_paths = [f.path for f in srcs]
    srcs_paths = [x for x in srcs_paths if x.endswith(".rst")]

    sub_commands.append(
        "%s -b %s -E -d /tmp/bazel-sphinx-build-%s -W -n -N -T -q %s %s %s %s" %
        (
            sphinx_build.path,
            builder,
            builder,
            src_dir,
            out_dir,
            " ".join(srcs_paths),
            opts,
        ),
    )

    if builder == "html":
        sub_commands.append("(CWD=`pwd` && cd %s && zip --quiet -r $CWD/%s ./)" % (out_dir, out.path))

    elif builder == "latex":
        sub_commands.extend([
            "make -e -C %s 1>/dev/null 2>/dev/null" % out_dir,
            "find %s -name *.pdf -exec cp {} %s \\;" % (out_dir, out.path),
            "rm -rf %s" % out_dir,
        ])

    ctx.actions.run_shell(
        tools = [sphinx_build],
        inputs = srcs,
        outputs = [out],
        progress_message = "Generating %s documentation" % builder,
        command = " && \\\n".join(sub_commands),
        use_default_shell_env = True,
        execution_requirements = exec_reqs,
    )

sphinx_build = rule(
    implementation = _build_impl,
    attrs = {
        "srcs": attr.label_list(allow_files = True),
        "sphinx_build": attr.label(executable = True, mandatory = True, cfg = "host"),
        "builder": attr.string(mandatory = True),
        "opts": attr.string_dict(),
        "out": attr.output(mandatory = True),
    },
)

def _spelling_impl(ctx):
    srcs = ctx.files.srcs
    src_dir = _get_src_dir(srcs, short_path = True)
    out = ctx.outputs.executable
    sphinx_build = ctx.executable.sphinx_build
    sphinx_build_runfiles = ctx.attr.sphinx_build.default_runfiles.files.to_list()
    opts = " ".join(["-D%s=%s" % x for x in ctx.attr.opts.items()])
    sub_commands = []

    for f in sphinx_build_runfiles:
        _add_runfile(
            sub_commands,
            f.short_path,
            sphinx_build.short_path + ".runfiles/krpc/" + f.short_path,
        )

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

    return struct(
        name = ctx.label.name,
        out = out,
        tools = [sphinx_build],
        runfiles = ctx.runfiles(files = [sphinx_build] + sphinx_build_runfiles + srcs),
    )

sphinx_spelling_test = rule(
    implementation = _spelling_impl,
    attrs = {
        "srcs": attr.label_list(allow_files = True),
        "sphinx_build": attr.label(
            executable = True,
            allow_single_file = True,
            mandatory = True,
            cfg = "host",
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
    sphinx_build_runfiles = ctx.attr.sphinx_build.default_runfiles.files.to_list()
    opts = " ".join(["-D%s=%s" % x for x in ctx.attr.opts.items()])
    sub_commands = []

    for f in sphinx_build_runfiles:
        _add_runfile(
            sub_commands,
            f.short_path,
            sphinx_build.short_path + ".runfiles/krpc/" + f.short_path,
        )

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

    return struct(
        name = ctx.label.name,
        out = out,
        tools = [sphinx_build],
        runfiles = ctx.runfiles(files = [sphinx_build] + sphinx_build_runfiles + srcs),
    )

sphinx_linkcheck_test = rule(
    implementation = _linkcheck_impl,
    attrs = {
        "srcs": attr.label_list(allow_files = True),
        "sphinx_build": attr.label(
            executable = True,
            allow_single_file = True,
            mandatory = True,
            cfg = "host",
        ),
        "opts": attr.string_dict(),
    },
    test = True,
)
