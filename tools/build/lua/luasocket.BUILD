"""LuaSocket, built from source as one library.

The rockspec builds socket.core, socket.unix and socket.serial as three shared objects,
each compiling the same handful of support files again. Linked into one binary those
copies collide, so everything the client needs is compiled once here instead. Only the
luaopen_ entry points differ between the modules, and //tools/build/lua registers those.
"""

load("@rules_cc//cc:defs.bzl", "cc_library")

cc_library(
    name = "luasocket",
    srcs = [
        "src/auxiliar.c",
        "src/buffer.c",
        "src/except.c",
        "src/inet.c",
        "src/io.c",
        "src/luasocket.c",
        "src/mime.c",
        "src/options.c",
        "src/select.c",
        "src/tcp.c",
        "src/timeout.c",
        "src/udp.c",
    ] + glob(["src/*.h"]) + select({
        "@platforms//os:windows": ["src/wsocket.c"],
        # unix domain sockets, which windows has no equivalent of.
        "//conditions:default": [
            "src/unix.c",
            "src/usocket.c",
        ],
    }),
    local_defines = ["LUASOCKET_DEBUG"] + select({
        "@platforms//os:osx": ["UNIX_HAS_SUN_LEN"],
        "//conditions:default": [],
    }),
    linkopts = select({
        # The sockets are winsock's on Windows, which is a library of its own rather
        # than part of the C runtime.
        "@platforms//os:windows": ["ws2_32.lib"],
        "//conditions:default": [],
    }),
    visibility = ["//visibility:public"],
    deps = ["@lua//:lua"],
)

filegroup(
    name = "lua_srcs",
    srcs = [
        "src/ftp.lua",
        "src/headers.lua",
        "src/http.lua",
        "src/ltn12.lua",
        "src/mime.lua",
        "src/smtp.lua",
        "src/socket.lua",
        "src/tp.lua",
        "src/url.lua",
    ],
    visibility = ["//visibility:public"],
)
