def _add_runfile(sub_commands, path, runfile_path):
    sub_commands.extend([
        "mkdir -p `dirname %s`" % runfile_path,
        'ln -f -s "`pwd`/%s" "`pwd`/%s"' % (path, runfile_path),
    ])

def _java_checkstyle_impl(ctx):
    out = ctx.outputs.executable
    config = ctx.file.config
    properties = ctx.file.properties
    srcs = ctx.files.srcs
    checkstyle = ctx.executable.checkstyle
    checkstyle_runfiles = ctx.attr.checkstyle.default_runfiles.files.to_list()
    runfiles = [checkstyle] + checkstyle_runfiles + [config, properties] + srcs
    sub_commands = []

    runfiles_dir = out.path + ".runfiles/krpc"
    sub_commands.append("rm -rf %s" % runfiles_dir)
    _add_runfile(sub_commands, checkstyle.short_path, runfiles_dir + "/" + checkstyle.basename)
    for f in checkstyle_runfiles:
        _add_runfile(sub_commands, f.short_path, runfiles_dir + "/" + checkstyle.basename + ".runfiles/krpc/" + f.short_path)

    args = ["-c", config.short_path, "-p", properties.short_path]
    args.extend([x.short_path for x in srcs])
    sub_commands.append("%s/%s %s" % (runfiles_dir, checkstyle.basename, " ".join(args)))

    ctx.actions.write(
        ctx.outputs.executable,
        content = " &&\n".join(sub_commands) + "\n",
        is_executable = True,
    )

    return struct(
        name = ctx.label.name,
        runfiles = ctx.runfiles(files = runfiles),
    )

java_checkstyle_test = rule(
    implementation = _java_checkstyle_impl,
    attrs = {
        "srcs": attr.label_list(allow_files = True),
        "config": attr.label(
            default = Label("//tools/build/checkstyle:google_checks.xml"),
            allow_single_file = True,
        ),
        "properties": attr.label(
            default = Label("//tools/build/checkstyle:default.properties"),
            allow_single_file = True,
        ),
        "checkstyle": attr.label(default = Label("//tools/build/checkstyle"), executable = True, cfg = "host"),
    },
    test = True,
)
