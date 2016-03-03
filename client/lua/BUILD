load('/tools/build/lua', 'lua_rock', 'lua_test')
load('/tools/build/client_test', 'client_test')
load('/config', 'version')

filegroup(
    name = 'lua',
    srcs = [':client', ':rockspec'],
    visibility = ['//visibility:public']
)

lua_rock(
    name = 'client',
    pkg = 'krpc',
    version = '%s-0' % version,
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
    outs = ['krpc-'+version+'-0.rockspec'],
    cmd = 'sed \'s/%VERSION%/'+version+'/g\' $(locations :rockspec.tmpl) > $@',
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
    srcrock = ':client',
    deps = [
        '@lua_luafilesystem//file',
        '@lua_luasocket//file',
        '@lua_penlight//file',
        '@lua_protobuf//file'
    ],
    tags = ['requires-network'],
    size = 'small'
)
