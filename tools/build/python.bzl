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

def _create_py_env(out, install):
    tmp = out+'.tmp-create-py-env'
    cmds = [
        'rm -rf %s' % tmp,
        'virtualenv %s --quiet --no-site-packages' % tmp
    ]
    for lib in install:
        cmds.append('%s/bin/python %s/bin/pip install --quiet --no-deps %s' % (tmp, tmp, lib.path))
    cmds.extend([
        '(CWD=`pwd`; cd %s; tar -c -f $CWD/%s *)' % (tmp, out)
    ])
    return cmds

def _extract_py_env(env, path):
    return [
        'rm -rf %s' % path,
        'mkdir -p %s' % path,
        '(CWD=`pwd`; cd %s; tar -xf $CWD/%s)' % (path, env)
    ]

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
            command = 'ln -f -s "`pwd`/%s" "`pwd`/%s"' % (input.path, staging_file.path)
        )
        staging_inputs.append(staging_file)

    # Run setup.py sdist from the staging directory
    sub_commands = [
        '(cd %s ; BAZEL_BUILD=1 python setup.py --quiet sdist --formats=zip)' % staging_dir_path,
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

def _script_impl(ctx):
    script_setup = ctx.new_file(ctx.configuration.genfiles_dir, 'py_script-setup-%s' % ctx.attr.script)
    script_env = ctx.new_file(ctx.configuration.genfiles_dir, 'py_script-env-%s' % ctx.attr.script)
    script_run = ctx.outputs.executable

    ctx.file_action(
        output = script_setup,
        content = '&& \\\n'.join(_create_py_env(script_env.path, install = ctx.files.deps + [ctx.file.pkg]))+'\n',
        executable = True
    )

    ctx.action(
        inputs = [ctx.file.pkg] + ctx.files.deps,
        outputs = [script_env],
        progress_message = 'Setting up python script %s' % ctx.attr.script,
        executable = script_setup,
        use_default_shell_env = True
    )

    env = ctx.attr.script+'.py_script-env'
    sub_commands = _extract_py_env('$0.runfiles/krpc/%s' % script_env.short_path, env)
    sub_commands.append('%s/bin/python %s/bin/%s "$@"' % (env, env, ctx.attr.script))
    ctx.file_action(
        output = script_run,
        content = ' && \\\n'.join(sub_commands)+'\n',
        executable = True
    )

    return struct(
        name = ctx.label.name,
        out = script_run,
        runfiles = ctx.runfiles(files = [script_env])
    )

py_script = rule(
    implementation = _script_impl,
    attrs = {
        'script': attr.string(mandatory=True),
        'pkg': attr.label(allow_files=True, single_file=True),
        'deps': attr.label_list(allow_files=True)
    },
    executable = True
)

def _test_impl(ctx, pyexe='python2'):
    sub_commands = ['virtualenv env --quiet --no-site-packages --python=%s' % pyexe]
    for dep in ctx.files.deps:
        sub_commands.append('env/bin/python env/bin/pip install --quiet --no-deps %s' % dep.short_path)
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

def _lint_impl(ctx):
    files = []
    deps = ctx.files._pylint + ctx.files.deps
    if ctx.attr.src:
        # Run pylint on a python package
        args = [ctx.attr.pkg]
        deps.append(ctx.file.src)
    else:
        # Run pylint on a list of file paths
        args = []
        for x in ctx.files.srcs:
            args.append(x.short_path)
        files.extend(ctx.files.srcs)

    sub_commands = ['virtualenv env --quiet --no-site-packages']
    for dep in deps:
        sub_commands.append('env/bin/python env/bin/pip install --quiet --no-deps %s' % dep.short_path)
    args = ' '.join(args)
    sub_commands.append('env/bin/python env/bin/pylint --rcfile=%s %s' % (ctx.file.rcfile.short_path, args))

    ctx.file_action(
        output = ctx.outputs.executable,
        content = '&& \\\n'.join(sub_commands)+'\n',
        executable = True
    )

    runfiles = ctx.runfiles(files = files + deps + [ctx.file.rcfile])

    return struct(
        name = ctx.label.name,
        out = ctx.outputs.executable,
        runfiles = runfiles
    )

py_lint_test = rule(
    implementation = _lint_impl,
    attrs = {
        'src': attr.label(allow_files=True, single_file=True),
        'pkg': attr.string(),
        'srcs': attr.label_list(allow_files=True),
        'deps': attr.label_list(allow_files=True),
        'rcfile': attr.label(allow_files=True, single_file=True),
        '_pylint': attr.label_list(default=[
            Label('@python_six//file'),
            Label('@python_pylint//file'),
            Label('@python_astroid//file'),
            Label('@python_wrapt//file'),
            Label('@python_lazy_object_proxy//file')
        ], allow_files=True)
    },
    test = True
)
