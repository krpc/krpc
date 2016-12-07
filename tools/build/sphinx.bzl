def _get_src_dir(srcs, short_path=False):
    """ Given a list of input files, get the path of the src dir,
    based on the location of conf.py """
    for src in srcs:
        if src.basename == 'conf.py':
            if short_path:
                return src.short_path.rpartition('/')[0]
            else:
                return src.dirname
    return None

def _add_runfile(sub_commands, path, runfile_path):
    sub_commands.extend([
        'mkdir -p `dirname %s`' % runfile_path,
        'ln -f -s "`pwd`/%s" "`pwd`/%s"' % (path, runfile_path)
    ])

def _build_impl(ctx):
    srcs = ctx.files.srcs
    src_dir = _get_src_dir(srcs)
    out = ctx.outputs.out
    out_dir = out.path+'.sphinx-build-out'
    sphinx_build = ctx.executable.sphinx_build
    sphinx_build_runfiles = list(ctx.attr.sphinx_build.default_runfiles.files)
    builder = ctx.attr.builder
    opts = ' '.join(['-D%s=%s' % x for x in ctx.attr.opts.items()])
    exec_reqs = {}
    sub_commands = []

    runfiles_dir = out.path + '.runfiles/krpc'
    sub_commands.append('rm -rf %s' % runfiles_dir)
    _add_runfile(sub_commands, sphinx_build.path, runfiles_dir + '/' + sphinx_build.basename)
    for f in sphinx_build_runfiles:
        _add_runfile(sub_commands, f.path, runfiles_dir+ '/' + sphinx_build.basename + '.runfiles/krpc/' + f.short_path)

    sub_commands.append('%s/%s -b %s -E -d /tmp/bazel-sphinx-build-%s -W -n -N -T -q %s %s %s' % (runfiles_dir, sphinx_build.basename, builder, builder, src_dir, out_dir, opts))

    if builder == 'html':
        sub_commands.append('(CWD=`pwd` && cd %s && zip --quiet -r $CWD/%s ./)' % (out_dir, out.path))

    elif builder == 'latex':
        exec_reqs['local'] = '' # FIXME: pdflatex fails to run from the sandbox
        sub_commands.extend([
            # FIXME: Use full path to pdflatex binary to workaround Bazel issue
            'PDFLATEX=/usr/bin/pdflatex make -e -C %s 1>/dev/null' % out_dir,
            'find %s -name *.pdf -exec cp {} %s \;' % (out_dir, out.path),
            'rm -rf %s' % out_dir
        ])

    ctx.action(
        inputs = [sphinx_build] + sphinx_build_runfiles + srcs,
        outputs = [out],
        progress_message = 'Generating %s documentation' % builder,
        command = ' && \\\n'.join(sub_commands),
        use_default_shell_env = True,
        execution_requirements = exec_reqs
    )

sphinx_build = rule(
    implementation = _build_impl,
    attrs = {
        'srcs': attr.label_list(allow_files=True),
        'sphinx_build': attr.label(executable=True, mandatory=True, cfg='host'),
        'builder': attr.string(mandatory=True),
        'opts': attr.string_dict(),
        'out': attr.output(mandatory=True)
    }
)

