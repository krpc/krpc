" Rules for the kRPC for KSA mod folder "

def _files_in_impl(ctx):
    suffix = "/" + ctx.attr.directory
    files = [f for f in ctx.files.srcs if f.dirname.endswith(suffix) and f.basename.endswith(ctx.attr.extension)]
    if not files:
        fail("No %s files in %s" % (ctx.attr.extension, ctx.attr.directory))
    return [DefaultInfo(files = depset(files))]

files_in = rule(
    implementation = _files_in_impl,
    doc = "The files of srcs in a directory, with an extension, such as the assemblies of a NuGet runtime pack.",
    attrs = {
        "srcs": attr.label_list(allow_files = True, mandatory = True),
        "directory": attr.string(mandatory = True),
        "extension": attr.string(default = ".dll"),
    },
)

# The native library of System.IO.Ports for each platform the mod ships it for
NATIVE_PORTS = {
    "linux-x64": "libSystem.IO.Ports.Native.so",
    "osx-arm64": "libSystem.IO.Ports.Native.dylib",
}

# The platforms the mod ships the ASP.NET Core runtime pack for, which the gRPC server runs on
ASPNETCORE_RIDS = ["linux-x64", "osx-arm64", "win-x64"]

def deps_json(mod_version, core_version, protobuf_version, ports_version):
    """The deps.json of KRPC.KSA, from which StarMap's load context resolves the mod's assemblies.

    It picks the System.IO.Ports build for the platform from the runtimes folder.

    Args:
      mod_version: The version of the mod.
      core_version: The version of KRPC.Core.
      protobuf_version: The version of the Google.Protobuf package.
      ports_version: The version of the System.IO.Ports package.

    Returns:
      The file content.
    """
    native_ports = {rid: "runtime.%s.runtime.native.System.IO.Ports" % rid for rid in NATIVE_PORTS}
    targets = {
        "KRPC.KSA/" + mod_version: {
            "dependencies": {
                "Google.Protobuf": protobuf_version,
                "KRPC.Core": core_version,
                "KRPC.KSA.Service": mod_version,
                "System.IO.Ports": ports_version,
            },
            "runtime": {"KRPC.KSA.dll": {}},
        },
        "KRPC.KSA.Service/" + mod_version: {"runtime": {"KRPC.KSA.Service.dll": {}}},
        "KRPC.Core/" + core_version: {"runtime": {"KRPC.Core.dll": {}}},
        "Google.Protobuf/" + protobuf_version: {"runtime": {"lib/net8.0/Google.Protobuf.dll": {}}},
        "System.IO.Ports/" + ports_version: {
            "dependencies": {package: ports_version for package in native_ports.values()},
            "runtime": {"lib/net10.0/System.IO.Ports.dll": {}},
            "runtimeTargets": {
                "runtimes/%s/lib/net10.0/System.IO.Ports.dll" % rid: {"rid": rid, "assetType": "runtime"}
                for rid in ["unix", "win"]
            },
        },
    }
    for rid, package in native_ports.items():
        targets[package + "/" + ports_version] = {
            "runtimeTargets": {
                "runtimes/%s/native/%s" % (rid, NATIVE_PORTS[rid]): {"rid": rid, "assetType": "native"},
            },
        }
    libraries = {}
    for name in targets:
        project = name.startswith("KRPC.")
        libraries[name] = {"type": "project" if project else "package", "serviceable": False, "sha512": ""}
        if not project:
            libraries[name]["path"] = name.lower()
    return json.encode_indent({
        "runtimeTarget": {"name": ".NETCoreApp,Version=v10.0", "signature": ""},
        "compilationOptions": {},
        "targets": {".NETCoreApp,Version=v10.0": targets},
        "libraries": libraries,
    }, indent = "  ") + "\n"
