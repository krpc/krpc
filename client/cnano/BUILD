load('/tools/build/autotools', 'autotools_dist')
load('/tools/build/cpp', 'cpp_check_test', 'cpp_lint_test')
load('/tools/build/protobuf/nanopb', 'protobuf_nanopb')
load('/tools/build/client_test', 'client_test')
load('/tools/krpctools/clientgen', 'clientgen_cnano')
load('/tools/build/pkg', 'pkg_zip')
load('/config', 'version')

name = 'krpc-cnano-%s' % version

autotools_dist(
    name = 'cnano',
    out = '%s.zip' % name,
    source_dir = name,
    files = glob(['include/**/*.h', 'src/*.c', '**/Makefile.am']) + [
        '//:readme', '//:version', 'CHANGES.txt', 'INSTALL.txt',
        'LICENSE', '//:COPYING', '//:COPYING.LESSER',
        ':autotools',
        ':cmake',
        ':protobuf',
        ':services-krpc',
        ':services-spacecenter',
        ':services-drawing',
        ':services-infernalrobotics',
        ':services-kerbalalarmclock',
        ':services-remotetech',
        ':services-ui',
        '@c_nanopb//:LICENSE.txt',
        '@c_nanopb//:pb.h',
        '@c_nanopb//:pb_common.h',
        '@c_nanopb//:pb_common.c',
        '@c_nanopb//:pb_encode.h',
        '@c_nanopb//:pb_encode.c',
        '@c_nanopb//:pb_decode.h',
        '@c_nanopb//:pb_decode.c'
    ],
    path_map = {
        '../c_nanopb/LICENSE.txt': '%s/LICENSE.nanopb' % name,
        '../c_nanopb/*.h': '%s/include/' % name,
        '../c_nanopb/*.c': '%s/src/' % name,
        'client/cnano/': '%s/' % name,
        'COPYING': '%s/COPYING' % name,
        'COPYING.LESSER': '%s/COPYING.LESSER' % name,
        'README.txt': '%s/README.txt' % name,
        'VERSION.txt': '%s/VERSION.txt' % name
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

clientgen_cnano(
    name = 'services-krpc',
    service = 'KRPC',
    defs = '//server:ServiceDefinitions',
    out = 'include/krpc/services/krpc.h'
)

clientgen_cnano(
    name = 'services-spacecenter',
    service = 'SpaceCenter',
    defs = '//service/SpaceCenter:ServiceDefinitions',
    out = 'include/krpc/services/space_center.h'
)

clientgen_cnano(
    name = 'services-drawing',
    service = 'Drawing',
    defs = '//service/Drawing:ServiceDefinitions',
    out = 'include/krpc/services/drawing.h'
)

clientgen_cnano(
    name = 'services-infernalrobotics',
    service = 'InfernalRobotics',
    defs = '//service/InfernalRobotics:ServiceDefinitions',
    out = 'include/krpc/services/infernal_robotics.h'
)

clientgen_cnano(
    name = 'services-kerbalalarmclock',
    service = 'KerbalAlarmClock',
    defs = '//service/KerbalAlarmClock:ServiceDefinitions',
    out = 'include/krpc/services/kerbal_alarm_clock.h'
)

clientgen_cnano(
    name = 'services-remotetech',
    service = 'RemoteTech',
    defs = '//service/RemoteTech:ServiceDefinitions',
    out = 'include/krpc/services/remote_tech.h'
)

clientgen_cnano(
    name = 'services-ui',
    service = 'UI',
    defs = '//service/UI:ServiceDefinitions',
    out = 'include/krpc/services/ui.h'
)

clientgen_cnano(
    name = 'services-test-service',
    service = 'TestService',
    defs = '//tools/TestServer:ServiceDefinitions',
    out = 'test/services/test_service.h'
)

srcs = glob(['src/**/*.c'])
hdrs = glob(['include/**/*.h']) + [
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
    deps = ['@c_nanopb//:nanopb'],
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
    test_executable = ':cnanotest',
    server_executable = '//tools/TestServer',
    server_type = 'serialio',
    tags = ['requires-network', 'local'],
    size = 'small'
)

test_srcs = glob(['test/test_*.cpp', 'test/*.hpp']) + [':services-test-service']

cc_binary(
    name = 'cnanotest',
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
    filters = ['+build/include_alpha', '-legal/copyright', '-readability/todo', '-readability/casting', '-build/header_guard'],
    extra_files = ['include/krpc/services/CPPLINT.cfg', 'test/services/CPPLINT.cfg'],
    size = 'small'
)

#TODO: use targets in //protobuf:... instead of the following
protobuf_nanopb(
    name = 'protobuf',
    header = 'include/krpc/krpc.pb.h',
    source = 'src/krpc.pb.c',
    include = 'krpc/krpc.pb.h',
    src = '//protobuf:krpc.proto',
    options = 'src/krpc.options'
)
exports_files(['src/krpc.options'])
