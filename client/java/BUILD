load('/tools/krpctools/clientgen', 'clientgen_java')
load('/tools/build/client_test', 'client_test')
load('/config', 'version')

copts = ['-Werror', '-Xlint:all', '-target 1.7', '-source 1.7']

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

java_import(
    name = 'protobuf',
    jars = ['@java_protobuf//file'],
)

java_import(
    name = 'javatuples',
    jars = ['@java_javatuples//file']
)

java_import(
    name = 'junit',
    jars = ['@java_junit//file', '@java_hamcrest//file']
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
        ':protobuf',
        ':javatuples'
    ],
    javacopts = copts,
    visibility = ['//visibility:public']
)

java_library(
    name = 'testlib',
    srcs = glob(['test/**/*.java']) + [':services-testservice'],
    deps = [
        ':krpc',
        ':protobuf',
        ':junit',
        ':javatuples'
    ],
    javacopts = copts
)

java_test(
    name = 'testexe',
    main_class = 'krpc.client.TestSuite',
    runtime_deps = [
        ':krpc',
        ':testlib',
        ':protobuf',
        ':junit',
        ':javatuples'
    ],
    javacopts = copts,
    size = 'small',
    tags = ['requires-network']
)

test_suite(
    name = 'test',
    tests = [':client']
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
