def _check_documented_impl(ctx):

    expected = ctx.label.name + '.check-documented.expected'
    actual_tmp = ctx.label.name + '.check-documented.actual.tmp'
    actual = ctx.label.name + '.check-documented.actual'

    sub_commands = ['rm -rf %s' % actual]
    for src in ctx.files.srcs:
        if src.short_path.endswith('.documented.txt'): #TODO: use a provider instead
            sub_commands.append('cat %s >> %s' % (src.short_path, actual_tmp))
    sub_commands.extend([
        'echo "KRPC.AddStream\nKRPC.GetServices\nKRPC.GetStatus\nKRPC.RemoveStream\n" >> %s' % actual_tmp,
        'sed -i "/^$/d" %s' % actual_tmp,
        'sort %s | uniq > %s' % (actual_tmp, actual),
        'sort %s | uniq > %s' % (ctx.file.members.short_path, expected),
        'diff %s %s' % (expected, actual)
    ])

    ctx.file_action(
        output = ctx.outputs.executable,
        content = '&& \\\n'.join(sub_commands)+'\n',
        executable = True
    )

    return struct(
        name = ctx.label.name,
        out = ctx.outputs.executable,
        runfiles = ctx.runfiles(files = [ctx.file.members] + ctx.files.srcs)
    )

check_documented_test = rule(
    implementation = _check_documented_impl,
    attrs = {
        'members': attr.label(allow_files=True, single_file=True),
        'srcs': attr.label_list(allow_files=True)
    },
    test = True
)
