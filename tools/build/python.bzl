def _apply_path_map(path_map, path):
    """ Apply the path mappings to a path.
        Replaces the longest prefix match from the mapping. """
    matchlen = 0
    match = path
    for x,y in path_map.items():
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
    staging_dir = output.basename + '.py-sdist-tmp'
    staging_dir_path = output.path.replace(
        ctx.configuration.bin_dir.path, ctx.configuration.genfiles_dir.path) + '.py-sdist-tmp'
    staging_inputs = []
    for input in inputs:
        staging_path = staging_dir + '/' + _apply_path_map(path_map, input.short_path)
        staging_file = ctx.new_file(ctx.configuration.genfiles_dir, staging_path)

        ctx.action(
            mnemonic = 'PackageFile',
            inputs = [input],
            outputs = [staging_file],
            command = 'ln -f -r -s %s %s' % (input.path, staging_file.path)
        )
        staging_inputs.append(staging_file)

    # Run setup.py sdist from the staging directory
    sub_commands = [
        '(cd %s ; python setup.py --quiet sdist --formats=zip)' % staging_dir_path,
        'cp %s/dist/*.zip %s' % (staging_dir_path, output.path)
    ]
    ctx.action(
        inputs = staging_inputs,
        outputs = [output],
        progress_message = 'Packaging files into %s' % output.short_path,
        command = ' && '.join(sub_commands)
    )

py_sdist = rule(
    implementation = _sdist_impl,
    attrs = {
        'files': attr.label_list(allow_files=True, mandatory=True, non_empty=True),
        'path_map': attr.string_dict(),
        'out': attr.output(mandatory=True)
    }
)

def _test_impl(ctx, pyexe='python2'):
    sub_commands = [
        'virtualenv env --system-site-packages --python=%s' % pyexe,
        'sed -i "1s/.*/#!env\\/bin\\/python/" env/bin/pip'
    ]
    for dep in ctx.files.deps:
        sub_commands.append('env/bin/pip install --no-deps %s' % dep.path)
    sub_commands.extend([
        'unzip -o %s' % (ctx.file.src.short_path), #TODO: install the package then run the tests??
        '(cd %s ; ../env/bin/python setup.py test)' % ctx.attr.pkg
    ])
    ctx.file_action(
        output = ctx.outputs.executable,
        content = '&& \\\n'.join(sub_commands)+'\n',
        executable = True
    )

    runfiles = ctx.runfiles(files = [ctx.file.src] + ctx.files.deps)

    return struct(
        name = ctx.label.name,
        out = ctx.outputs.executable,
        runfiles = runfiles
    )

py_test = rule(
    implementation = _test_impl,
    attrs = {
        'src': attr.label(allow_files=True, single_file=True),
        'pkg': attr.string(mandatory=True),
        'deps': attr.label_list(allow_files=True)
    },
    test = True
)

def _test3_impl(ctx):
    return _test_impl(ctx, pyexe='python3')

py3_test = rule(
    implementation = _test3_impl,
    attrs = {
        'src': attr.label(allow_files=True, single_file=True),
        'pkg': attr.string(mandatory=True),
        'deps': attr.label_list(allow_files=True)
    },
    test = True
)
