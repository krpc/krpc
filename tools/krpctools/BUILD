load('/tools/build/python', 'py_sdist', 'py_script', 'py_lint_test')
load('/config', 'version')

license_text = """This license (GPL v3) applies to all parts of krpc.clientgen except for the following:

  - krpctools/bin/{mscorlib.dll,System.*.dll} are from the Mono project.
    See http://www.mono-project.com/docs/faq/licensing/

  - krpctools/bin/Newtonsoft.Json.dll is under the MIT license.
    See https://www.nuget.org/packages/Newtonsoft.Json/

  - krpctools/bin/NDesk.Options.dll is under the MIT license.
    See https://www.nuget.org/packages/NDesk.Options/

  - krpctools/bin/Google.Protobuf.dll is from Google's protobuf project.
    See https://github.com/google/protobuf

Copyright 2015-2016 djungelorm

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.
"""

genrule(
    name = 'license',
    outs = ['LICENSE'],
    cmd = 'echo "%s" > "$@"' % license_text
)

py_sdist(
    name = 'krpctools',
    out = 'krpctools-%s.zip' % version,
    files = [
        'README.txt', '//:version', 'CHANGES.txt',
        ':license', '//:COPYING',
        'setup.py', 'MANIFEST.in', '//:python_version',
        '//tools/ServiceDefinitions',
        '//tools/ServiceDefinitions:KRPC',
        '//tools/build/mono-4.5:Google.Protobuf.dll',
        '//tools/build/mono-4.5:Newtonsoft.Json.dll',
        '//tools/build/mono-4.5:NDesk.Options.dll',
        '//tools/build/mono-4.5:mscorlib',
        '//tools/build/mono-4.5:System',
        '//tools/build/mono-4.5:System.Core',
        '//tools/build/mono-4.5:System.Runtime',
        '//tools/build/mono-4.5:System.IO',
        '//tools/build/mono-4.5:System.Xml'
    ] + glob(['krpctools/**/*.py', 'krpctools/**/*.tmpl']),
    path_map = {
        'version.py': 'krpctools/version.py',
        'tools/krpctools/': '',
        'tools/ServiceDefinitions/ServiceDefinitions.exe': 'krpctools/bin/ServiceDefinitions.exe',
        'tools/ServiceDefinitions/KRPC.dll': 'krpctools/bin/KRPC.dll',
        'tools/ServiceDefinitions/KRPC.xml': 'krpctools/bin/KRPC.xml',
        'tools/build/mono-4.5/': 'krpctools/bin/'
    }
)

deps = [
    '//client/python',
    '@python_protobuf//file',
    '@python_enum34//file',
    '@python_six//file',
    '@python_jinja2//file',
    '@python_markupsafe//file'
]

py_script(
    name = 'clientgen',
    script = 'krpc-clientgen',
    pkg = ':krpctools',
    deps = deps,
    visibility = ['//visibility:public']
)

py_script(
    name = 'docgen',
    script = 'krpc-docgen',
    pkg = ':krpctools',
    deps = deps,
    visibility = ['//visibility:public']
)

test_suite(
    name = 'test',
    tests = [':lint']
)

py_lint_test(
    name = 'lint',
    pkg = ':krpctools',
    pkg_name = 'krpctools',
    deps = deps,
    pylint_config = 'pylint.rc',
    size = 'small'
)
