" macros "

load("@bazel_skylib//rules:write_file.bzl", "write_file")
load("@rules_cc//cc:defs.bzl", "cc_binary")
load("@rules_dotnet//dotnet:defs.bzl", "csharp_binary", "csharp_library")
load("@rules_java//java:java_binary.bzl", "java_binary")
load("//service:registry.bzl", "SERVICES")
load("//tools/build:lua.bzl", "lua_binary")

# buildifier: disable=function-docstring
def csharp_library_multiple(name, srcs, target_framework, deps):
    names = []
    for src in srcs:
        subname = name + "/" + src
        names.append(subname)
        csharp_library(
            name = subname,
            srcs = [src],
            target_frameworks = [target_framework],
            deps = deps,
        )
    native.filegroup(name = name, srcs = names)

# buildifier: disable=function-docstring
def csharp_binary_multiple(name, srcs, target_framework, deps):
    names = []
    for src in srcs:
        subname = name + "/" + src
        names.append(subname)
        csharp_binary(
            name = subname,
            srcs = [src],
            # The assembly is named for the file, as each binary is in a folder of its own
            out = src.rpartition("/")[2].rpartition(".")[0],
            target_frameworks = [target_framework],
            deps = deps,
        )
    native.filegroup(name = name, srcs = names)

# buildifier: disable=function-docstring
def cc_binary_multiple(name, srcs, deps, copts = [], target_compatible_with = None, testonly = False):
    names = []
    for src in srcs:
        subname = name + "/" + src
        names.append(subname)
        cc_binary(
            name = subname,
            srcs = [src],
            copts = copts,
            deps = deps,
            target_compatible_with = target_compatible_with,
            testonly = testonly,
        )
    native.filegroup(
        name = name,
        srcs = names,
        target_compatible_with = target_compatible_with,
        testonly = testonly,
    )
    return [":" + subname for subname in names]

# buildifier: disable=function-docstring
def java_binary_multiple(name, srcs, deps, copts):
    names = []
    for src in srcs:
        subname = name + "/" + src
        names.append(subname)
        java_binary(
            name = subname,
            main_class = src.rpartition("/")[2].rpartition(".")[0],
            srcs = [src],
            deps = deps,
            javacopts = copts,
        )
    native.filegroup(name = name, srcs = names)

# buildifier: disable=function-docstring
def lua_binary_multiple(name, srcs, deps, testonly = False):
    names = []
    for src in srcs:
        subname = name + "/" + src
        names.append(subname)
        lua_binary(
            name = subname,
            main = src,
            deps = deps,
            testonly = testonly,
        )
    native.filegroup(name = name, srcs = names, testonly = testonly)

def api_toctree(name, language):
    """Writes the toctree of a client guide's API reference, src/<language>/api.inc.

    The pages are in three groups: the core's services, the stock services, then the mod
    services. The services keep their order in the registry.

    Args:
      name: The target name.
      language: The language of the client guide.
    """
    groups = [
        ("Server", ["krpc", "stdlib"]),
        ("Game", [service.doc for service in SERVICES if not service.mods]),
        ("Mods", [service.doc for service in SERVICES if service.mods]),
    ]
    content = []
    for caption, pages in groups:
        if content:
            content.append("")
        content += [".. toctree::", "   :caption: " + caption, "   :maxdepth: 1", ""]
        content += ["   %s/api/%s" % (language, page) for page in pages]
    write_file(
        name = name,
        out = "src/%s/api.inc" % language,
        content = content,
    )
