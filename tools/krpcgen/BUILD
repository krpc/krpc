load('/tools/build/python', 'py_sdist', 'py_script')
load('/config', 'version')

license_text = """This license (GPL v3) applies to all parts of krpcgen except for the following:

  - krpcgen/bin/{mscorlib.dll,System.*.dll} are from the Mono project.
    See http://www.mono-project.com/docs/faq/licensing/

  - krpcgen/bin/Newtonsoft.Json.dll is under the MIT license.
    See https://www.nuget.org/packages/Newtonsoft.Json/

  - krpcgen/bin/NDesk.Options.dll is under the MIT license.
    See https://www.nuget.org/packages/NDesk.Options/

  - krpcgen/bin/Google.Protobuf.dll is from Google's protobuf project.
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
        ':license', '//:COPYING',
        'setup.py', 'MANIFEST.in', ':version',
        '//tools/ServiceDefinitions',
        '//tools/ServiceDefinitions:KRPC',
        '//tools/build/ksp:Google.Protobuf.dll',
        '//tools/build/mono-4.5:Newtonsoft.Json.dll',
        '//tools/build/mono-4.5:NDesk.Options.dll',
        '//tools/build/mono-4.5:mscorlib',
        '//tools/build/mono-4.5:System',
        '//tools/build/mono-4.5:System.Core',
        '//tools/build/mono-4.5:System.Runtime',
        '//tools/build/mono-4.5:System.IO',
        '//tools/build/mono-4.5:System.Xml',
        '//tools/build/mono-4.5:System.Xml.Linq'
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
