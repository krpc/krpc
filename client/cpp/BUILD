load('/tools/build/autotools', 'autotools_dist')
load('/tools/build/cpp', 'cpp_check_test', 'cpp_lint_test')
load('/tools/build/protobuf/cpp', 'protobuf_cpp')
load('/tools/build/client_test', 'client_test')
load('/tools/krpctools/clientgen', 'clientgen_cpp')
load('/config', 'version')

name = 'krpc-cpp-%s' % version

autotools_dist(
    name = 'cpp',
    out = '%s.zip' % name,
    source_dir = name,
    files = glob(['include/**/*.hpp', 'src/*.cpp', '**/Makefile.am']) + [
        '//:readme', '//:version', 'CHANGES.txt', 'INSTALL.txt',
        'LICENSE', '//:COPYING', '//:COPYING.LESSER',
        ':autotools', '@m4_stdcxx//file', '@m4_stdcxx_11//file',
        ':cmake',
        '//protobuf:cpp',
        ':services-krpc',
        ':services-spacecenter',
        ':services-drawing',
        ':services-infernalrobotics',
        ':services-kerbalalarmclock',
        ':services-remotetech',
        ':services-ui'
    ],
    path_map = {
        'client/cpp/': '%s/' % name,
        'COPYING': '%s/COPYING' % name,
        'COPYING.LESSER': '%s/COPYING.LESSER' % name,
        'README.txt': '%s/README.txt' % name,
        'VERSION.txt': '%s/VERSION.txt' % name,
        'client/cpp/': '%s/' % name,
        'protobuf/krpc.pb.hpp': '%s/include/krpc/krpc.pb.hpp' % name,
        'protobuf/krpc.pb.cpp': '%s/src/krpc.pb.cpp' % name,
        '../m4_stdcxx/file/gitweb': '%s/m4/ax_cxx_compile_stdcxx.m4' % name,
        '../m4_stdcxx_11/file/gitweb': '%s/m4/ax_cxx_compile_stdcxx_11.m4' % name
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
    name = 'services-drawing',
    service = 'Drawing',
    defs = '//service/Drawing:ServiceDefinitions',
    out = 'include/krpc/services/drawing.hpp'
)

clientgen_cpp(
    name = 'services-infernalrobotics',
    service = 'InfernalRobotics',
    defs = '//service/InfernalRobotics:ServiceDefinitions',
    out = 'include/krpc/services/infernal_robotics.hpp'
)

clientgen_cpp(
    name = 'services-kerbalalarmclock',
    service = 'KerbalAlarmClock',
    defs = '//service/KerbalAlarmClock:ServiceDefinitions',
    out = 'include/krpc/services/kerbal_alarm_clock.hpp'
)

clientgen_cpp(
    name = 'services-remotetech',
    service = 'RemoteTech',
    defs = '//service/RemoteTech:ServiceDefinitions',
    out = 'include/krpc/services/remote_tech.hpp'
)

clientgen_cpp(
    name = 'services-ui',
    service = 'UI',
    defs = '//service/UI:ServiceDefinitions',
    out = 'include/krpc/services/ui.hpp'
)

clientgen_cpp(
    name = 'services-test-service',
    service = 'TestService',
    defs = '//tools/TestServer:ServiceDefinitions',
    out = 'test/services/test_service.hpp'
)

srcs = glob(['src/*.cpp'])
hdrs = glob(['include/**/*.hpp']) + [
    ':services-krpc',
    ':services-spacecenter',
    ':services-drawing',
    ':services-infernalrobotics',
    ':services-kerbalalarmclock',
    ':services-remotetech',
    ':services-ui'
]

cc_library(
    name = 'krpc',
    srcs = srcs + [':protobuf'],
    hdrs = hdrs + [':protobuf'],
    includes = ['include'],
    deps = ['@cpp_protobuf//:protobuf', '@cpp_asio//:asio'],
    visibility = ['//visibility:public']
)

test_suite(
    name = 'test',
    tests = [':client', ':check', ':lint']
)

test_suite(
    name = 'ci-test',
    tests = [':client']
)

client_test(
    name = 'client',
    test_executable = ':cpptest',
    server_executable = '//tools/TestServer',
    tags = ['requires-network'],
    size = 'small'
)

test_srcs = glob(['test/test_*.cpp']) + [
    ':services-test-service',
    'test/server_test.hpp'
]

cc_binary(
    name = 'cpptest',
    srcs = test_srcs,
    includes = ['test'],
    deps = [':krpc', '@cpp_googletest//:gtest', '@cpp_googletest//:gmock']
)

cpp_check_test(
    name = 'check',
    srcs = srcs + test_srcs,
    hdrs = hdrs,
    includes = ['include'],
    size = 'small'
)

cpp_lint_test(
    name = 'lint',
    srcs = srcs + test_srcs,
    hdrs = hdrs,
    includes = ['include'],
    filters = ['+build/include_alpha', '-legal/copyright', '-readability/todo', '-runtime/references'], #FIXME: dont pass non-const references
    extra_files = ['include/krpc/services/CPPLINT.cfg', 'test/services/CPPLINT.cfg'],
    size = 'small'
)

#TODO: use targets in //protobuf:... instead of the following
protobuf_cpp(
    name = 'protobuf',
    header = 'include/krpc/krpc.pb.hpp',
    source = 'src/krpc.pb.cpp',
    include = 'krpc/krpc.pb.hpp',
    src = '//protobuf:krpc.proto'
)
