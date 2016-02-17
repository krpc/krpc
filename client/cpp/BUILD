load('/tools/build/autotools', 'autotools_dist')
load('/tools/build/protobuf/cpp', 'protobuf_cpp')
load('/tools/build/client_test', 'client_test')
load('/tools/clientgen/build', 'clientgen_cpp')
load('/config', 'version')

autotools_dist(
    name = 'cpp',
    out = 'krpc-cpp-%s.zip' % version,
    files = glob(['include/krpc/*.hpp', 'src/*.cpp', '**/Makefile.am']) + [
        '//:readme', '//:version', 'CHANGES.txt', 'INSTALL.txt',
        'LICENSE', '//:COPYING', '//:COPYING.LESSER',
        ':autotools', '@m4_stdcxx//file', '@m4_stdcxx_11//file',
        ':cmake',
        '//protobuf:cpp',
        ':services-krpc',
        ':services-spacecenter',
        ':services-kerbalalarmclock',
        ':services-infernalrobotics'
    ],
    path_map = {
        'client/cpp/': '',
        'protobuf/krpc.pb.hpp': 'include/krpc/krpc.pb.hpp',
        'protobuf/krpc.pb.cpp': 'src/krpc.pb.cpp',
        'external/m4_stdcxx/file/gitweb': 'm4/ax_cxx_compile_stdcxx.m4',
        'external/m4_stdcxx_11/file/gitweb': 'm4/ax_cxx_compile_stdcxx_11.m4'
    },
    visibility = ['//:__pkg__']
)

genrule(
    name = 'autotools',
    srcs = ['configure.ac.tmpl'],
    outs = ['configure.ac'],
    cmd = 'sed \'s/%VERSION%/'+version+'/g\' "$<" > "$@"'
)

genrule(
    name = 'cmake',
    srcs = ['CMakeLists.txt.tmpl'],
    outs = ['CMakeLists.txt'],
    cmd = 'sed \'s/%VERSION%/'+version+'/g\' "$<" > "$@"'
)

clientgen_cpp(
    name = 'services-krpc',
    service = 'KRPC',
    defs = '//server:ServiceDefinitions',
    out = 'include/krpc/services/krpc.hpp'
)

clientgen_cpp(
    name = 'services-spacecenter',
    service = 'SpaceCenter',
    defs = '//service/SpaceCenter:ServiceDefinitions',
    out = 'include/krpc/services/space_center.hpp'
)

clientgen_cpp(
    name = 'services-kerbalalarmclock',
    service = 'KerbalAlarmClock',
    defs = '//service/KerbalAlarmClock:ServiceDefinitions',
    out = 'include/krpc/services/kerbal_alarm_clock.hpp'
)

clientgen_cpp(
    name = 'services-infernalrobotics',
    service = 'InfernalRobotics',
    defs = '//service/InfernalRobotics:ServiceDefinitions',
    out = 'include/krpc/services/infernal_robotics.hpp'
)

clientgen_cpp(
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
    deps = ['@protobuf//:protobuf', '@cpp_asio//:asio']
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
        ':services-krpc',
        ':services-spacecenter',
        ':services-kerbalalarmclock',
        ':services-infernalrobotics',
        ':services-test-service',
        'test/server_test.hpp'
    ],
    includes = ['test'],
    deps = [':krpc', '@cpp_gmock//:main']
)

#TODO: use targets in //protobuf:... instead of the following
protobuf_cpp(
    name = 'protobuf',
    header = 'include/krpc/krpc.pb.hpp',
    source = 'src/krpc.pb.cpp',
    include = 'krpc/krpc.pb.hpp',
    src = '//protobuf:krpc.proto'
)
