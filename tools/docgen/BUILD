load('/tools/build/python', 'py_sdist', 'py_script')
load('/config', 'version')

genrule(
    name = 'version',
    outs = ['krpc/docgen/version.py'],
    cmd = 'echo "__version__ = \'%s\'" > "$@"' % version
)

py_sdist(
    name = 'krpc.docgen',
    out = 'krpc.docgen-%s.zip' % version,
    files = [
        'setup.py', 'MANIFEST.in', 'README.txt', '//:version', ':version'
    ] + glob(['krpc/docgen/*.py', 'krpc/docgen/*.tmpl']),
    path_map = {
        'tools/docgen/': ''
    }
)

py_script(
    name = 'script',
    script = 'krpc-docgen',
    pkg = ':krpc.docgen',
    deps = [
        '//client/python',
        '@python.protobuf//file',
        '@python.enum34//file',
        '@python.six//file',
        '@python.jinja2//file',
        '@python.markupsafe//file'
    ],
    visibility = ['//visibility:public']
)
