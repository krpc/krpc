def _impl(ctx):
    language = ctx.attr.language
    service = ctx.attr.service
    defs = ctx.file.defs
    output = ctx.outputs.out

    ctx.action(
        inputs = [defs],
        outputs = [output],
        progress_message = 'Generating %s code for %s service' % (language, service),
        executable = ctx.file._krpcgen,
        arguments = [language, service, defs.path, output.path]
    )

krpcgen = rule(
    implementation = _impl,
    attrs = {
        'service': attr.string(mandatory=True),
        'defs': attr.label(allow_files=True, single_file=True),
        'out': attr.output(mandatory=True),
        'language': attr.string(mandatory=True),
        '_krpcgen': attr.label(default=Label('//tools/krpcgen:script'), executable=True, allow_files=True, single_file=True)
    }
)

def krpcgen_cpp(name, service, defs, out):
    krpcgen(
        name = name,
        service = service,
        defs = defs,
        out = out,
        language = 'cpp'
    )

def krpcgen_csharp(name, service, defs, out):
    krpcgen(
        name = name,
        service = service,
        defs = defs,
        out = out,
        language = 'csharp'
    )
