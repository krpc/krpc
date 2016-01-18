def _impl(ctx):
    output = ctx.outputs.out
    input = ctx.file.src
    ctx.action(
        inputs = [input],
        outputs = [output],
        progress_message = 'Generating PNG image %s' % output.short_path,
        #TODO: need quiet flag for inkscape. Don't want to have to pipe output to /dev/null
        command = 'inkscape --without-gui --export-png=%s %s 1>/dev/null 2>/dev/null' % (output.path, input.path)
    )

png_image = rule(
    implementation = _impl,
    attrs = {'src': attr.label(allow_files=FileType(['.svg']), single_file=True)},
    outputs = {'out': '%{name}.png'}
)

def png_images(name, srcs, visibility=None):
    png_srcs = []
    for src in srcs:
        png_name = src.replace('.svg','')
        png_srcs.append(png_name)
        png_image(name=png_name, src=src)
    native.filegroup(name=name, srcs=png_srcs, visibility=visibility)
