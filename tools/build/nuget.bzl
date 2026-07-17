" Builds a NuGet package from a compiled assembly "

# DotnetAssemblyRuntimeInfo is only available from rules_dotnet's private module.
load("@rules_dotnet//dotnet/private:providers.bzl", "DotnetAssemblyRuntimeInfo")  # buildifier: disable=bzl-visibility

_NUGET_FRAMEWORK_NAMES = {
    "net45": ".NETFramework4.5",
    "net472": ".NETFramework4.7.2",
}

def _nuget_package_out(id, version):
    return {"out": "%s.%s.nupkg" % (id, version)}

def _nuget_package_impl(ctx):
    info = ctx.attr.assembly[DotnetAssemblyRuntimeInfo]
    assembly_info = struct(lib = info.libs[0], doc = info.xml_docs[0])
    nuspec = ctx.actions.declare_file(ctx.attr.id + ".nuspec")
    assemblies = {
        ctx.attr.framework: assembly_info,
    }

    nuspec_contents = [
        '<?xml version="1.0"?>',
        '<package xmlns="http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd">',
        "<metadata>",
        "  <id>%s</id>" % ctx.attr.id,
        "  <version>%s</version>" % ctx.attr.version,
        "  <authors>%s</authors>" % ctx.attr.author,
        "  <description>%s</description>" % ctx.attr.description,
    ]
    if ctx.attr.project_url:
        nuspec_contents.append("  <projectUrl>%s</projectUrl>" % ctx.attr.project_url)
    if ctx.attr.project_url:
        nuspec_contents.append('  <license type="expression">%s</license>' % ctx.attr.license)
    nuspec_contents.extend([
        "  <frameworkAssemblies>",
    ])
    nuspec_contents.extend(['    <frameworkAssembly assemblyName="%s" targetFramework="%s"/>' % (x, ctx.attr.framework) for x in ctx.attr.framework_deps])
    nuspec_contents.extend([
        "  </frameworkAssemblies>",
        "  <dependencies>",
        '    <group targetFramework="%s">' % _NUGET_FRAMEWORK_NAMES[ctx.attr.framework],
    ])
    nuspec_contents.extend(['      <dependency id="%s" version="%s"/>' % x for x in ctx.attr.deps.items()])
    nuspec_contents.extend([
        "    </group>",
        "  </dependencies>",
        "</metadata>",
        "</package>",
    ])

    ctx.actions.write(
        output = nuspec,
        content = "\n".join(nuspec_contents),
    )

    # A .nupkg is a zip archive following the Open Packaging Conventions:
    # the nuspec and lib DLLs plus [Content_Types].xml, a relationships part
    # pointing at the nuspec, and a core-properties metadata part.
    content_types = "\n".join([
        '<?xml version="1.0" encoding="utf-8"?>',
        '<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">',
        '  <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml" />',
        '  <Default Extension="dll" ContentType="application/octet" />',
        '  <Default Extension="xml" ContentType="application/octet" />',
        '  <Default Extension="nuspec" ContentType="application/octet" />',
        '  <Default Extension="psmdcp" ContentType="application/vnd.openxmlformats-package.core-properties+xml" />',
        "</Types>",
    ])

    psmdcp_path = "package/services/metadata/core-properties/%s.psmdcp" % ctx.attr.id.lower().replace(".", "")

    rels = "\n".join([
        '<?xml version="1.0" encoding="utf-8"?>',
        '<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">',
        '  <Relationship Type="http://schemas.microsoft.com/packaging/2010/07/manifest" Target="/%s.nuspec" Id="R1" />' % ctx.attr.id,
        '  <Relationship Type="http://schemas.openxmlformats.org/package/2006/relationships/metadata/core-properties" Target="/%s" Id="R2" />' % psmdcp_path,
        "</Relationships>",
    ])

    psmdcp = "\n".join([
        '<?xml version="1.0" encoding="utf-8"?>',
        '<coreProperties xmlns:dc="http://purl.org/dc/elements/1.1/" xmlns="http://schemas.openxmlformats.org/package/2006/metadata/core-properties">',
        "  <dc:creator>%s</dc:creator>" % ctx.attr.author,
        "  <dc:description>%s</dc:description>" % ctx.attr.description,
        "  <dc:identifier>%s</dc:identifier>" % ctx.attr.id,
        "  <version>%s</version>" % ctx.attr.version,
        "</coreProperties>",
    ])

    # A .nupkg is an OPC zip. Write the generated XML parts to files and archive
    # them, with the pinned assemblies, via the write_zip helper.
    content_types_file = ctx.actions.declare_file(ctx.attr.id + ".Content_Types.xml")
    ctx.actions.write(output = content_types_file, content = content_types)
    rels_file = ctx.actions.declare_file(ctx.attr.id + ".rels")
    ctx.actions.write(output = rels_file, content = rels)
    psmdcp_file = ctx.actions.declare_file(ctx.attr.id + ".psmdcp")
    ctx.actions.write(output = psmdcp_file, content = psmdcp)

    args = ctx.actions.args()
    args.add("--out", ctx.outputs.out.path)
    args.add("--entry", "%s=%s.nuspec" % (nuspec.path, ctx.attr.id))
    args.add("--entry", "%s=[Content_Types].xml" % content_types_file.path)
    args.add("--entry", "%s=_rels/.rels" % rels_file.path)
    args.add("--entry", "%s=%s" % (psmdcp_file.path, psmdcp_path))

    inputs = [nuspec, content_types_file, rels_file, psmdcp_file]
    for framework, info in assemblies.items():
        args.add("--entry", "%s=lib/%s/%s.dll" % (info.lib.path, framework, ctx.attr.id))
        args.add("--entry", "%s=lib/%s/%s.xml" % (info.doc.path, framework, ctx.attr.id))
        inputs.extend([info.lib, info.doc])

    ctx.actions.run(
        mnemonic = "NuGetPackage",
        executable = ctx.executable._write_zip,
        arguments = [args],
        inputs = inputs,
        outputs = [ctx.outputs.out],
    )

nuget_package = rule(
    implementation = _nuget_package_impl,
    attrs = {
        "id": attr.string(mandatory = True),
        "assembly": attr.label(providers = [DotnetAssemblyRuntimeInfo]),
        "framework": attr.string(default = "net45", values = _NUGET_FRAMEWORK_NAMES.keys()),
        "version": attr.string(mandatory = True),
        "author": attr.string(mandatory = True),
        "project_url": attr.string(),
        "license": attr.string(),
        "description": attr.string(mandatory = True),
        "framework_deps": attr.string_list(),
        "deps": attr.string_dict(),
        "_write_zip": attr.label(
            default = Label("//tools/build:write_zip"),
            executable = True,
            cfg = "exec",
        ),
    },
    outputs = _nuget_package_out,
)
