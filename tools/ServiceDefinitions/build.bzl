def _impl(ctx):
    args = ['--output=%s' % ctx.outputs.out.path, ctx.attr.service] + [x.lib.path for x in ctx.attr.assemblies]
    ctx.action(
        inputs = ctx.files.assemblies,
        outputs = [ctx.outputs.out],
        arguments = args,
        progress_message = 'Generating service definitions for %s' % ctx.outputs.out.short_path,
        executable = ctx.executable._service_definitions_tool
    )

service_definitions = rule(
    implementation=_impl,
    attrs={
        'assemblies': attr.label_list(allow_files=True),
        'service': attr.string(mandatory=True),
        'out': attr.output(mandatory=True),
        '_service_definitions_tool': attr.label(
            executable=True, default=Label('//tools/ServiceDefinitions:ServiceDefinitions'))
     }
)
