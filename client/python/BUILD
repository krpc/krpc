load('/tools/build/python', 'py_sdist', 'py_test', 'py3_test')
load('/tools/build/client_test', 'client_test')
load('/config', 'version')

py_sdist(
    name = 'python',
    out = 'krpc-python-%s.zip' % version,
    files = [
        '//:readme', '//:version', 'CHANGES.txt',
        'LICENSE', '//:COPYING', '//:COPYING.LESSER',
        'setup.py', 'MANIFEST.in',
        '//protobuf:py',
        '//protobuf:py-test'
    ] + glob(['krpc/**/*']),
    path_map = {
        'client/python/': '',
        'protobuf/': 'krpc/schema/',
        'protobuf/Test.py': 'krpc/test/schema/Test.py'
    },
    visibility = ['//visibility:public']
)

test_suite(
    name = 'test',
    tests = [':test2', ':test3']
)

client_test(
    name = 'test2',
    test_executable = ':py2test',
    server_executable = '//tools/TestServer',
    rpc_port = '50010',
    stream_port = '50011',
    tags = ['requires-network'],
    size = 'small'
)

client_test(
    name = 'test3',
    test_executable = ':py3test',
    server_executable = '//tools/TestServer',
    rpc_port = '50012',
    stream_port = '50013',
    tags = ['requires-network'],
    size = 'small'
)

py_test(
    name = 'py2test',
    src = ':python',
    pkg = 'krpc-'+version,
    deps = ['@python.six//file', '@python.protobuf//file', '@python.enum34//file'],
    tags = ['requires-network'],
    size = 'small'
)

py3_test(
    name = 'py3test',
    src = ':python',
    pkg = 'krpc-'+version,
    #use3 = True,
    deps = ['@python.six//file', '@python.protobuf//file'],
    tags = ['requires-network'],
    size = 'small'
)
