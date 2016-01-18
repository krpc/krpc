load('/tools/build/python', 'py_sdist', 'py_test')
load('/version', 'version')

py_sdist(
    name = 'python',
    out = 'krpc-python-%s.zip' % version,
    files = [
        '//:readme', '//:version', 'CHANGES.txt',
        'LICENSE', '//:COPYING', '//:COPYING.LESSER',
        'setup.py', 'MANIFEST.in',
        '//protobuf:py',
        '//protobuf:py-test',
        '//tools/TestServer:TestServer',
        '//server:kRPC',
        '//tools/build/protobuf:Google.Protobuf',
        '//tools/build/protobuf:LICENSE'
    ] + glob(['krpc/**/*']),
    path_map = {
        'client/python/': '',
        'protobuf/': 'krpc/schema/',
        'protobuf/Test.py': 'krpc/test/schema/Test.py',
        'tools/TestServer/': 'bin/TestServer/',
        'server/': 'bin/TestServer/',
        'tools/build/protobuf/': 'bin/TestServer/',
        'tools/build/protobuf/LICENSE': 'bin/TestServer/LICENSE.Google.Protobuf'
    },
    visibility = ['//visibility:public']
)

py_test(
    name = 'test',
    src = ':python',
    pkg = 'krpc-'+version,
    deps = ['@python.protobuf//file']
)
