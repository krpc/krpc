" Generates an AssemblyInfo.cs source file for a C# assembly "

def _assembly_info_impl(ctx):
    content = ["using System;", "using System.Reflection;", "using System.Runtime.InteropServices;"]
    if len(ctx.attr.internals_visible_to) > 0:
        content.append("using System.Runtime.CompilerServices;")
    for x in ctx.attr.using:
        content.append("using %s;" % x)
    content.append('[assembly: AssemblyTitle ("%s")]' % ctx.attr.title)
    content.append('[assembly: AssemblyDescription ("%s")]' % ctx.attr.description)
    content.append('[assembly: AssemblyCopyright ("%s")]' % ctx.attr.copyright)
    content.append('[assembly: AssemblyVersion ("%s")]' % ctx.attr.version)

    for pkg in ctx.attr.internals_visible_to:
        content.append('[assembly: InternalsVisibleTo ("%s")]' % pkg)

    for k, v in ctx.attr.custom.items():
        content.append("[assembly: %s (%s)]" % (k, v))

    if ctx.attr.cls_compliant:
        cls_compliant = "true"
    else:
        cls_compliant = "false"
    content.append("[assembly: CLSCompliant (%s)]" % cls_compliant)

    if ctx.attr.com_visible:
        com_visible = "true"
    else:
        com_visible = "false"
    content.append("[assembly: ComVisible (%s)]" % com_visible)

    ctx.actions.write(
        output = ctx.outputs.out,
        content = "\n".join(content) + "\n",
    )

csharp_assembly_info = rule(
    implementation = _assembly_info_impl,
    attrs = {
        "title": attr.string(mandatory = True),
        "description": attr.string(),
        "copyright": attr.string(mandatory = True),
        "version": attr.string(mandatory = True),
        "using": attr.string_list(),
        "custom": attr.string_dict(),
        "internals_visible_to": attr.string_list(),
        "cls_compliant": attr.bool(default = True),
        "com_visible": attr.bool(default = False),
    },
    outputs = {"out": "%{name}.cs"},
)
