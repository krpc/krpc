_LUA_VERSION = '5.1'

def _apply_path_map(path_map, path):
    """ Apply the path mappings to a path.
        Replaces the longest prefix match from the mapping. """
    matchlen = 0
    match = path
    for x,y in path_map.items():
        if path.startswith(x):
            if len(x) > matchlen:
                match = y + path[len(x):]
                matchlen = len(x)
    return match

def _rock_out(attr):
    return { 'out': attr.pkg + "-" + attr.version + '.src.rock' }

def _rock_impl(ctx):
    pkg = ctx.attr.pkg
    version = ctx.attr.version
    rockspec = ctx.file.rockspec
    output = ctx.outputs.out
    inputs = ctx.files.srcs
    path_map = ctx.attr.path_map

    # Symlink all the files to a staging directory
    # to get the required directory structure
    staging_dir = output.basename + '.lua-rock-tmp'
    staging_dir_path = output.path.replace(
        ctx.configuration.bin_dir.path, ctx.configuration.genfiles_dir.path) + '.lua-rock-tmp'
    staging_inputs = []
    for input in inputs:
        staging_path = staging_dir+'/'+pkg+'-'+version+'/'+_apply_path_map(path_map, input.short_path)
        staging_file = ctx.new_file(ctx.configuration.genfiles_dir, staging_path)

        ctx.action(
            mnemonic = 'StageLuaRockFile',
            inputs = [input],
            outputs = [staging_file],
            command = 'ln -f -r -s %s %s' % (input.path, staging_file.path)
        )
        staging_inputs.append(staging_file)

    # Build the rock
    ctx.action(
        inputs = staging_inputs,
        outputs = [output],
        progress_message = 'Building Lua rock %s' % output.short_path,
        command = '(CWD=`pwd` && cd %s && zip --quiet -r $CWD/%s ./)' % (staging_dir_path, output.path)
    )

def _test_impl(ctx):
    sub_commands = []
    for dep in ctx.files.deps:
        sub_commands.append('luarocks --tree=lua-tree install %s' % dep.short_path)
    sub_commands.extend([
        'rm -rf lua-src',
        'unzip -q %s -d lua-src' % ctx.file.srcrock.short_path,
        'CWD=`pwd`',
        '(cd lua-src/*/; luarocks --tree=$CWD/lua-tree make $CWD/%s)' % ctx.file.rockspec.short_path,
        'ln -f -s %s luaunit.lua' % ctx.file._luaunit.short_path,
        'LUA_PATH="lua-tree/share/lua/'+_LUA_VERSION+'/?.lua;lua-tree/share/lua/'+_LUA_VERSION+'/?/init.lua;;" ' + \
        'LUA_CPATH="lua-tree/lib/lua/'+_LUA_VERSION+'/?.so;lua-tree/lib/lua/'+_LUA_VERSION+'/?/init.so;;" ' + \
        'lua'+_LUA_VERSION+' lua-tree/share/lua/5.1/krpc/test/init.lua -v'
    ])
    ctx.file_action(
        output = ctx.outputs.executable,
        content = ' &&\n'.join(sub_commands)+'\n',
        executable = True
    )

    runfiles = ctx.runfiles(files = [ctx.file.srcrock, ctx.file.rockspec, ctx.file._luaunit] + ctx.files.deps)

    return struct(
        name = ctx.label.name,
        out = ctx.outputs.executable,
        runfiles = runfiles
    )

lua_rock = rule(
    implementation = _rock_impl,
    attrs = {
        'pkg': attr.string(mandatory=True),
        'version': attr.string(mandatory=True),
        'rockspec': attr.label(allow_files=True, single_file=True),
        'srcs': attr.label_list(allow_files=True, mandatory=True, non_empty=True),
        'path_map': attr.string_dict()
    },
    outputs = _rock_out
)

lua_test = rule(
    implementation = _test_impl,
    attrs = {
        'rockspec': attr.label(allow_files=True, single_file=True),
        'srcrock': attr.label(allow_files=True, single_file=True),
        'deps': attr.label_list(allow_files=True),
        '_luaunit': attr.label(default=Label('@lua.luaunit//file'), allow_files=True, single_file=True)
    },
    test = True
)
