def _outputs(attr):
    return {'out': attr.outdir + '/' + attr.src.name.replace('.tmpl','.rst')}

def _impl(ctx):
    language = ctx.attr.language
    src = ctx.file.src
    out = ctx.outputs.out

    # shell script to create generate_env
    generate_setup = ctx.new_file(ctx.configuration.genfiles_dir, 'generate-setup')
    # tarball containing python environment to run generate_tool
    generate_env = ctx.new_file(ctx.configuration.genfiles_dir, 'generate-env')
    # shell script to run generate.py using generate_env
    generate_tool = ctx.new_file(ctx.configuration.genfiles_dir, 'generate')
    # files depended on by generate_tool
    generate_tool_files = [ctx.file._generate_tool, generate_env] + ctx.files._generate_library

    # create generate_setup
    subcommands = [
        'virtualenv env --quiet --system-site-packages',
        'env/bin/pip install --quiet --no-deps %s' % ctx.file._python_client.path,
        'env/bin/pip install --quiet --no-deps %s' % ctx.file._protobuf_library.path,
        'env/bin/pip install --quiet --no-deps %s' % ctx.file._jinja2_library.path,
        'env/bin/pip install --quiet --no-deps %s' % ctx.file._markupsafe_library.path,
        'tar -cf %s env' % generate_env.path
    ]
    ctx.file_action(
        output = generate_setup,
        content = ' &&\n'.join(subcommands)+'\n',
        executable = True
    )

    # create generate_env
    ctx.action(
        inputs = [
            ctx.file._python_client,
            ctx.file._protobuf_library,
            ctx.file._jinja2_library,
            ctx.file._markupsafe_library
        ] + ctx.files._generate_library,
        outputs = [generate_env],
        progress_message = 'Creating documentation generator',
        executable = generate_setup,
        use_default_shell_env = True
    )

    # create generate_tool
    subcommands = [
        'tar -xf %s' % generate_env.path,
        'env/bin/python %s "$@"' % ctx.file._generate_tool.path
    ]
    ctx.file_action(
        output = generate_tool,
        content = ' &&\n'.join(subcommands)+'\n',
        executable = True
    )

    # run generate_tool
    ctx.action(
        inputs = generate_tool_files + ctx.files.defs + [
            src,
            ctx.file._generate_order,
            ctx.file._generate_cpp_macros,
            ctx.file._generate_csharp_macros,
            ctx.file._generate_lua_macros,
            ctx.file._generate_python_macros
        ],
        outputs = [out],
        progress_message = 'Generating %s documentation %s' % (language, out.short_path),
        executable = generate_tool,
        arguments = [
            language,
            src.path,
            out.path,
        ] + [f.path for f in ctx.files.defs] + [
            '--order-file=%s' % ctx.file._generate_order.path,
            '--cpp-macros=%s' % ctx.file._generate_cpp_macros.path,
            '--csharp-macros=%s' % ctx.file._generate_csharp_macros.path,
            '--lua-macros=%s' % ctx.file._generate_lua_macros.path,
            '--python-macros=%s' % ctx.file._generate_python_macros.path
        ],
        use_default_shell_env = True
    )

generate = rule(
    implementation = _impl,
    attrs = {
        'outdir': attr.string(mandatory=True),
        'language': attr.string(mandatory=True),
        'src': attr.label(allow_files=True, single_file=True),
        'defs': attr.label_list(allow_files=True, mandatory=True, non_empty=True),
        '_generate_tool': attr.label(default=Label('//doc:generate.py'),
                                     executable=True, allow_files=True, single_file=True),
        '_generate_library': attr.label(default=Label('//doc:generate_lib'),
                                        allow_files=True),
        '_generate_cpp_macros': attr.label(default=Label('//doc:lib/cpp.tmpl'),
                                           allow_files=True, single_file=True),
        '_generate_csharp_macros': attr.label(default=Label('//doc:lib/csharp.tmpl'),
                                              allow_files=True, single_file=True),
        '_generate_lua_macros': attr.label(default=Label('//doc:lib/lua.tmpl'),
                                           allow_files=True, single_file=True),
        '_generate_python_macros': attr.label(default=Label('//doc:lib/python.tmpl'),
                                              allow_files=True, single_file=True),
        '_generate_order': attr.label(default=Label('//doc:order.txt'),
                                      allow_files=True, single_file=True),
        '_python_client': attr.label(default=Label('//client/python'),
                                     allow_files=True, single_file=True),
        '_protobuf_library': attr.label(default=Label('@python.protobuf//file'),
                                        allow_files=True, single_file=True),
        '_jinja2_library': attr.label(default=Label('@python.jinja2//file'),
                                      allow_files=True, single_file=True),
        '_markupsafe_library': attr.label(default=Label('@python.markupsafe//file'),
                                          allow_files=True, single_file=True)
    },
    outputs = _outputs
)

def generate_multiple(name, outdir, language, srcs, defs):
    names = []
    for src in srcs:
        subname = name + src
        names.append(subname)
        generate(
            name = subname,
            outdir = outdir,
            language = language,
            src = src,
            defs = defs
        )
    native.filegroup(name=name, srcs=names)
