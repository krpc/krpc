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
    tmp = out+'.tmp-create-py-env.$$'
    cmds = [
        'PWD=`pwd`',
        'rm -rf %s' % tmp,
        'virtualenv %s --python python3 --quiet --never-download ' % tmp
    ]
    for lib in install:
        cmds.append(
            'CFLAGS="-O0" %s/bin/python %s/bin/pip install --quiet --no-deps --no-cache-dir file:$PWD/%s'
            % (tmp, tmp, lib.path))
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

def _add_runfile(sub_commands, path, runfile_path):
    sub_commands.extend([
        'mkdir -p `dirname %s`' % runfile_path,
        'cp "%s" "%s"' % (path, runfile_path)
    ])

def _get_parent_dirname(target):
    """
        'client/krpc/client.py' returns 'krpc'
        'client/krpc/' returns 'client'
    """
    first_slash = -1
    second_slash = -1
    for i in range(len(target) - 2, 0 - 1, -1):
        if target[i] == "/":
            if first_slash == -1:
                first_slash = i
            else:
                second_slash = i
                break
    return target[second_slash+1 : first_slash]

def _sdist_impl(ctx):
    output = ctx.outputs.out
    inputs = ctx.files.files
    path_map = ctx.attr.path_map

    # Symlink all the files to a staging directory
    # to get the required directory structure in the archive
    staging_dir = output.basename + '.py-sdist-tmp'
    staging_inputs = []
    for input in inputs:
        staging_path = staging_dir + '/' + _apply_path_map(path_map, input.short_path)
        staging_file = ctx.actions.declare_file(staging_path)

        if "setup.py" in input.path:
            typed_sub = ''
            if len(ctx.files.stub_files) > 0:
                package = _get_parent_dirname(ctx.files.stub_files[0].path)
                typed_sub = 'package_data = {"%s": ["py.typed",%s]},' % (package, ','.join([ '"%s"' % stub_file.basename for stub_file in ctx.files.stub_files]))

            # Uses setup.py as a template and replaces "{typed_files}" with '' or package_data when there are stubs
            ctx.actions.expand_template(
                template = input,
                output = staging_file,
                substitutions = {
                    "{typed_files}": typed_sub,
                },
            )
        else:
            ctx.actions.run_shell(
                mnemonic = 'PackageFile',
                inputs = [input],
                outputs = [staging_file],
                command = 'cp "%s" "%s"' % (input.path, staging_file.path)
            )
        staging_inputs.append(staging_file)

    for stub_file in ctx.files.stub_files:
        staging_path = staging_dir + '/' + _apply_path_map(path_map, stub_file.short_path)
        staging_file = ctx.actions.declare_file(staging_path)

        ctx.actions.run_shell(
            mnemonic = 'PackageFile',
            inputs = [stub_file],
            outputs = [staging_file],
            command = 'cp "%s" "%s"' % (stub_file.path, staging_file.path)
        )

        staging_inputs.append(staging_file)

    # Run setup.py sdist from the staging directory
    staging_dir_path = output.path.replace(
        ctx.configuration.bin_dir.path, ctx.configuration.genfiles_dir.path) + '.py-sdist-tmp'
    sub_commands = [
        '(cd %s ; BAZEL_BUILD=1 python setup.py --quiet sdist --formats=zip)' % staging_dir_path,
        'cp %s/dist/*.zip %s' % (staging_dir_path, output.path)
    ]
    ctx.actions.run_shell(
        inputs = staging_inputs,
        outputs = [output],
        progress_message = 'Packaging files into %s' % output.short_path,
        command = ' && '.join(sub_commands)
    )

py_sdist = rule(
    implementation = _sdist_impl,
    attrs = {
        'files': attr.label_list(allow_files=True, mandatory=True, allow_empty=True),
        'path_map': attr.string_dict(),
        'out': attr.output(mandatory=True),
        'stub_files': attr.label_list(allow_files=True, allow_empty=True),
    }
)

def _script_impl(ctx):
    script_setup = ctx.actions.declare_file('py_script-setup-%s' % ctx.attr.script)
    script_env = ctx.actions.declare_file('py_script-env-%s' % ctx.attr.script)
    script_run = ctx.outputs.executable

    ctx.actions.write(
        output = script_setup,
        content = ' && \\\n'.join(_create_py_env(script_env.path, install = ctx.files.deps + [ctx.file.pkg]))+'\n',
        is_executable = True
    )

    ctx.actions.run(
        inputs = [ctx.file.pkg] + ctx.files.deps,
        outputs = [script_env],
        progress_message = 'Setting up python script %s' % ctx.attr.script,
        executable = script_setup,
        use_default_shell_env = True
    )

    env = ctx.attr.script+'.py_script-env-$$'
    sub_commands = _extract_py_env('$0.runfiles/krpc/%s' % script_env.short_path, env)
    sub_commands.append('%s/bin/python %s/bin/%s "$@"' % (env, env, ctx.attr.script))
    sub_commands.append('rm -rf %s' % env)
    ctx.actions.write(
        output = script_run,
        content = ' && \\\n'.join(sub_commands)+'\n',
        is_executable = True
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
        'pkg': attr.label(allow_single_file=True),
        'deps': attr.label_list(allow_files=True)
    },
    executable = True
)

