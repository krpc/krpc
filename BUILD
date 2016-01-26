load('/tools/build/package', 'package_archive')
load('/config', 'version')

exports_files(['COPYING', 'COPYING.LESSER'])

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

genrule(
    name = 'license',
    outs = ['LICENSE'],
    cmd = 'echo "%s" > "$@"' % license_text
)

genrule(
    name = 'readme',
    srcs = ['README.md'],
    outs = ['README.txt'],
    cmd = 'cp $(location //:README.md) "$@"',
    visibility = ['//visibility:public']
)

genrule(
    name = 'version',
    outs = ['VERSION.txt'],
    cmd = 'echo "%s" > "$@"' % version,
    visibility = ['//visibility:public']
)

package_archive(
    name = 'krpc',
    out = 'krpc-%s.zip' % version,
    files = [
        ':readme',
        ':license',
        ':version',
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
        '//client/lua',
        # Schema
        '//protobuf:krpc.proto',
        '//protobuf:csharp',
        '//protobuf:py',
        '//protobuf:cpp',
        '//protobuf:lua',
        # TODO: add java
        '//protobuf:LICENSE',
        # Docs
        '//doc:latex',
    ],
    path_map = {
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
        '//client/lua:test'
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
        '//tools/ServiceDefinitions',
        '//tools/TestingTools',
        '//tools/TestServer'
    ]
)
