" The KSA game assemblies, from a local install, and the StarMap loader the game runs under "

load("@bazel_skylib//lib:paths.bzl", "paths")
load("@bazel_tools//tools/build_defs/repo:http.bzl", "http_archive")
load("//experimental/ksa:config.bzl", "starmap_sha256", "starmap_version")

# The assemblies the mod compiles against, other than the Brutal.* ones
_ASSEMBLIES = ["KSA", "Tomlet", "BepuUtilities", "Planet.Core", "Planet.Render.Core"]

# A constraint no platform has, named for the fix
_INCOMPATIBLE = '["%s"]' % Label("//experimental/ksa/tools/build/ksa:ksa_dir_is_set")

def _ksa_impl(rctx):
    content = [
        'load("@rules_dotnet//dotnet:defs.bzl", "import_dll", "import_library")',
        "",
        'package(default_visibility = ["//visibility:public"])',
        "",
    ]
    ksa_dir = rctx.getenv("KSA_DIR")
    if not ksa_dir:
        # Without an install the targets are incompatible, so `bazel build //...` skips the mod
        for name in _ASSEMBLIES + ["Brutal", "assemblies"]:
            content.append("filegroup(name = %r, target_compatible_with = %s)" % (name, _INCOMPATIBLE))
        rctx.file("BUILD.bazel", "\n".join(content) + "\n")
        return
    if not paths.is_absolute(ksa_dir):
        fail("KSA_DIR must be an absolute path, not %s" % ksa_dir)
    ksa = rctx.path(ksa_dir)
    rctx.watch(ksa.get_child("KSA.dll"))
    if not ksa.get_child("KSA.dll").exists:
        fail("KSA_DIR is set to %s, which holds no KSA.dll" % ksa_dir)

    brutal = []
    for entry in ksa.readdir():
        if entry.basename.endswith(".dll"):
            rctx.symlink(entry, entry.basename)
            if entry.basename.startswith("Brutal."):
                brutal.append(entry.basename)
    for name in _ASSEMBLIES:
        content.append("import_dll(name = %r, dll = %r)" % (name, name + ".dll"))
    content.append(
        "import_library(name = \"Brutal\", library_name = \"Brutal\", version = \"0.0.0\", " +
        "libs = %r, refs = %r)" % (sorted(brutal), sorted(brutal)),
    )
    content.append("")
    content.append("# Every assembly of the game, which the service scan resolves types from")
    content.append('filegroup(name = "assemblies", srcs = glob(["*.dll"]))')
    rctx.file("BUILD.bazel", "\n".join(content) + "\n")

ksa_repository = repository_rule(
    implementation = _ksa_impl,
    doc = "The game assemblies of the KSA install at KSA_DIR. With KSA_DIR unset, the KSA targets are skipped.",
    local = True,
)

def _ksa_extension_impl(_mctx):
    ksa_repository(name = "ksa")
    http_archive(
        name = "starmap",
        build_file_content = 'filegroup(name = "files", srcs = glob(["**"]), visibility = ["//visibility:public"])',
        sha256 = starmap_sha256,
        url = "https://github.com/StarMapLoader/StarMap/releases/download/%s/StarMap-%s.zip" % (
            starmap_version,
            starmap_version,
        ),
    )

ksa = module_extension(implementation = _ksa_extension_impl)
