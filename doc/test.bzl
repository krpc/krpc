def _check_documented_impl(ctx):

    expected = ctx.file.members.short_path
    actual = ctx.label.name + '.documented.actual.txt'

    sub_commands = ['rm -rf %s' % actual]
    for src in ctx.files.srcs:
        if src.short_path.endswith('.documented.txt'): #TODO: use a provider instead
            sub_commands.append('cat %s >> %s' % (src.short_path, actual))
    sub_commands.extend([
        'doc/test.py %s %s' % (expected, actual)
    ])

    ctx.file_action(
        output = ctx.outputs.executable,
        content = '&& \\\n'.join(sub_commands)+'\n',
        executable = True
    )

    return struct(
        name = ctx.label.name,
        out = ctx.outputs.executable,
        runfiles = ctx.runfiles(files = [ctx.file._tool] + [ctx.file.members] + ctx.files.srcs)
    )

check_documented_test = rule(
    implementation = _check_documented_impl,
    attrs = {
        'members': attr.label(allow_files=True, single_file=True),
        'srcs': attr.label_list(allow_files=True),
        '_tool': attr.label(default=Label('//doc:test.py'), allow_files=True,
                            single_file=True, executable=True, cfg='host'),
    },
    test = True
)
