load('/tools/build/package', 'package_archive')
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
        '//protobuf:lua', '//protobuf:lua-test'
    ],
    path_map = {
        'client/lua/CHANGES.txt': 'CHANGES.txt',
        'client/lua/LICENSE': 'LICENSE',
        'client/': '',
        'protobuf/KRPC.lua': 'lua/krpc/schema/KRPC.lua',
        'protobuf/Test.lua': 'lua/krpc/test/Test.lua'
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
    rpc_port = '50010',
    stream_port = '50011',
    tags = ['local', 'requires-network'],
    size = 'small'
)

lua_test(
    name = 'luatest',
    rockspec = ':rockspec',
    srcrock = ':client',
    deps = [
        '@lua.luafilesystem//file',
        '@lua.luasocket//file',
        '@lua.penlight//file',
        '@lua.protobuf//file'
    ],
    size = 'small',
    tags = ['local']
)
