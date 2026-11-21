" Rules for the kRPC for KSA documentation "

def _expand_files_impl(ctx):
    outs = []
    package = ctx.label.package + "/"
    for src in ctx.files.srcs:
        path = src.short_path.removeprefix(package)
        out = ctx.actions.declare_file(ctx.attr.prefix + "/" + path)
        ctx.actions.expand_template(
            template = src,
            output = out,
            substitutions = ctx.attr.substitutions,
        )
        outs.append(out)
    return [DefaultInfo(files = depset(outs))]

expand_files = rule(
    implementation = _expand_files_impl,
    doc = "Copies files of the package under prefix, with the substitutions applied.",
    attrs = {
        "srcs": attr.label_list(allow_files = True, mandatory = True),
        "prefix": attr.string(mandatory = True),
        "substitutions": attr.string_dict(),
    },
)
