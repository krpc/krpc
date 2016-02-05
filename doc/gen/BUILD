load('/tools/build/python', 'py_sdist', 'py_script')
load('/config', 'version')

py_sdist(
    name = 'krpcdocgen',
    out = 'krpcdocgen-%s.zip' % version,
    files = [
        'setup.py', 'MANIFEST.in', 'README.txt', '//:version'
    ] + glob(['krpcdocgen/*.py', 'krpcdocgen/*.tmpl']),
    path_map = {
        'doc/gen/': ''
    }
)

py_script(
    name = 'script',
    script = 'krpcdocgen',
    pkg = ':krpcdocgen',
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
