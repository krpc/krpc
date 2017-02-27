load('/tools/build/python', 'py_sdist', 'py_test', 'py3_test', 'py_lint_test')
load('/tools/build/client_test', 'client_test')
load('/config', 'version', 'python_version')

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

test_suite(
    name = 'ci-test',
    tests = [':wstest']
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
    '@python_six//file',
    '@python_protobuf//file',
    '@python_enum34//file',
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
