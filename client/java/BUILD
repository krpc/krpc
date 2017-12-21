load('//tools/krpctools:clientgen.bzl', 'clientgen_java')
load('//tools/build:client_test.bzl', 'client_test')
load('//tools/build:java.bzl', 'java_checkstyle_test')
load('//:config.bzl', 'version')

copts = ['-Werror', '-Xlint:all', '-target 1.8', '-source 1.8']

filegroup(
    name = 'java',
    srcs = [':krpc-jar'],
    visibility = ['//visibility:public']
)

genrule(
    name = 'krpc-jar',
    srcs = [':krpc'],
    outs = ['krpc-java-%s.jar' % version],
    cmd = 'cp "$<" "$@"',
    output_to_bindir = True,
    visibility = ['//visibility:public']
)

java_library(
    name = 'krpc',
    srcs = glob(['src/krpc/**/*.java']) + [
        '//protobuf:java',
        ':services-krpc',
        ':services-spacecenter',
        ':services-drawing',
        ':services-infernalrobotics',
        ':services-kerbalalarmclock',
        ':services-remotetech',
        ':services-ui'
    ],
    resources = [
        '//:readme', '//:version', 'CHANGES.txt',
        'LICENSE', '//:COPYING', '//:COPYING.LESSER',
    ],
    deps = [
        '@java_protobuf//jar',
        '@java_javatuples//jar'
    ],
    javacopts = copts,
    visibility = ['//visibility:public']
)

java_library(
    name = 'testlib',
    srcs = glob(['test/**/*.java']) + [':services-testservice'],
    deps = [
        ':krpc',
        '@java_protobuf//jar',
        '@java_junit//jar',
        '@java_hamcrest//jar',
        '@java_javatuples//jar'
    ],
    javacopts = copts
)

java_test(
    name = 'testexe',
    main_class = 'krpc.client.TestSuite',
    runtime_deps = [
        ':krpc',
        ':testlib',
        '@java_protobuf//jar',
        '@java_junit//jar',
        '@java_hamcrest//jar',
        '@java_javatuples//jar'
    ],
    javacopts = copts,
    size = 'medium',
    tags = ['requires-network']
)

test_suite(
    name = 'test',
    tests = [':client', ':lint']
)

test_suite(
    name = 'ci-test',
    tests = [':client']
)

client_test(
    name = 'client',
    test_executable = ':testexe',
    server_executable = '//tools/TestServer',
    tags = ['requires-network'],
    size = 'medium',
    visibility = ['//:__pkg__']
)

java_checkstyle_test(
    name = 'lint',
    srcs = glob(['**/*.java']),
    size = 'small'
)

clientgen_java(
    name = 'services-krpc',
    service = 'KRPC',
    defs = '//server:ServiceDefinitions',
    out = 'services/KRPC.java'
)

clientgen_java(
    name = 'services-spacecenter',
    service = 'SpaceCenter',
    defs = '//service/SpaceCenter:ServiceDefinitions',
    out = 'services/SpaceCenter.java'
)

clientgen_java(
    name = 'services-drawing',
    service = 'Drawing',
    defs = '//service/Drawing:ServiceDefinitions',
    out = 'services/Drawing.java'
)

clientgen_java(
    name = 'services-infernalrobotics',
    service = 'InfernalRobotics',
    defs = '//service/InfernalRobotics:ServiceDefinitions',
    out = 'services/InfernalRobotics.java'
)

clientgen_java(
    name = 'services-kerbalalarmclock',
    service = 'KerbalAlarmClock',
    defs = '//service/KerbalAlarmClock:ServiceDefinitions',
    out = 'services/KerbalAlarmClock.java'
)

clientgen_java(
    name = 'services-remotetech',
    service = 'RemoteTech',
    defs = '//service/RemoteTech:ServiceDefinitions',
    out = 'services/RemoteTech.java'
)

clientgen_java(
    name = 'services-ui',
    service = 'UI',
    defs = '//service/UI:ServiceDefinitions',
    out = 'services/UI.java'
)

clientgen_java(
    name = 'services-testservice',
    service = 'TestService',
    defs = '//tools/TestServer:ServiceDefinitions',
    out = 'services/TestService.java'
)
