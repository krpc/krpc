load('/tools/build/python', 'py_sdist', 'py_test', 'py3_test', 'py_lint_test')
load('/config', 'version', 'python_version')

py_sdist(
    name = 'krpctest',
    out = 'krpctest-%s.zip' % version,
    files = [
        'README.txt', '//:version', 'CHANGES.txt',
        'LICENSE', '//:COPYING',
        'setup.py', 'MANIFEST.in', '//:python_version',
        'krpctest/krpctest.sfs',
        'krpctest/krpctest_career.sfs',
    ] + glob(['krpctest/**/*.py']),
    path_map = {
        'version.py': 'krpctest/version.py',
        'tools/krpctest/': ''
    },
    visibility = ['//visibility:public']
)

test_suite(
    name = 'test',
    tests = [':test2', ':test3', ':lint']
)

test_suite(
    name = 'ci-test',
    tests = [':test2', ':test3']
)

deps = [
    '//client/python',
    '@python_six//file',
    '@python_protobuf//file'
]

py_test(
    name = 'test2',
    src = ':krpctest',
    pkg = 'krpctest-'+python_version,
    deps = deps + ['@python_enum34//file'],
    size = 'small'
)

py3_test(
    name = 'test3',
    src = ':krpctest',
    pkg = 'krpctest-'+python_version,
    deps = deps,
    size = 'small'
)

py_lint_test(
    name = 'lint',
    pkg = ':krpctest',
    pkg_name = 'krpctest',
    deps = deps + ['@python_enum34//file'],
    pylint_config = 'pylint.rc',
    size = 'small'
)
