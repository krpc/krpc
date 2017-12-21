load('//tools/build:python.bzl', 'py_sdist', 'py_test', 'py3_test', 'py_lint_test')
load('//tools/build:client_test.bzl', 'client_test')
load('//:config.bzl', 'version', 'python_version')

py_sdist(
    name = 'krpcserialio',
    out = 'krpcserialio-%s.zip' % version,
    files = [
        '//:version', '//:python_version', 'setup.py', 'README.txt'
    ] + glob(['krpcserialio/**/*']),
    path_map = {
        'version.py': 'krpcserialio/version.py',
        'client/serialio/': ''
    }
)

test_suite(
    name = 'test',
    tests = [':iotest', ':lint']
)

test_suite(
    name = 'ci-test',
    tests = [':iotest']
)

client_test(
    name = 'iotest',
    test_executable = ':iotestexe',
    server_executable = '//tools/TestServer',
    server_type = 'serialio',
    tags = ['requires-network', 'local'],
    size = 'small'
)

deps = [
    '@python_six//file',
    '@python_protobuf//file',
    '@python_enum34//file',
    '//client/python'
]

py_test(
    name = 'iotestexe',
    src = ':krpcserialio',
    pkg = 'krpcserialio-'+python_version,
    deps = deps,
    tags = ['requires-network'],
    size = 'small'
)

py_lint_test(
    name = 'lint',
    pkg = ':krpcserialio',
    pkg_name = 'krpcserialio',
    srcs = glob(['krpcserialio/**/*']),
    deps = deps,
    pylint_config = 'pylint.rc',
    size = 'small'
)
