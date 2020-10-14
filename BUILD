load('//tools/build:pkg.bzl', 'pkg_zip')
load('//:config.bzl', 'version', 'python_version', 'avc_version', 'ksp_avc_version_max', 'ksp_avc_version_min')

exports_files(['COPYING', 'COPYING.LESSER'])

readme_text = """Documentation: https://krpc.github.io/krpc

Forum release thread: http://forum.kerbalspaceprogram.com/index.php?/topic/130742-105-krpc-remote-control-your-ships-using-python-c-c-lua-v021-10th-feb-2016/

Forum development thread: http://forum.kerbalspaceprogram.com/threads/69313

"""

license_text = """This license (LGPL v3) applies to all parts of kRPC except for the following:

  - GameData/kRPC/KRPC.SpaceCenter.* is under the GPLv3 license.
    See LICENSE.KRPC.SpaceCenter

  - GameData/kRPC/Google.Protobuf.dll is a binary from Google's protobuf project.
    See LICENSE.Google.Protobuf

  - GameData/kRPC/KRPC.IO.Ports.dll is a modified binary from the Mono project.
    See LICENSE.KRPC.IO.Ports

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
    outs = ['README.txt'],
    cmd = 'echo "%s" > "$@"' % readme_text,
    visibility = ['//visibility:public']
)

genrule(
    name = 'blank_settings',
    outs = ['GameData/kRPC/PluginData/settings.cfg'],
    cmd = 'echo "" > "$@"'
)

genrule(
    name = 'version',
    outs = ['VERSION.txt'],
    cmd = 'echo "%s" > "$@"' % version,
    visibility = ['//visibility:public']
)

genrule(
    name = 'python_version',
    outs = ['version.py'],
    cmd = 'echo "__version__ = \'%s\'" > "$@"' % python_version,
    visibility = ['//visibility:public']
)

ksp_avc_version = """{
  "NAME": "kRPC",
  "URL": "http://ksp-avc.cybutek.net/version.php?id=254",
  "DOWNLOAD": "https://github.com/krpc/krpc/releases/latest",
  "VERSION": { %s },
  "KSP_VERSION": { %s },
  "KSP_VERSION_MAX": { %s },
  "KSP_VERSION_MIN": { %s }
}""" % (avc_version, ksp_avc_version_max, ksp_avc_version_max, ksp_avc_version_min)

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
        ':blank_settings',
        '//tools/build/ksp:Google.Protobuf',
        '//tools/build/protobuf:LICENSE',
        '//tools/build/ksp:KRPC.IO.Ports',
        '@csharp_krpc_io_ports_license//file',
        # Services
        '//service/SpaceCenter',
        '//service/Drawing',
        '//service/InfernalRobotics',
        '//service/KerbalAlarmClock',
        '//service/RemoteTech',
        '//service/UI',
        # Clients
        '//client/cnano',
        '//client/cpp',
        '//client/csharp',
        '//client/java',
        '//client/lua',
        '//client/python',
        # Schema
        '//protobuf:krpc.proto',
        '//protobuf:cnano',
        '//protobuf:cpp',
        '//protobuf:csharp',
        '//protobuf:java',
        '//protobuf:lua',
        '//protobuf:python',
        '//protobuf:LICENSE',
        # Docs
        '//doc:pdf'
    ],
    path_map = {
        'kRPC.version': 'GameData/kRPC/kRPC.version',
        # Server
        'server/': 'GameData/kRPC/',
        'server/src/icons': 'GameData/kRPC/icons',
        'tools/build/ksp/': 'GameData/kRPC/',
        'tools/build/protobuf/LICENSE': 'LICENSE.Google.Protobuf',
        '../csharp_krpc_io_ports_license/file/LICENSE': 'LICENSE.KRPC.IO.Ports',
        # Services
        'service/SpaceCenter/': 'GameData/kRPC/',
        'service/SpaceCenter/CHANGES.txt': 'GameData/kRPC/CHANGES.SpaceCenter.txt',
        'service/SpaceCenter/LICENSE': 'LICENSE.KRPC.SpaceCenter',
        'service/SpaceCenter/src/module-manager.cfg': 'GameData/kRPC/module-manager.cfg',
        'service/Drawing/': 'GameData/kRPC/',
        'service/Drawing/CHANGES.txt': 'GameData/kRPC/CHANGES.Drawing.txt',
        'service/InfernalRobotics/': 'GameData/kRPC/',
        'service/InfernalRobotics/CHANGES.txt': 'GameData/kRPC/CHANGES.InfernalRobotics.txt',
        'service/KerbalAlarmClock/': 'GameData/kRPC/',
        'service/KerbalAlarmClock/CHANGES.txt': 'GameData/kRPC/CHANGES.KerbalAlarmClock.txt',
        'service/RemoteTech/': 'GameData/kRPC/',
        'service/RemoteTech/CHANGES.txt': 'GameData/kRPC/CHANGES.RemoteTech.txt',
        'service/UI/': 'GameData/kRPC/',
        'service/UI/CHANGES.txt': 'GameData/kRPC/CHANGES.UI.txt',
        # Module Manager
        '../module_manager/file/ModuleManager.4.1.3.dll': 'GameData/ModuleManager.4.1.3.dll',
        # Clients
        'client/cnano/': 'client/',
        'client/cpp/': 'client/',
        'client/csharp/': 'client/',
        'client/java/': 'client/',
        'client/lua/': 'client/',
        'client/python/': 'client/',
        # Schema
        'protobuf/': 'schema/',
        # Docs
        'doc/kRPC.pdf': 'kRPC.pdf'
    },
    exclude = ['*.mdb']
)

pkg_zip(
    name = 'krpc-curse',
    out = 'krpc-%s-curse.zip' % version,
    files = [
        ':readme',
        ':license',
        ':version',
        ':ksp-avc-version',
        'COPYING',
        'COPYING.LESSER',
        # Server
        '//server',
        ':blank_settings',
        '//tools/build/ksp:Google.Protobuf',
        '//tools/build/protobuf:LICENSE',
        '//tools/build/ksp:KRPC.IO.Ports',
        '@csharp_krpc_io_ports_license//file',
        # Services
        '//service/SpaceCenter',
        '//service/Drawing',
        '//service/InfernalRobotics',
        '//service/KerbalAlarmClock',
        '//service/RemoteTech',
        '//service/UI'
    ],
    path_map = {
        'kRPC.version': 'GameData/kRPC/kRPC.version',
        'COPYING': 'GameData/kRPC/COPYING',
        'COPYING.LESSER': 'GameData/kRPC/COPYING.LESSER',
        'LICENSE': 'GameData/kRPC/LICENSE',
        'README.txt': 'GameData/kRPC/README.txt',
        'VERSION.txt': 'GameData/kRPC/VERSION.txt',
        # Server
        'server/': 'GameData/kRPC/',
        'server/src/icons': 'GameData/kRPC/icons',
        'tools/build/ksp/': 'GameData/kRPC/',
        'tools/build/protobuf/LICENSE': 'GameData/kRPC/LICENSE.Google.Protobuf',
        '../csharp_krpc_io_ports_license/file/LICENSE': 'GameData/kRPC/LICENSE.KRPC.IO.Ports',
        # Services
        'service/SpaceCenter/': 'GameData/kRPC/',
        'service/SpaceCenter/CHANGES.txt': 'GameData/kRPC/CHANGES.SpaceCenter.txt',
        'service/SpaceCenter/LICENSE': 'GameData/kRPC/LICENSE.KRPC.SpaceCenter',
        'service/Drawing/': 'GameData/kRPC/',
        'service/Drawing/CHANGES.txt': 'GameData/kRPC/CHANGES.Drawing.txt',
        'service/InfernalRobotics/': 'GameData/kRPC/',
        'service/InfernalRobotics/CHANGES.txt': 'GameData/kRPC/CHANGES.InfernalRobotics.txt',
        'service/KerbalAlarmClock/': 'GameData/kRPC/',
        'service/KerbalAlarmClock/CHANGES.txt': 'GameData/kRPC/CHANGES.KerbalAlarmClock.txt',
        'service/RemoteTech/': 'GameData/kRPC/',
        'service/RemoteTech/CHANGES.txt': 'GameData/kRPC/CHANGES.RemoteTech.txt',
        'service/UI/': 'GameData/kRPC/',
        'service/UI/CHANGES.txt': 'GameData/kRPC/CHANGES.UI.txt'
    },
    exclude = ['*.mdb']
)

test_suite(
    name = 'test',
    tests = [
        '//server:test',
        '//service/SpaceCenter:test',
        '//service/Drawing:test',
        '//service/InfernalRobotics:test',
        '//service/KerbalAlarmClock:test',
        '//service/RemoteTech:test',
        '//service/UI:test',
        '//client/cnano:test',
        '//client/cpp:test',
        '//client/csharp:test',
        '//client/java:test',
        '//client/lua:test',
        '//client/python:test',
        '//client/websockets:test',
        '//client/serialio:test',
        '//tools/krpctest:test',
        '//tools/krpctools:test',
        '//tools/ServiceDefinitions:test',
        '//tools/TestingTools:test',
        '//tools/TestServer:test',
        '//doc:test'
    ]
)

test_suite(
    name = 'ci-test',
    tests = [
        '//server:ci-test',
        '//client/csharp:ci-test',
        '//client/cnano:ci-test',
        '//client/cpp:ci-test',
        '//client/java:ci-test',
        '//client/lua:ci-test',
        '//client/python:ci-test',
        '//client/serialio:ci-test',
        '//client/websockets:ci-test',
        '//tools/krpctest:ci-test',
        '//tools/krpctools:ci-test',
        '//doc:ci-test'
    ]
)

test_suite(
    name = 'lint',
    tests = [
        '//server:lint',
        '//service/SpaceCenter:lint',
        '//service/Drawing:lint',
        '//service/InfernalRobotics:lint',
        '//service/KerbalAlarmClock:lint',
        '//service/RemoteTech:lint',
        '//service/UI:lint',
        '//client/cnano:lint',
        '//client/cpp:lint',
        '//client/csharp:lint',
        '//client/java:lint',
        '//client/python:lint',
        '//client/websockets:lint',
        '//tools/krpctest:lint',
        '//tools/krpctools:lint',
        '//doc:lint'
    ]
)

filegroup(
    name = 'csproj',
    srcs = [
        '//tools/cslibs',
        '//server',
        '//server:KRPC.Test',
        '//service/SpaceCenter',
        '//service/Drawing',
        '//service/InfernalRobotics',
        '//service/KerbalAlarmClock',
        '//service/RemoteTech',
        '//service/UI',
        '//client/csharp',
        '//client/csharp:KRPC.Client.Test',
        '//tools/ServiceDefinitions',
        '//tools/TestingTools',
        '//tools/TestServer'
    ]
)
