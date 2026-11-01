" changeloggen tool "

def _impl(ctx):
    out = ctx.outputs.out

    args = ctx.actions.args()
    args.add(out.path)
    args.add("--version", ctx.attr.version)

    inputs = []
    for f in ctx.files.changelogs:
        args.add("--changelog", f.path)
        inputs.append(f)
    for f in ctx.files.fragments:
        args.add("--fragment", f.path)
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
        # The CHANGELOG.md of each component, named and ordered on the page by
        # COMPONENTS in tools/release/changelog.py
        "changelogs": attr.label_list(
            allow_files = True,
            mandatory = True,
        ),
        # The changelog fragments, merged into the unreleased sections
        "fragments": attr.label_list(allow_files = True),
        # The version the fragments are for, from config.bzl
        "version": attr.string(mandatory = True),
        "_changeloggen": attr.label(
            default = Label("//doc:changeloggen"),
            executable = True,
            cfg = "exec",
        ),
    },
)
