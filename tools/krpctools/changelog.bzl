" changeloggen tool "

def _impl(ctx):
    out = ctx.outputs.out

    args = ctx.actions.args()
    args.add(out.path)

    inputs = []
    for target, name in ctx.attr.components.items():
        for f in target.files.to_list():
            args.add("--entry")
            args.add(name)
            args.add(f.path)
            inputs.append(f)

    ctx.actions.run(
        inputs = inputs,
        outputs = [out],
        progress_message = "Generating changelog %s" % out.path,
        executable = ctx.executable._changeloggen,
        arguments = [args],
    )

changeloggen = rule(
    implementation = _impl,
    attrs = {
        "out": attr.output(mandatory = True),
        # Maps each CHANGES.txt target to the display name shown on the page.
        # Insertion order is preserved and becomes the per-version component
        # order on the rendered page.
        "components": attr.label_keyed_string_dict(
            allow_files = True,
            mandatory = True,
        ),
        "_changeloggen": attr.label(
            default = Label("//tools/krpctools:changeloggen"),
            executable = True,
            cfg = "exec",
        ),
    },
)
