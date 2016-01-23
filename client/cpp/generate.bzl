def _impl(ctx):
    service = ctx.attr.service
    srcs = ctx.files.srcs
    out = ctx.outputs.out

    subcommands = [
        'virtualenv env --quiet --system-site-packages',
        'virtualenv env --quiet --relocatable',
        'env/bin/pip install --quiet --no-deps %s' % ctx.file._python_client.path,
        'env/bin/pip install --quiet --no-deps %s' % ctx.file._protobuf_library.path,
        'env/bin/pip install --quiet --no-deps %s' % ctx.file._markupsafe_library.path,
        'env/bin/pip install --quiet --no-deps %s' % ctx.file._jinja2_library.path,
        'env/bin/python %s "$@"' % ctx.file._generate_tool.path
    ]

    generate = ctx.new_file(ctx.configuration.genfiles_dir, 'generate')
    ctx.file_action(
        output = generate,
        content = ' &&\n'.join(subcommands)+'\n',
        executable = True
    )

    ctx.action(
        inputs = [
            ctx.file._generate_tool,
            ctx.file._generate_template,
            ctx.file._python_client,
            ctx.file._protobuf_library,
            ctx.file._jinja2_library,
            ctx.file._markupsafe_library
        ] + srcs,
        outputs = [out],
        progress_message = 'Generating C++ header for %s service' % service,
        executable = generate,
        arguments = [ctx.file._generate_template.path, service, out.path] + [f.path for f in srcs],
        use_default_shell_env = True
    )

generate = rule(
    implementation = _impl,
    attrs = {
        'service': attr.string(mandatory=True),
        'srcs': attr.label_list(allow_files=True, mandatory=True, non_empty=True),
        'out': attr.output(mandatory=True),
        '_generate_tool': attr.label(default=Label('//client/cpp:generate.py'),
                                     executable=True, allow_files=True, single_file=True),
        '_generate_template': attr.label(default=Label('//client/cpp:generate.tmpl'),
                                         allow_files=True, single_file=True),
        '_python_client': attr.label(default=Label('//client/python'),
                                     allow_files=True, single_file=True),
        '_protobuf_library': attr.label(default=Label('@python.protobuf//file'),
                                        allow_files=True, single_file=True),
        '_jinja2_library': attr.label(default=Label('@python.jinja2//file'),
                                      allow_files=True, single_file=True),
        '_markupsafe_library': attr.label(default=Label('@python.markupsafe//file'),
                                          allow_files=True, single_file=True)
    }
)
