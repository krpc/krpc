load('/tools/build/pkg', 'pkg_zip')
load('/config', 'version')

exports_files(['COPYING', 'COPYING.LESSER'])

readme_text = """
Documentation: https://krpc.github.io/krpc

Forum release thread: http://forum.kerbalspaceprogram.com/index.php?/topic/130742-105-krpc-remote-control-your-ships-using-python-c-c-lua-v021-10th-feb-2016/

Forum development thread: http://forum.kerbalspaceprogram.com/threads/69313

"""

license_text = """This license (LGPL v3) applies to all parts of kRPC except for the following:

  - GameData/kRPC/KRPC.SpaceCenter.* is under the GPLv3 license.
    See LICENSE.KRPC.SpaceCenter

  - GameData/kRPC/Google.Protobuf.dll is a binary from Google's protobuf project.
    See LICENSE.Google.Protobuf

  - schema/* is under the MIT license. See schema/LICENSE

Copyright 2015-2016 djungelorm

This program is free software: you can redistribute it and/or modify
it under the terms of the Lesser GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
Lesser GNU General Public License for more details.

You should have received a copy of the Lesser GNU General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.
"""

version_parts = version.split('.')
ksp_avc_version = """{
  "NAME": "kRPC",
  "URL": "http://ksp-avc.cybutek.net/version.php?id=254",
  "DOWNLOAD": "https://github.com/krpc/krpc/releases/latest",
  "VERSION": {
    "MAJOR": %s,
    "MINOR": %s,
    "PATCH": %s
  },
  "KSP_VERSION": {
    "MAJOR": 1,
    "MINOR": 0,
    "PATCH": 5
  }
}""" % (version_parts[0], version_parts[1], version_parts[2])

genrule(
    name = 'license',
    outs = ['LICENSE'],
    cmd = 'echo "%s" > "$@"' % license_text
)

genrule(
    name = 'readme',
    outs = ['README.txt'],
    cmd = 'echo "%s" > "$@"' % readme_text,
    visibility = ['//visibility:public']
)

genrule(
    name = 'version',
    outs = ['VERSION.txt'],
    cmd = 'echo "%s" > "$@"' % version,
    visibility = ['//visibility:public']
)

genrule(
    name = 'ksp-avc-version',
    outs = ['kRPC.version'],
    cmd = 'echo \'%s\' > "$@"' % ksp_avc_version
)

pkg_zip(
    name = 'krpc',
    out = 'krpc-%s.zip' % version,
    files = [
        ':readme',
        ':license',
        ':version',
        ':ksp-avc-version',
        'COPYING',
        'COPYING.LESSER',
        # Server
        '//server',
        '//service/SpaceCenter',
        '//service/KerbalAlarmClock',
        '//service/InfernalRobotics',
        '//tools/build/ksp:Google.Protobuf',
        '//tools/build/protobuf:LICENSE',
        # Clients
        '//client/python',
        '//client/cpp',
        '//client/csharp',
        '//client/lua',
        '//client/java',
        # Schema
        '//protobuf:krpc.proto',
        '//protobuf:csharp',
        '//protobuf:py',
        '//protobuf:cpp',
        '//protobuf:lua',
        '//protobuf:java',
        '//protobuf:LICENSE',
        # Docs
        '//doc:latex',
    ],
    path_map = {
        'kRPC.version': 'GameData/kRPC/kRPC.version',
        # Server
        'server/': 'GameData/kRPC/',
        'server/src/icons': 'GameData/kRPC/icons',
        'service/SpaceCenter/CHANGES.txt': 'GameData/kRPC/CHANGES.SpaceCenter.txt',
        'service/KerbalAlarmClock/CHANGES.txt': 'GameData/kRPC/CHANGES.KerbalAlarmClock.txt',
        'service/InfernalRobotics/CHANGES.txt': 'GameData/kRPC/CHANGES.InfernalRobotics.txt',
        'service/SpaceCenter/': 'GameData/kRPC/',
        'service/KerbalAlarmClock/': 'GameData/kRPC/',
        'service/InfernalRobotics/': 'GameData/kRPC/',
        'tools/build/ksp/': 'GameData/kRPC/',
        'tools/build/protobuf/LICENSE': 'LICENSE.Google.Protobuf',
        'service/SpaceCenter/LICENSE': 'LICENSE.KRPC.SpaceCenter',
        # Schema
        'protobuf/': 'schema/',
        # Docs
        'doc/latex.pdf': 'kRPC.pdf',
    }
)

test_suite(
    name = 'test',
    tests = [
        '//server:test',
        '//client/python:test',
        '//client/cpp:test',
        '//client/lua:test',
        '//client/csharp:test',
        '//client/java:test'
    ]
)

filegroup(
    name = 'csproj',
    srcs = [
        '//server',
        '//server:test',
        '//service/SpaceCenter',
        '//service/InfernalRobotics',
        '//service/KerbalAlarmClock',
        '//client/csharp',
        '//client/csharp:test',
        '//tools/ServiceDefinitions',
        '//tools/TestingTools',
        '//tools/TestServer'
    ]
)
