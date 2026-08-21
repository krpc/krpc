"""Lua 5.1.5, built from the distribution's sources.

The C modules the client needs are linked into the interpreter rather than loaded from
shared objects, so the core is built without the dynamic loader and //tools/build/lua
supplies its own linit.c in place of the one here.
"""

load("@rules_cc//cc:defs.bzl", "cc_library")

# Upstream sources, compiled unmodified. The warnings the toolchain's -Wall finds in
# them are not ours to fix.
_NO_WARNINGS = select({
    "@platforms//os:windows": ["/w"],
    "//conditions:default": ["-w"],
})

cc_library(
    name = "lua",
    srcs = glob(
        ["src/*.c"],
        exclude = [
            # Registers the standard libraries; replaced so the statically linked C
            # modules can be registered alongside them.
            "src/linit.c",
            # The two programs the distribution ships. lua.c is built by :interpreter
            # below; the bytecode compiler is not wanted at all.
            "src/lua.c",
            "src/luac.c",
            "src/print.c",
        ],
    ),
    hdrs = glob(["src/*.h"]),
    copts = _NO_WARNINGS,
    defines = select({
        # MSVC deprecates the C library functions lua calls throughout (fopen, getenv,
        # tmpnam), which is otherwise a warning on nearly every file.
        "@platforms//os:windows": [
            "_CRT_SECURE_NO_WARNINGS",
            "_CRT_NONSTDC_NO_DEPRECATE",
        ],
        # Not LUA_USE_LINUX or LUA_USE_MACOSX, which add dlopen and readline on top of
        # this: nothing is loaded dynamically and the interpreter is never interactive.
        # These have to be visible to everything that includes luaconf.h, not just the
        # core, so that the whole binary agrees on how an error is thrown and what a
        # lua number is.
        "//conditions:default": ["LUA_USE_POSIX"],
    }),
    includes = ["src"],
    linkopts = select({
        "@platforms//os:windows": [],
        "//conditions:default": ["-lm"],
    }),
    visibility = ["//visibility:public"],
)

# The interpreter's main(). alwayslink so that it survives being reached through an
# archive, where nothing in the build refers to it by name.
cc_library(
    name = "interpreter",
    srcs = ["src/lua.c"],
    alwayslink = True,
    copts = _NO_WARNINGS,
    visibility = ["//visibility:public"],
    deps = [":lua"],
)
