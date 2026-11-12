" macros "

load("@bazel_skylib//rules:write_file.bzl", "write_file")
load("@rules_cc//cc:defs.bzl", "cc_binary")
load("@rules_dotnet//dotnet:defs.bzl", "csharp_library")
load("@rules_java//java:java_binary.bzl", "java_binary")
load("//service:registry.bzl", "SERVICES")

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
def cc_binary_multiple(name, srcs, deps, target_compatible_with = None):
    names = []
    for src in srcs:
        subname = name + "/" + src
        names.append(subname)
        cc_binary(
            name = subname,
            srcs = [src],
            deps = deps,
            target_compatible_with = target_compatible_with,
        )
    native.filegroup(
        name = name,
        srcs = names,
        target_compatible_with = target_compatible_with,
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

def api_toctree(name, language):
    """Writes the toctree of a client guide's API reference, src/<language>/api.inc.

    The pages are the core's, then those of the services in the registry.

    Args:
      name: The target name.
      language: The language of the client guide.
    """
    pages = ["krpc", "stdlib"] + [service.doc for service in SERVICES]
    write_file(
        name = name,
        out = "src/%s/api.inc" % language,
        content = [".. toctree::", "   :maxdepth: 1", ""] + [
            "   %s/api/%s" % (language, page)
            for page in pages
        ],
    )
