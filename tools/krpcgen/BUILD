load('/tools/build/python', 'py_sdist', 'py_script')
load('/config', 'version')

genrule(
    name = 'version',
    outs = ['krpcgen/version.py'],
    cmd = 'echo "__version__ = \'%s\'" > "$@"' % version,
    visibility = ['//visibility:public']
)

py_sdist(
    name = 'krpcgen',
    out = 'krpcgen-%s.zip' % version,
    files = [
        'README.txt', '//:version', 'CHANGES.txt',
        'LICENSE', '//:COPYING',
        'setup.py', 'MANIFEST.in', ':version',
        '//tools/ServiceDefinitions',
        '//tools/ServiceDefinitions:KRPC',
        '//tools/build/ksp:Google.Protobuf.dll',
        '//tools/build/mono-4.5:Newtonsoft.Json.dll' #TODO: include licenses for binaries
    ] + glob(['krpcgen/*.py', 'krpcgen/*.tmpl']),
    path_map = {
        'tools/krpcgen/': '',
        'tools/ServiceDefinitions/ServiceDefinitions.exe': 'krpcgen/bin/ServiceDefinitions.exe',
        'tools/ServiceDefinitions/KRPC.dll': 'krpcgen/bin/KRPC.dll',
        'tools/ServiceDefinitions/KRPC.xml': 'krpcgen/bin/KRPC.xml',
        'tools/build/mono-4.5/': 'krpcgen/bin/',
        'tools/build/ksp/': 'krpcgen/bin/'
    }
)

py_script(
    name = 'script',
    script = 'krpcgen',
    pkg = ':krpcgen',
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
