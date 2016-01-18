load('/tools/build/package', 'package_archive')
load('/tools/build/protobuf/cpp', 'protobuf_cpp')
load('/tools/build/client_test', 'client_test')
load('/config', 'version')
load('generate', 'generate')

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

generate(
    name = 'services-krpc',
    service = 'KRPC',
    srcs = ['//server:ServiceDefinitions'],
    out = 'include/krpc/services/krpc.hpp'
)

generate(
    name = 'services-spacecenter',
    service = 'SpaceCenter',
    srcs = ['//service/SpaceCenter:ServiceDefinitions'],
    out = 'include/krpc/services/space_center.hpp'
)

generate(
    name = 'services-kerbalalarmclock',
    service = 'KerbalAlarmClock',
    srcs = ['//service/KerbalAlarmClock:ServiceDefinitions'],
    out = 'include/krpc/services/kerbal_alarm_clock.hpp'
)

generate(
    name = 'services-infernalrobotics',
    service = 'InfernalRobotics',
    srcs = ['//service/InfernalRobotics:ServiceDefinitions'],
    out = 'include/krpc/services/infernal_robotics.hpp'
)

generate(
    name = 'services-test-service',
    service = 'TestService',
    srcs = ['//tools/TestServer:ServiceDefinitions'],
    out = 'include/services/test_service.hpp'
)

cc_library(
    name = 'krpc',
    srcs = glob(['src/*.cpp']) + [':protobuf'],
    hdrs = glob(['include/krpc/*.hpp']) + [':protobuf'],
    includes = ['include'],
    copts = ['-Ibazel-out/local_linux-fastbuild/bin/client/cpp/include'], #TODO: fix include path to generated files
    deps = ['@protobuf//:protobuf', '@boost//:system', '@boost//:thread', '@boost//:asio']
)

client_test(
    name = 'test',
    test_executable = ':cpptest',
    server_executable = '//tools/TestServer',
    rpc_port = '50010',
    stream_port = '50011',
    tags = ['requires-network']
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
    copts = ['-Iexternal/gtest/include', '-Iexternal/gmock/include',
             '-Ibazel-out/local_linux-fastbuild/bin/client/cpp/include', #TODO: fix includes
             '-Ibazel-out/local_linux-fastbuild/bin/client/cpp'],
    deps = [':krpc', '@gtest//:main']
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