def _spelling_impl(ctx):
    srcs = ctx.files.srcs
    src_dir = _get_src_dir(srcs, short_path=True)
    out = ctx.outputs.executable
    sphinx_build = ctx.executable.sphinx_build
    sphinx_build_runfiles = list(ctx.attr.sphinx_build.default_runfiles.files)
    opts = ' '.join(['-D%s=%s' % x for x in ctx.attr.opts.items()])
    sub_commands = []

    _add_runfile(sub_commands, sphinx_build.short_path, sphinx_build.basename + '.runfiles/krpc/' + sphinx_build.short_path)
    for f in sphinx_build_runfiles:
        _add_runfile(sub_commands, f.short_path, sphinx_build.basename + '.runfiles/krpc/' + sphinx_build.short_path + '.runfiles/krpc/' + f.short_path)

    sphinx_commands = [
        #FIXME: following copy is a hack to fix sphinx not being able to read the dictionary from a symlink and requiring the file to be writable
        'cp "`pwd`/doc/srcs/dictionary.txt" "`pwd`/doc/srcs/dictionary.txt.tmp"',
        'rm "`pwd`/doc/srcs/dictionary.txt"',
        'mv "`pwd`/doc/srcs/dictionary.txt.tmp" "`pwd`/doc/srcs/dictionary.txt"',
        'chmod 644 `pwd`/doc/srcs/dictionary.txt',
        '(cd %s.runfiles/krpc; %s -b spelling -E -W -N -T ../../%s ./out %s)' % (sphinx_build.basename, sphinx_build.short_path, src_dir, opts),
        'ret=$?',
        'lines=`cat %s.runfiles/krpc/out/output.txt | wc -l`' % sphinx_build.basename,
        'echo "Spelling checker messages ($lines lines):"',
        'cat %s.runfiles/krpc/out/output.txt' % sphinx_build.basename,
        'if [ $ret -ne 0 ]; then exit 1; fi'
    ]
    sub_commands.append('('+'; '.join(sphinx_commands)+')')

    ctx.file_action(
        output = out,
        content = ' &&\n'.join(sub_commands)+'\n',
        executable = True
    )

    return struct(
        name = ctx.label.name,
        out = out,
        runfiles = ctx.runfiles(files = [sphinx_build] + sphinx_build_runfiles + srcs)
    )

sphinx_spelling_test = rule(
    implementation = _spelling_impl,
    attrs = {
        'srcs': attr.label_list(allow_files=True),
        'sphinx_build': attr.label(executable=True, single_file=True,
                                   allow_files=True, mandatory=True, cfg='host'),
        'opts': attr.string_dict()
    },
    test = True
)

def _linkcheck_impl(ctx):
    srcs = ctx.files.srcs
    src_dir = _get_src_dir(srcs, short_path=True)
    out = ctx.outputs.executable
    sphinx_build = ctx.executable.sphinx_build
    sphinx_build_runfiles = list(ctx.attr.sphinx_build.default_runfiles.files)
    opts = ' '.join(['-D%s=%s' % x for x in ctx.attr.opts.items()])
    sub_commands = []

    _add_runfile(sub_commands, sphinx_build.short_path, sphinx_build.basename + '.runfiles/krpc/' + sphinx_build.short_path)
    for f in sphinx_build_runfiles:
        _add_runfile(sub_commands, f.short_path, sphinx_build.basename + '.runfiles/krpc/' + sphinx_build.short_path + '.runfiles/krpc/' + f.short_path)

    sphinx_commands = [
        '(cd %s.runfiles/krpc; %s -b linkcheck -E -N -T ../../%s ./out %s)' % (sphinx_build.basename, sphinx_build.short_path, src_dir, opts),
        'ret=$?',
        'lines=`cat %s.runfiles/krpc/out/output.txt | wc -l`' % sphinx_build.basename,
        'echo "Link checker messages ($lines lines):"',
        'cat %s.runfiles/krpc/out/output.txt' % sphinx_build.basename,
        'if [ $ret -ne 0 ]; then exit 1; fi'
    ]
    sub_commands.append('('+'; '.join(sphinx_commands)+')')

    ctx.file_action(
        output = out,
        content = ' &&\n'.join(sub_commands)+'\n',
        executable = True
    )

    return struct(
        name = ctx.label.name,
        out = out,
        runfiles = ctx.runfiles(files = [sphinx_build] + sphinx_build_runfiles + srcs)
    )

sphinx_linkcheck_test = rule(
    implementation = _linkcheck_impl,
    attrs = {
        'srcs': attr.label_list(allow_files=True),
        'sphinx_build': attr.label(executable=True, single_file=True,
                                   allow_files=True, mandatory=True, cfg='host'),
        'opts': attr.string_dict()
    },
    test = True
)
