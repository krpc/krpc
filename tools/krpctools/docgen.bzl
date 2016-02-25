def _outputs(attr):
    return {'out': attr.outdir + '/' + attr.src.name.replace('.tmpl','.rst')}

def _impl(ctx):
    language = ctx.attr.language
    src = ctx.file.src
    out = ctx.outputs.out

    args = [
        language,
        src.path,
        ctx.file._order.path,
        out.path
    ]
    args.extend([f.path for f in ctx.files.defs])

    ctx.action(
        inputs = ctx.files.defs + [src, ctx.file._order],
        outputs = [out],
        progress_message = 'Generating %s documentation %s' % (language, out.path),
        executable = ctx.file._docgen,
        arguments = args
    )

docgen = rule(
    implementation = _impl,
    attrs = {
        'outdir': attr.string(mandatory=True),
        'language': attr.string(mandatory=True),
        'src': attr.label(allow_files=True, single_file=True),
        'defs': attr.label_list(allow_files=True, mandatory=True, non_empty=True),
        '_docgen': attr.label(default=Label('//tools/krpctools:docgen'), executable=True, allow_files=True, single_file=True),
        '_order': attr.label(default=Label('//doc:order.txt'), allow_files=True, single_file=True)
    },
    outputs = _outputs
)

def docgen_multiple(name, outdir, language, srcs, defs):
    names = []
    for src in srcs:
        subname = name + src
        names.append(subname)
        docgen(
            name = subname,
            outdir = outdir,
            language = language,
            src = src,
            defs = defs
        )
    native.filegroup(name=name, srcs=names)