def _test_impl(ctx, pyexe='python3'):
    sub_commands = ['virtualenv env --python %s --quiet --never-download ' % pyexe]
    for dep in ctx.files.deps:
        if pyexe == 'python3' and dep.path == 'external/python_enum34/file/downloaded':
            # enum34 not required with Python 3
            continue
        sub_commands.append(
            'env/bin/python env/bin/pip install --quiet --no-deps --no-cache-dir file:`pwd`/%s'
            % dep.short_path)
    sub_commands.extend([
        'unzip -o %s' % (ctx.file.src.short_path), #TODO: install the package then run the tests??
        '(cd %s ; ../env/bin/python setup.py test)' % ctx.attr.pkg
    ])
    ctx.actions.write(
        output = ctx.outputs.executable,
        content = ' && \\\n'.join(sub_commands)+'\n',
        is_executable = True
    )

    runfiles = ctx.runfiles(files = [ctx.file.src] + ctx.files.deps)

    return struct(
        name = ctx.label.name,
        out = ctx.outputs.executable,
        runfiles = runfiles
    )

def _test3_impl(ctx):
    return _test_impl(ctx, pyexe='python3')

py3_test = rule(
    implementation = _test3_impl,
    attrs = {
        'src': attr.label(allow_single_file=True),
        'pkg': attr.string(mandatory=True),
        'deps': attr.label_list(allow_files=True)
    },
    test = True
)

py_test = py3_test

def _lint_impl(ctx):
    out = ctx.outputs.executable
    files = []
    deps = list(ctx.files.deps)
    pycodestyle_args = []
    pylint_args = []
    if ctx.attr.pycodestyle_config:
        pycodestyle_args.append('--config=%s' % ctx.file.pycodestyle_config.short_path)
    if ctx.attr.pylint_config:
        pylint_args.append('--rcfile=%s' % ctx.file.pylint_config.short_path)
    if ctx.attr.pkg:
        # Run on a python package
        pycodestyle_args.append('env/lib/python*/site-packages/%s' % ctx.attr.pkg_name)
        pylint_args.append(ctx.attr.pkg_name)
        deps.append(ctx.file.pkg)
    else:
        # Run on a list of file paths
        for x in ctx.files.srcs:
            pycodestyle_args.append(x.short_path)
            pylint_args.append(x.short_path)
        files.extend(ctx.files.srcs)

    pycodestyle = ctx.executable.pycodestyle
    pylint = ctx.executable.pylint
    pycodestyle_runfiles = ctx.attr.pycodestyle.default_runfiles.files.to_list()
    pylint_runfiles = ctx.attr.pylint.default_runfiles.files.to_list()

    runfiles = [pycodestyle, pylint] + pycodestyle_runfiles + pylint_runfiles + files + deps
    if ctx.attr.pycodestyle_config:
        runfiles.append(ctx.file.pycodestyle_config)
    if ctx.attr.pylint_config:
        runfiles.append(ctx.file.pylint_config)

    sub_commands = []

    # Install dependences in a new virtual env
    sub_commands = ['virtualenv env --python python3 --quiet --never-download ']
    for dep in deps:
        sub_commands.append(
            'env/bin/python env/bin/pip install --quiet --no-deps --no-cache-dir file:`pwd`/%s'
            % dep.short_path)

    # Run pycodestyle
    runfiles_dir = out.path + '.runfiles/krpc'
    sub_commands.append('rm -rf %s' % runfiles_dir)
    _add_runfile(sub_commands, pycodestyle.short_path, runfiles_dir + '/' + pycodestyle.basename)
    for f in pycodestyle_runfiles:
        _add_runfile(sub_commands, f.short_path, runfiles_dir+ '/' + pycodestyle.basename + '.runfiles/krpc/' + f.short_path)
    sub_commands.append('%s/%s %s' % (runfiles_dir, pycodestyle.basename, ' '.join(pycodestyle_args)))
    sub_commands.append('rm -rf %s' % runfiles_dir)

    # Run pylint
    runfiles_dir = out.path + '.runfiles/krpc'
    sub_commands.append('rm -rf %s' % runfiles_dir)
    _add_runfile(sub_commands, pylint.short_path, runfiles_dir + '/' + pylint.basename)
    for f in pylint_runfiles:
        _add_runfile(sub_commands, f.short_path, runfiles_dir+ '/' + pylint.basename + '.runfiles/krpc/' + f.short_path)
    # Set pythonpath so that pylint finds the dependent packages from the virtual environment
    sub_commands.append('pylibdir=`find env/lib -maxdepth 1 -name "python*"`')
    sub_commands.append('PYTHONPATH=${pylibdir}/site-packages PYLINTHOME=%s %s/%s %s' % (runfiles_dir, runfiles_dir, pylint.basename, ' '.join(pylint_args)))
    sub_commands.append('rm -rf %s' % runfiles_dir)

    ctx.actions.write(
        ctx.outputs.executable,
        content = ' &&\n'.join(sub_commands)+'\n',
        is_executable = True
    )

    return struct(
        name = ctx.label.name,
        runfiles = ctx.runfiles(files = runfiles)
    )

py_lint_test = rule(
    implementation = _lint_impl,
    attrs = {
        'pkg': attr.label(allow_single_file=True),
        'pkg_name': attr.string(),
        'srcs': attr.label_list(allow_files=True),
        'deps': attr.label_list(allow_files=True),
        'pycodestyle_config': attr.label(allow_single_file=True),
        'pylint_config': attr.label(allow_single_file=True),
        'pycodestyle': attr.label(default=Label('//tools/build/pycodestyle'), executable=True, cfg='host'),
        'pylint': attr.label(default=Label('//tools/build/pylint'), executable=True, cfg='host')
    },
    test = True
)
