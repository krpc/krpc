" Image tools "

def _impl(ctx):
    args = ctx.actions.args()
    args.add("--out", ctx.outputs.out)
    args.add(ctx.file.src)
    ctx.actions.run(
        executable = ctx.executable._rasterizer,
        arguments = [args],
        inputs = [ctx.file.src],
        outputs = [ctx.outputs.out],
        progress_message = "Generating PNG image %s" % ctx.outputs.out.short_path,
        mnemonic = "Rasterize",
    )

png_image = rule(
    implementation = _impl,
    attrs = {
        "src": attr.label(allow_single_file = [".svg"]),
        "_rasterizer": attr.label(
            default = Label("//tools/build:rasterize"),
            executable = True,
            cfg = "exec",
        ),
    },
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
