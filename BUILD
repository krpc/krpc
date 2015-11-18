load('/tools/build/package', 'package_archive')

version = '0.1.11'

package_archive(
    name = 'krpc-'+version,
    files = [
        'README.md',
        'VERSION.txt',
        'LICENSE.txt',
        '//lib:protobuf/LICENSE.txt',
        '//lib:protobuf/Google.Protobuf.dll',
        '//server:kRPC',
        '//server:icons',
        '//server:ServiceDefinitions',
        '//service:SpaceCenter',
        '//service:KerbalAlarmClock',
        '//service:InfernalRobotics'
    ],
    path_map = {
        'README.md': 'GameData/kRPC/README.txt',
        'VERSION.txt': 'GameData/kRPC/VERSION.txt',
        'LICENSE.txt': 'GameData/kRPC/LICENSE.txt',
        'server/': 'GameData/kRPC/',
        'lib/protobuf/': 'GameData/kRPC/',
        'lib/protobuf/LICENSE.txt': 'GameData/kRPC/LICENSE.protobuf.txt',
        'server/src/icons': 'GameData/kRPC/icons',
        'service/SpaceCenter/': 'GameData/kRPC/',
        'service/KerbalAlarmClock/': 'GameData/kRPC/',
        'service/InfernalRobotics/': 'GameData/kRPC/'
    }
)
