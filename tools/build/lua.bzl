def _test_impl(ctx):
    sub_commands = []
    for dep in ctx.files.deps:
        sub_commands.append('luarocks --tree=env install %s;' % dep.path)
    sub_commands.extend([
        'echo "install %s";' % ctx.file.src.path,
        'luarocks --tree=env install %s ;' % ctx.file.src.path,
        'ls -R env'
    ])
    ctx.file_action(
        output = ctx.outputs.executable,
        content = '\n'.join(sub_commands)+'\n',
        executable = True
    )

    runfiles = ctx.runfiles(files = [ctx.file.src] + ctx.files.deps)

    return struct(
        name = ctx.label.name,
        out = ctx.outputs.executable,
        runfiles = runfiles
    )

lua_test = rule(
    implementation = _test_impl,
    attrs = {
        'src': attr.label(allow_files=True, single_file=True),
        'deps': attr.label_list(allow_files=True)
    },
    test = True
)
