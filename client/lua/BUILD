load('/tools/build/package', 'package_archive')
load('/tools/build/lua', 'lua_test')
load('/config', 'version')

package_archive(
    name = 'lua',
    out = 'krpc-lua-%s.zip' % version,
    files = glob(['krpc/**/*.lua']) + ['//protobuf:lua', '//protobuf:lua-test'],
    path_map = {
        'client/': 'krpc-%s-0/' % version,
        'protobuf/KRPC.lua': 'krpc-%s-0/lua/krpc/schema/KRPC.lua' % version,
        'protobuf/Test.lua': 'krpc-%s-0/lua/krpc/test/Test.lua' % version
    },
    visibility = ['//visibility:public']
)

lua_test(
    name = 'test',
    src = ':lua',
    deps = ['@lua.luafilesystem//file', '@lua.protobuf//file']
)
