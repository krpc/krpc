load('/tools/build/python', 'py_sdist')
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
    }
)
