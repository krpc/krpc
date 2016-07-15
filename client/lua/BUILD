load('/tools/build/pkg', 'pkg_zip')
load('/tools/build/lua', 'lua_test')
load('/tools/build/client_test', 'client_test')
load('/config', 'version', 'lua_version')

pkg_zip(
    name = 'lua',
    out = 'krpc-lua-%s.zip' % version,
    files = [
        ':rockspec',
        ':src'
    ],
    path_map = {
        'client/lua/': ''
    },
    visibility = ['//visibility:public']
)

genrule(
    name = 'rockspec',
    srcs = ['rockspec.tmpl'],
    outs = ['krpc-'+lua_version+'-0.rockspec'],
    cmd = 'sed \'s/%VERSION%/'+version+'/g\' $(locations :rockspec.tmpl) | sed \'s/%LUA_VERSION%/'+lua_version+'/g\' > $@',
    output_to_bindir = True
)

pkg_zip(
    name = 'src',
    out = 'krpc-%s.zip' % version,
    files = glob(['krpc/**/*.lua']) + [
        '//:readme', '//:version', 'CHANGES.txt',
        'LICENSE', '//:COPYING', '//:COPYING.LESSER',
        '//protobuf:lua'
    ],
    path_map = {
        'COPYING': 'krpc-%s/COPYING' % version,
        'COPYING.LESSER': 'krpc-%s/COPYING.LESSER' % version,
        'README.txt': 'krpc-%s/README.txt' % version,
        'VERSION.txt': 'krpc-%s/VERSION.txt' % version,
        'client/lua/CHANGES.txt': 'krpc-%s/CHANGES.txt' % version,
        'client/lua/LICENSE': 'krpc-%s/LICENSE' % version,
        'client/': 'krpc-%s/' % version,
        'protobuf/KRPC.lua': 'krpc-%s/lua/krpc/schema/KRPC.lua' % version
    }
)

test_suite(
    name = 'test',
    tests = [':client']
)

test_suite(
    name = 'ci-test',
    tests = [':client']
)

client_test(
    name = 'client',
    test_executable = ':luatest',
    server_executable = '//tools/TestServer',
    tags = ['requires-network'],
    size = 'small'
)

lua_test(
    name = 'luatest',
    rockspec = ':rockspec',
    src = ':src',
    deps = [
        '@lua_luafilesystem//file',
        '@lua_luasocket//file',
        '@lua_penlight//file',
        '@lua_protobuf//file'
    ],
    tags = ['requires-network'],
    size = 'small'
)
