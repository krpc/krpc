def _add_runfile(sub_commands, path, runfile_path):
    sub_commands.extend([
        'mkdir -p `dirname %s`' % runfile_path,
        'ln -f -s "`pwd`/%s" "`pwd`/%s"' % (path, runfile_path)
    ])

def _check_impl(ctx):
    srcs = ctx.files.srcs
    hdrs = ctx.files.hdrs
    includes = ctx.files.includes
    runfiles = srcs + hdrs

    args = ['--enable=all', '--suppress=missingIncludeSystem', '--inline-suppr', '--error-exitcode=1', '--check-config']
    args.extend(['-I%s' % x.short_path for x in includes])
    args.extend([x.short_path for x in srcs])

    ctx.file_action(
        ctx.outputs.executable,
        '/usr/bin/cppcheck %s\n' % ' '.join(args)
    )

    return struct(
        name = ctx.label.name,
        runfiles = ctx.runfiles(files = runfiles)
    )

cpp_check_test = rule(
    implementation = _check_impl,
    attrs = {
        'srcs': attr.label_list(allow_files=True),
        'hdrs': attr.label_list(allow_files=True),
        'includes': attr.label_list(allow_files=True)
    },
    test = True
)

def _lint_impl(ctx):
    out = ctx.outputs.executable
    srcs = ctx.files.srcs
    hdrs = ctx.files.hdrs
    extra_files = ctx.files.extra_files
    filters = ctx.attr.filters
    cpplint = ctx.executable.cpplint
    cpplint_runfiles = list(ctx.attr.cpplint.default_runfiles.files)
    runfiles = [cpplint] + cpplint_runfiles + srcs + hdrs + extra_files
    sub_commands = []

    runfiles_dir = out.path + '.runfiles/krpc'
    sub_commands.append('rm -rf %s' % runfiles_dir)
    _add_runfile(sub_commands, cpplint.short_path, runfiles_dir + '/' + cpplint.basename)
    for f in cpplint_runfiles:
        _add_runfile(sub_commands, f.short_path, runfiles_dir+ '/' + cpplint.basename + '.runfiles/krpc/' + f.short_path)

    args = ['--linelength=100']
    if filters != None:
        args.append('--filter=%s' % ','.join(filters))
    args.extend([x.short_path for x in srcs + hdrs])
    sub_commands.append('%s/%s %s' % (runfiles_dir, cpplint.basename, ' '.join(args)))

    ctx.file_action(
        ctx.outputs.executable,
        content = ' &&\n'.join(sub_commands)+'\n',
        executable = True
    )

    return struct(
        name = ctx.label.name,
        runfiles = ctx.runfiles(files = runfiles)
    )

cpp_lint_test = rule(
    implementation = _lint_impl,
    attrs = {
        'srcs': attr.label_list(allow_files=True),
        'hdrs': attr.label_list(allow_files=True),
        'includes': attr.label_list(allow_files=True),
        'extra_files': attr.label_list(allow_files=True),
        'filters': attr.string_list(default=['+build/include_alpha']),
        'cpplint': attr.label(default=Label('//tools/build/cpplint'), executable=True, cfg='host')
    },
    test = True
)
