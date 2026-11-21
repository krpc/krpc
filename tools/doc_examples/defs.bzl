"""The targets of a game's documentation examples: one per example file, so each builds and runs
on its own, and the runner of the JavaScript examples."""

load("@aspect_rules_js//js:defs.bzl", "js_binary")
load("@rules_cc//cc:defs.bzl", "cc_binary")
load("@rules_dotnet//dotnet:defs.bzl", "csharp_binary", "csharp_library")
load("@rules_java//java:java_binary.bzl", "java_binary")
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

def example_js_runner(name, scripts, data):
    """Runs a JavaScript example against the game's WebSockets server.

    The in-game tests run it with the example's path under scripts.

    Args:
      name: The target name.
      scripts: The examples' folder, relative to the repository root.
      data: The examples, as a js_library, and the files they import.
    """
    js_binary(
        name = name,
        testonly = True,
        data = data + [
            "//tools/doc_examples:example-hooks",
            "//client/js:lib",
            "//client/js:package_json",
        ],
        entry_point = "//tools/doc_examples:run-example",
        fixed_args = [scripts],
    )
