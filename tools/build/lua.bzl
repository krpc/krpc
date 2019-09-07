_LUA_VERSION = '5.1'

def _test_impl(ctx):
    sub_commands = []
    sub_commands.append('export HOME=/tmp')  # so that lua .cache directory is placed in /tmp
    for dep in [ctx.file._luaunit] + ctx.files.deps:
        sub_commands.append('luarocks --tree=lua-tree install %s' % dep.short_path)
    sub_commands.extend([
        'rm -rf lua-src',
        'unzip -q %s -d lua-src' % ctx.file.src.short_path,
        'CWD=`pwd`',
        '(cd lua-src/*/; luarocks --tree=$CWD/lua-tree make $CWD/%s)' % ctx.file.rockspec.short_path,
        'LUA_PATH="lua-tree/share/lua/'+_LUA_VERSION+'/?.lua;lua-tree/share/lua/'+_LUA_VERSION+'/?/init.lua;;" ' + \
        'LUA_CPATH="lua-tree/lib/lua/'+_LUA_VERSION+'/?.so;lua-tree/lib/lua/'+_LUA_VERSION+'/?/init.so;;" ' + \
        'lua'+_LUA_VERSION+' lua-tree/share/lua/'+_LUA_VERSION+'/krpc/test/init.lua -v'
    ])
    ctx.actions.write(
        output = ctx.outputs.executable,
        content = ' &&\n'.join(sub_commands)+'\n',
        is_executable = True
    )

    runfiles = ctx.runfiles(files = [ctx.file.src, ctx.file.rockspec, ctx.file._luaunit] + ctx.files.deps)

    return struct(
        name = ctx.label.name,
        out = ctx.outputs.executable,
        runfiles = runfiles
    )

lua_test = rule(
    implementation = _test_impl,
    attrs = {
        'rockspec': attr.label(allow_single_file=True),
        'src': attr.label(allow_single_file=True),
        'deps': attr.label_list(allow_files=True),
        '_luaunit': attr.label(default=Label('@lua_luaunit//file'), allow_single_file=True)
    },
    test = True
)
