load('/tools/krpctools/clientgen', 'clientgen_java')
load('/tools/build/client_test', 'client_test')
load('/config', 'version')

copts = ['-Werror', '-Xlint:all']

filegroup(
    name = 'java',
    srcs = [':krpc-jar'],
    visibility = ['//visibility:public']
)

genrule(
    name = 'krpc-jar',
    srcs = [':krpc'],
    outs = ['krpc-java-%s.jar' % version],
    cmd = 'ln -f -r -s "$<" "$@"',
    output_to_bindir = True
)

java_library(
    name = 'krpc',
    srcs = glob(['src/krpc/**/*.java']) + [
        '//protobuf:java',
        ':services-krpc',
        ':services-spacecenter',
        ':services-kerbalalarmclock',
        ':services-infernalrobotics'
    ],
    resources = [
        '//:readme', '//:version', 'CHANGES.txt',
        'LICENSE', '//:COPYING', '//:COPYING.LESSER',
    ],
    deps = [
        '//tools/build/protobuf:protobuf',
        '@java_javatuples//:jar'
    ],
    javacopts = copts
)

java_import(
    name = 'junit',
    jars = ['@java_junit//file', '@java_hamcrest//file']
)

java_library(
    name = 'testlib',
    srcs = glob(['test/**/*.java']) + [':services-testservice'],
    deps = [
        ':krpc',
        '//tools/build/protobuf:protobuf',
        ':junit',
        '@java_javatuples//:jar'],
    javacopts = copts
)

java_test(
    name = 'testexe',
    main_class = 'krpc.client.TestSuite',
    runtime_deps = [
        ':krpc',
        ':testlib',
        '//tools/build/protobuf:protobuf',
        ':junit',
        '@java_javatuples//:jar'
    ],
    size = 'small',
    tags = ['requires-network']
)

client_test(
    name = 'test',
    test_executable = ':testexe',
    server_executable = '//tools/TestServer',
    tags = ['requires-network'],
    size = 'small',
    visibility = ['//:__pkg__']
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
    name = 'services-kerbalalarmclock',
    service = 'KerbalAlarmClock',
    defs = '//service/KerbalAlarmClock:ServiceDefinitions',
    out = 'services/KerbalAlarmClock.java'
)

clientgen_java(
    name = 'services-infernalrobotics',
    service = 'InfernalRobotics',
    defs = '//service/InfernalRobotics:ServiceDefinitions',
    out = 'services/InfernalRobotics.java'
)

clientgen_java(
    name = 'services-testservice',
    service = 'TestService',
    defs = '//tools/TestServer:ServiceDefinitions',
    out = 'services/TestService.java'
)
