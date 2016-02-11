def _impl(ctx):
    language = ctx.attr.language
    service = ctx.attr.service
    defs = ctx.file.defs
    output = ctx.outputs.out

    ctx.action(
        inputs = [defs],
        outputs = [output],
        progress_message = 'Generating %s code for %s service' % (language, service),
        executable = ctx.file._clientgen,
        arguments = [language, service, defs.path, '--output=%s' % output.path]
    )

clientgen = rule(
    implementation = _impl,
    attrs = {
        'service': attr.string(mandatory=True),
        'defs': attr.label(allow_files=True, single_file=True),
        'out': attr.output(mandatory=True),
        'language': attr.string(mandatory=True),
        '_clientgen': attr.label(default=Label('//tools/clientgen:script'), executable=True, allow_files=True, single_file=True)
    }
)

def clientgen_cpp(name, service, defs, out):
    clientgen(
        name = name,
        service = service,
        defs = defs,
        out = out,
        language = 'cpp'
    )

def clientgen_csharp(name, service, defs, out):
    clientgen(
        name = name,
        service = service,
        defs = defs,
        out = out,
        language = 'csharp'
    )
