load('//tools/build:python.bzl', 'py_sdist', 'py_test', 'py_lint_test')
load('//tools/build:client_test.bzl', 'client_test')
load('//tools/krpctools:clientgen.bzl', 'clientgen_python')
load('//:config.bzl', 'version', 'python_version')

# Needed as clientgen depends on these files
genrule(
    name = 'python_base',
    srcs = [':python-without-stubs'],
    outs = ['krpc-python-base-%s.zip' % version],
    cmd = 'cp "$<" "$@"',
    output_to_bindir = True,
    visibility = ['//visibility:public']
)

py_sdist(
    name = 'python-without-stubs',
    out = 'krpc-base-%s.zip' % version,
    files = [
        '//:readme', '//:version', 'CHANGES.txt',
        'LICENSE', '//:COPYING', '//:COPYING.LESSER',
        'setup.py', 'MANIFEST.in', '//:python_version',
        '//protobuf:python'
    ] + glob(['krpc/**/*']),
    path_map = {
        'version.py': 'krpc/version.py',
        'client/python/': '',
        'protobuf/': 'krpc/schema/'
    }
)

genrule(
    name = 'python',
    srcs = [':python-pypi'],
    outs = ['krpc-python-%s.zip' % version],
    cmd = 'cp "$<" "$@"',
    output_to_bindir = True,
    visibility = ['//visibility:public']
)

py_sdist(
    name = 'python-pypi',
    out = 'krpc-%s.zip' % version,
    files = [
        '//:readme', '//:version', 'CHANGES.txt',
        'LICENSE', '//:COPYING', '//:COPYING.LESSER',
        'setup.py', 'MANIFEST.in', '//:python_version',
        '//protobuf:python'
    ] + glob(['krpc/**/*']),
    path_map = {
        'version.py': 'krpc/version.py',
        'client/python/': '',
        'protobuf/': 'krpc/schema/'
    },
    stub_files = [
        ':services-krpc-stub',
        ':services-spacecenter-stub',
        ':services-drawing-stub',
        ':services-infernalrobotics-stub',
        ':services-kerbalalarmclock-stub',
        ':services-remotetech-stub',
        ':services-ui-stub',
        ':services-lidar-stub',
        ':services-dockingcamera-stub',
    ]
)

clientgen_python(
    name = 'services-krpc-stub',
    service = 'KRPC',
    defs = '//server:ServiceDefinitions',
    out = 'krpc/krpc.pyi'
)

clientgen_python(
    name = 'services-spacecenter-stub',
    service = 'SpaceCenter',
    defs = '//service/SpaceCenter:ServiceDefinitions',
    out = 'krpc/spacecenter.pyi'
)

clientgen_python(
    name = 'services-drawing-stub',
    service = 'Drawing',
    defs = '//service/Drawing:ServiceDefinitions',
    out = 'krpc/drawing.pyi'
)

clientgen_python(
    name = 'services-infernalrobotics-stub',
    service = 'InfernalRobotics',
    defs = '//service/InfernalRobotics:ServiceDefinitions',
    out = 'krpc/infernalrobotics.pyi'
)

clientgen_python(
    name = 'services-kerbalalarmclock-stub',
    service = 'KerbalAlarmClock',
    defs = '//service/KerbalAlarmClock:ServiceDefinitions',
    out = 'krpc/kerbalalarmclock.pyi'
)

clientgen_python(
    name = 'services-remotetech-stub',
    service = 'RemoteTech',
    defs = '//service/RemoteTech:ServiceDefinitions',
    out = 'krpc/remotetech.pyi'
)

clientgen_python(
    name = 'services-ui-stub',
    service = 'UI',
    defs = '//service/UI:ServiceDefinitions',
    out = 'krpc/ui.pyi'
)

clientgen_python(
    name = 'services-lidar-stub',
    service = 'LiDAR',
    defs = '//service/LiDAR:ServiceDefinitions',
    out = 'krpc/lidar.pyi'
)

clientgen_python(
    name = 'services-dockingcamera-stub',
    service = 'DockingCamera',
    defs = '//service/DockingCamera:ServiceDefinitions',
    out = 'krpc/dockingcamera.pyi'
)

clientgen_python(
    name = 'services-test-service-stub',
    service = 'TestService',
    defs = '//tools/TestServer:ServiceDefinitions',
    out = 'test/testservice.pyi'
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
    test_executable = ':clienttest',
    server_executable = '//tools/TestServer',
    tags = ['requires-network'],
    size = 'medium'
)

deps = ['@python_protobuf//file']

py_test(
    name = 'clienttest',
    src = ':python',
    pkg = 'krpc-'+python_version,
    deps = deps,
    tags = ['requires-network'],
    size = 'small'
)

py_lint_test(
    name = 'lint',
    pkg = ':python',
    pkg_name = 'krpc',
    srcs = glob(['krpc/**/*']),
    deps = deps,
    pycodestyle_config = 'pycodestyle.ini',
    pylint_config = 'pylint.rc',
    size = 'small'
)
