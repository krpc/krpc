load('/tools/build/python', 'py_sdist', 'py_test', 'py3_test', 'py_lint_test')
load('/config', 'version')

genrule(
    name = 'version',
    outs = ['krpctools/version.py'],
    cmd = 'echo "__version__ = \'%s\'" > "$@"' % version
)

py_sdist(
    name = 'krpctest',
    out = 'krpctest-%s.zip' % version,
    files = [
        'README.txt', '//:version', 'CHANGES.txt',
        'LICENSE', '//:COPYING',
        'setup.py', 'MANIFEST.in', ':version'
    ] + glob(['krpctest/**/*.py']),
    path_map = {
        'tools/krpctest/': ''
    },
    visibility = ['//visibility:public']
)

test_suite(
    name = 'test',
    tests = [':test2', ':test3', ':lint']
)

deps = [
    '//client/python',
    '@python_six//file',
    '@python_protobuf//file'
]

py_test(
    name = 'test2',
    src = ':krpctest',
    pkg = 'krpctest-'+version,
    deps = deps + ['@python_enum34//file'],
    size = 'small'
)

py3_test(
    name = 'test3',
    src = ':krpctest',
    pkg = 'krpctest-'+version,
    deps = deps,
    size = 'small'
)

py_lint_test(
    name = 'lint',
    src = ':krpctest',
    pkg = 'krpctest',
    deps = deps + ['@python_enum34//file'],
    rcfile = 'pylint.rc',
    size = 'small'
)
