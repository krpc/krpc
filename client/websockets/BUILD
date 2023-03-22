load('//tools/build:python.bzl', 'py_sdist', 'py_test', 'py_lint_test')
load('//tools/build:client_test.bzl', 'client_test')
load('//:config.bzl', 'version', 'python_version')

py_sdist(
    name = 'krpcwebsockets',
    out = 'krpcwebsockets-%s.zip' % version,
    files = [
        '//:version', '//:python_version', 'setup.py', 'README.txt'
    ] + glob(['krpcwebsockets/**/*']),
    path_map = {
        'version.py': 'krpcwebsockets/version.py',
        'client/websockets/': ''
    }
)

test_suite(
    name = 'test',
    tests = [':wstest', ':lint']
)

client_test(
    name = 'wstest',
    test_executable = ':wstestexe',
    server_executable = '//tools/TestServer',
    server_type = 'websockets',
    tags = ['requires-network'],
    size = 'small'
)

deps = [
    '@python_protobuf//file',
    '@python_websocket_client//file',
    '//client/python'
]

py_test(
    name = 'wstestexe',
    src = ':krpcwebsockets',
    pkg = 'krpcwebsockets-'+python_version,
    deps = deps,
    tags = ['requires-network'],
    size = 'small'
)

py_lint_test(
    name = 'lint',
    pkg = ':krpcwebsockets',
    pkg_name = 'krpcwebsockets',
    srcs = glob(['krpcwebsockets/**/*']),
    deps = deps,
    pylint_config = 'pylint.rc',
    size = 'small'
)
