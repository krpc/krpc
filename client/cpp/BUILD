load('/tools/build/package', 'package_archive')
load('/tools/build/protobuf/cpp', 'protobuf_cpp')
load('/tools/build/client_test', 'client_test')
load('/tools/krpcgen/build', 'krpcgen_cpp')
load('/config', 'version')

package_archive(
    name = 'cpp',
    out = 'krpc-cpp-%s.zip' % version,
    files = glob(['include/krpc/*.hpp', 'src/*.cpp']) + [
        '//:readme', '//:version', 'CHANGES.txt',
        'LICENSE', '//:COPYING', '//:COPYING.LESSER',
        '//protobuf:cpp',
        ':services-krpc',
        ':services-spacecenter',
        ':services-kerbalalarmclock',
        ':services-infernalrobotics'
    ],
    path_map = {
        'client/cpp': '',
        'protobuf/krpc.pb.hpp': 'include/krpc/krpc.pb.hpp',
        'protobuf/krpc.pb.cpp': 'src/krpc.pb.cpp'
    },
    visibility = ['//:__pkg__']
)

krpcgen_cpp(
    name = 'services-krpc',
    service = 'KRPC',
    defs = '//server:ServiceDefinitions',
    out = 'include/krpc/services/krpc.hpp'
)

krpcgen_cpp(
    name = 'services-spacecenter',
    service = 'SpaceCenter',
    defs = '//service/SpaceCenter:ServiceDefinitions',
    out = 'include/krpc/services/space_center.hpp'
)

krpcgen_cpp(
    name = 'services-kerbalalarmclock',
    service = 'KerbalAlarmClock',
    defs = '//service/KerbalAlarmClock:ServiceDefinitions',
    out = 'include/krpc/services/kerbal_alarm_clock.hpp'
)

krpcgen_cpp(
    name = 'services-infernalrobotics',
    service = 'InfernalRobotics',
    defs = '//service/InfernalRobotics:ServiceDefinitions',
    out = 'include/krpc/services/infernal_robotics.hpp'
)

krpcgen_cpp(
    name = 'services-test-service',
    service = 'TestService',
    defs = '//tools/TestServer:ServiceDefinitions',
    out = 'include/services/test_service.hpp'
)

cc_library(
    name = 'krpc',
    srcs = glob(['src/*.cpp']) + [':protobuf'],
    hdrs = glob(['include/krpc/*.hpp']) + [':protobuf'],
    includes = ['include'],
    copts = ['-Iexternal/asio/include',
             '-Ibazel-out/local_linux-fastbuild/bin/client/cpp/include'], #TODO: fix include path to generated files
    deps = ['@protobuf//:protobuf', '@asio//:asio']
)

client_test(
    name = 'test',
    test_executable = ':cpptest',
    server_executable = '//tools/TestServer',
    tags = ['requires-network'],
    size = 'small'
)

cc_binary(
    name = 'cpptest',
    srcs = glob(['test/test_*.cpp']) + [
        ':protobuf-test',
        ':services-krpc',
        ':services-spacecenter',
        ':services-kerbalalarmclock',
        ':services-infernalrobotics',
        ':services-test-service',
        'test/server_test.hpp'
    ],
    includes = ['test'],
    copts = ['-Iexternal/asio/include', '-Iexternal/gtest/include' ,'-Iexternal/gmock/include',
             '-Ibazel-out/local_linux-fastbuild/bin/client/cpp/include', #TODO: fix includes
             '-Ibazel-out/local_linux-fastbuild/bin/client/cpp'],
    deps = [':krpc', '@gmock//:main']
)

#TODO: use targets in //protobuf:... instead of the following
protobuf_cpp(
    name = 'protobuf',
    header = 'include/krpc/krpc.pb.hpp',
    source = 'src/krpc.pb.cpp',
    include = 'krpc/krpc.pb.hpp',
    src = '//protobuf:krpc.proto'
)

protobuf_cpp(
    name = 'protobuf-test',
    header = 'Test.pb.hpp',
    source = 'Test.pb.cpp',
    include = 'Test.pb.hpp',
    src = '//protobuf:test.proto'
)
