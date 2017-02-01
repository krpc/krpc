load('/tools/build/python', 'py_sdist', 'py_test', 'py3_test', 'py_lint_test')
load('/tools/build/client_test', 'client_test')
load('/config', 'version', 'python_version')

genrule(
    name = 'python',
    srcs = [':python-pypi'],
    outs = ['krpc-python-%s.zip' % version],
    cmd = 'cp "$<" "$@"',
    output_to_bindir = True,
    visibility = ['//visibility:public']
)

py_sdist(
    name = 'python-pypi',
    out = 'krpc-%s.zip' % version,
    files = [
        '//:readme', '//:version', 'CHANGES.txt',
        'LICENSE', '//:COPYING', '//:COPYING.LESSER',
        'setup.py', 'MANIFEST.in', '//:python_version',
        '//protobuf:python'
    ] + glob(['krpc/**/*']),
    path_map = {
        'version.py': 'krpc/version.py',
        'client/python/': '',
        'protobuf/': 'krpc/schema/'
    }
)

test_suite(
    name = 'test',
    tests = [':test2', ':test3', ':lint']
)

test_suite(
    name = 'ci-test',
    tests = [':test2', ':test3']
)

client_test(
    name = 'test2',
    test_executable = ':py2test',
    server_executable = '//tools/TestServer',
    tags = ['requires-network'],
    size = 'small'
)

client_test(
    name = 'test3',
    test_executable = ':py3test',
    server_executable = '//tools/TestServer',
    tags = ['requires-network'],
    size = 'small'
)

deps = ['@python_six//file', '@python_protobuf//file']

py_test(
    name = 'py2test',
    src = ':python',
    pkg = 'krpc-'+python_version,
    deps = deps + ['@python_enum34//file'],
    tags = ['requires-network'],
    size = 'small'
)

py3_test(
    name = 'py3test',
    src = ':python',
    pkg = 'krpc-'+python_version,
    deps = deps,
    tags = ['requires-network'],
    size = 'small'
)

py_lint_test(
    name = 'lint',
    pkg = ':python',
    pkg_name = 'krpc',
    srcs = glob(['krpc/**/*']),
    deps = deps + ['@python_enum34//file'],
    pep8_config = 'pep8.ini',
    pylint_config = 'pylint.rc',
    size = 'small'
)
