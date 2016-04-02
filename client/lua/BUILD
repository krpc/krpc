load('/tools/build/lua', 'lua_rock', 'lua_test')
load('/tools/build/client_test', 'client_test')
load('/config', 'version', 'lua_version')

filegroup(
    name = 'lua',
    srcs = [':zip'],
    visibility = ['//visibility:public']
)

genrule(
    name = 'zip',
    srcs = [':rock'],
    outs = ['krpc-lua-%s.zip' % version],
    cmd = 'ln -f -r -s "$<" "$@"',
    output_to_bindir = True
)

lua_rock(
    name = 'rock',
    pkg = 'krpc',
    version = lua_version,
    rockspec = ':rockspec',
    srcs = glob(['krpc/**/*.lua']) + [
        '//:readme', '//:version', 'CHANGES.txt',
        'LICENSE', '//:COPYING', '//:COPYING.LESSER',
        '//protobuf:lua'
    ],
    path_map = {
        'client/lua/CHANGES.txt': 'CHANGES.txt',
        'client/lua/LICENSE': 'LICENSE',
        'client/': '',
        'protobuf/KRPC.lua': 'lua/krpc/schema/KRPC.lua'
    }
)

genrule(
    name = 'rockspec',
    srcs = ['rockspec.tmpl'],
    outs = ['krpc-'+lua_version+'.rockspec'],
    cmd = 'sed \'s/%VERSION%/'+lua_version+'/g\' $(locations :rockspec.tmpl) > $@',
    output_to_bindir = True
)

client_test(
    name = 'test',
    test_executable = ':luatest',
    server_executable = '//tools/TestServer',
    tags = ['requires-network'],
    size = 'small'
)

lua_test(
    name = 'luatest',
    rockspec = ':rockspec',
    srcrock = ':rock',
    deps = [
        '@lua_luafilesystem//file',
        '@lua_luasocket//file',
        '@lua_penlight//file',
        '@lua_protobuf//file'
    ],
    tags = ['requires-network'],
    size = 'small'
)
