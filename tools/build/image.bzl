" Image tools "

def _impl(ctx):
    output = ctx.outputs.out
    input = ctx.file.src
    ctx.actions.run_shell(
        inputs = [input],
        outputs = [output],
        progress_message = "Generating PNG image %s" % output.short_path,
        command = "rsvg-convert --format=png -o %s %s" % (output.path, input.path),
    )

png_image = rule(
    implementation = _impl,
    attrs = {"src": attr.label(allow_single_file = [".svg"])},
    outputs = {"out": "%{name}.png"},
)

# buildifier: disable=function-docstring
def png_images(name, srcs, visibility = None):
    png_srcs = []
    for src in srcs:
        png_name = src.replace(".svg", "")
        png_srcs.append(png_name)
        png_image(name = png_name, src = src)
    native.filegroup(name = name, srcs = png_srcs, visibility = visibility)
