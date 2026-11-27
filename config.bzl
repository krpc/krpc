" global configuration "

load("@bazel_skylib//rules:write_file.bzl", "write_file")
load("@krpc_release//:defs.bzl", "release")

author = "djungelorm"

# The version of each release train, keyed by its tag prefix, as in python-v1.0.0: the release it
# works towards. KSA releases on its own tooling. A vx.y.z release takes every other train.
versions = {
    "ksp": "1.0.0",
    "python": "1.0.0",
    "csharp": "1.0.0",
    "java": "1.0.0",
    "cpp": "1.0.0",
    "cnano": "1.0.0",
    "lua": "1.0.0",
    "js": "1.0.0",
    "krpctools": "1.0.0",
    "ksa": "0.4.0",
}

# The assembly version of KRPC.Core, which is fixed. Each game mod ships core built from its own
# commit, so the mod's version identifies it
core_version = "1.0.0"

# Whether this is a dev build (tools/build/release/release.bzl)
dev = not release

ksp_version_max = "1.12.5.3190"
ksp_version_min = "1.12.5.3190"

# The version people read, and the NuGet, npm, Maven and Arduino version: x.y.z, or x.y.z-dev for a
# dev build. A format with no pre-releases, such as an assembly version, takes x.y.z
def readable_version(version):
    return version + "-dev" if dev else version

# Python version x.y.z[.dev0]
def python_version(version):
    return version + ".dev0" if dev else version

# KSP-AVC version fields, from the first three parts of a version
def avc_version(version):
    fields = zip(["MAJOR", "MINOR", "PATCH"], version.split("."))
    return ", ".join(['"%s": %s' % field for field in fields])

ksp_avc_version_max = avc_version(ksp_version_max)
ksp_avc_version_min = avc_version(ksp_version_min)

def _version_file_impl(ctx):
    ctx.actions.run_shell(
        inputs = [ctx.info_file],
        outputs = [ctx.outputs.out],
        command = 'commit=$(sed -n "s/^STABLE_GIT_COMMIT //p" "$1"); printf "%s\\n%s\\n" "$2" "${commit:-unknown}" > "$3"',
        arguments = [ctx.info_file.path, ctx.attr.version, ctx.outputs.out.path],
        mnemonic = "VersionFile",
    )

_version_file = rule(
    implementation = _version_file_impl,
    attrs = {
        "version": attr.string(mandatory = True),
        "out": attr.output(mandatory = True),
    },
)

# VERSION.txt, holding the readable form of a version and the commit, for a package. The commit
# comes from the workspace status, which Bazel writes on every build
def version_file(name, version, out = "VERSION.txt", **kwargs):
    _version_file(name = name, out = out, version = readable_version(version), **kwargs)

# The version.py of a Python package, holding its Python version
def python_version_file(name, out, version):
    write_file(
        name = name,
        out = out,
        content = ['__version__ = "%s"' % python_version(version), ""],
        newline = "unix",
    )
