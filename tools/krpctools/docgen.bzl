" docgen tool "

def _page_path(src):
    """The path of a template within its package, such as api/space-center/parts.tmpl"""
    return src.owner.name

def _impl(ctx):
    # One action per template directory, so a new template reruns only its own directory
    groups = {}
    for src in ctx.files.srcs:
        groups.setdefault(_page_path(src).rpartition("/")[0], []).append(src)
    outputs = []
    for directory, srcs in sorted(groups.items()):
        documented = ctx.actions.declare_file("%s/%s-%s.documented.txt" % (
            ctx.attr.outdir,
            ctx.label.name,
            directory.replace("/", "-") or "root",
        ))
        outputs.extend(_generate(ctx, srcs, documented))
    return DefaultInfo(files = depset(outputs))

def _generate(ctx, srcs, documented):
    outputs = [documented]
    args = ctx.actions.args()
    args.add(ctx.attr.language)
    args.add(ctx.file.order)
    args.add_all(ctx.files.defs)
    for src in srcs:
        path = _page_path(src).removesuffix(".tmpl")
        out = ctx.actions.declare_file(ctx.attr.outdir + "/" + path + ".rst")
        args.add("--page", src)
        args.add(out)
        outputs.append(out)
    args.add("--documented", documented)
    args.use_param_file("@%s", use_always = True)
    args.set_param_file_format("multiline")

    ctx.actions.run(
        inputs = ctx.files.defs + srcs + [ctx.file.order],
        outputs = outputs,
        progress_message = "Generating %s documentation for %%{label}" % ctx.attr.language,
        mnemonic = "Docgen",
        executable = ctx.executable._docgen,
        arguments = [args],
    )
    return outputs

# Generates the API documentation for one language, a page per template
docgen = rule(
    implementation = _impl,
    attrs = {
        "outdir": attr.string(mandatory = True),
        "language": attr.string(mandatory = True),
        "srcs": attr.label_list(allow_files = [".tmpl"]),
        "defs": attr.label_list(allow_files = True, mandatory = True, allow_empty = True),
        "_docgen": attr.label(
            default = Label("//tools/krpctools:docgen"),
            executable = True,
            cfg = "exec",
        ),
        "order": attr.label(
            default = Label("//doc:order.txt"),
            allow_single_file = True,
            doc = "The ordering file, listing every member of the services in page order",
        ),
    },
)
