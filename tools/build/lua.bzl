" lua build tools "

_LUA_VERSION = "5.1"

_LUA_ENV = (
    'LUA_PATH="lua-tree/share/lua/' + _LUA_VERSION + "/?.lua;lua-tree/share/lua/" +
    _LUA_VERSION + '/?/init.lua;;" ' +
    'LUA_CPATH="lua-tree/lib/lua/' + _LUA_VERSION + "/?.so;lua-tree/lib/lua/" +
    _LUA_VERSION + '/?/init.so;;" '
)

def _install_commands(ctx, rocks):
    """Commands that build a lua tree holding the client and everything it needs.

    There is no maintained hermetic Lua ruleset, so this drives the system luarocks and lua
    interpreter: install the dependency rocks into a tree, then build the client's own rock
    from its source zip into the same tree.
    """
    commands = ["export HOME=/tmp"]  # so that lua's .cache directory is placed in /tmp
    for rock in rocks:
        commands.append("luarocks --tree=lua-tree install %s" % rock.short_path)
    commands.extend([
        "rm -rf lua-src",
        "unzip -q %s -d lua-src" % ctx.file.src.short_path,
        "CWD=`pwd`",
        "(cd lua-src/*/; luarocks --tree=$CWD/lua-tree make $CWD/%s)" % ctx.file.rockspec.short_path,
    ])
    return commands

def _write(ctx, commands, files):
    # With a shebang the script can be executed directly, not only by a test runner that
    # already knows to hand it to a shell.
    ctx.actions.write(
        output = ctx.outputs.executable,
        content = "#!/usr/bin/env bash\nset -e\n" + " &&\n".join(commands) + "\n",
        is_executable = True,
    )
    return DefaultInfo(
        executable = ctx.outputs.executable,
        runfiles = ctx.runfiles(files = files),
    )

def _test_impl(ctx):
    rocks = [ctx.file._luaunit] + ctx.files.deps
    commands = _install_commands(ctx, rocks) + [
        _LUA_ENV + "lua" + _LUA_VERSION + " lua-tree/share/lua/" + _LUA_VERSION +
        "/krpc/test/init.lua -v",
    ]
    return _write(ctx, commands, [ctx.file.src, ctx.file.rockspec] + rocks)

def _binary_impl(ctx):
    # The script is run from outside the tree rather than installed into it, so that a program
    # which is not part of the client - a benchmark, say - does not ship in the released rock.
    #
    # Building the tree is noisy, and that noise is not what the program printed, so it goes to
    # stderr: standard output belongs to the script alone.
    commands = [
        "{ " + " &&\n".join(_install_commands(ctx, ctx.files.deps)) + " ; } >&2",
        _LUA_ENV + "lua" + _LUA_VERSION + " %s \"$@\"" % ctx.file.main.short_path,
    ]
    return _write(ctx, commands, [ctx.file.src, ctx.file.rockspec, ctx.file.main] + ctx.files.deps)

_common_attrs = {
    "rockspec": attr.label(allow_single_file = True),
    "src": attr.label(allow_single_file = True),
    "deps": attr.label_list(allow_files = True),
}

_lua_test = rule(
    implementation = _test_impl,
    attrs = dict(_common_attrs, _luaunit = attr.label(
        default = Label("@lua_luaunit//file"),
        allow_single_file = True,
    )),
    test = True,
)

_lua_binary = rule(
    implementation = _binary_impl,
    attrs = dict(_common_attrs, main = attr.label(allow_single_file = True)),
    executable = True,
)

# buildifier: disable=function-docstring
def lua_test(**kwargs):
    # Runs the generated bash test through luarocks + the system lua interpreter;
    # there is no maintained hermetic Lua Bazel ruleset, so this is Linux-only.
    _lua_test(
        target_compatible_with = ["@platforms//os:linux"],
        **kwargs
    )

# buildifier: disable=function-docstring
def lua_binary(**kwargs):
    # As lua_test, but runs the given script instead of the test suite. Linux-only for the
    # same reason.
    _lua_binary(
        target_compatible_with = ["@platforms//os:linux"],
        **kwargs
    )
