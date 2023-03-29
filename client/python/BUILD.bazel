load('//tools/build:python.bzl', 'py_sdist', 'py_test', 'py_lint_test')
load('//tools/build:client_test.bzl', 'client_test')
load('//tools/krpctools:clientgen.bzl', 'clientgen_python')
load('//:config.bzl', 'version', 'python_version')

# Needed as clientgen depends on these files
genrule(
    name = 'python_base',
    srcs = [':python-without-services'],
    outs = ['krpc-python-base-%s.zip' % version],
    cmd = 'cp "$<" "$@"',
    output_to_bindir = True,
    visibility = ['//visibility:public']
)

py_sdist(
    name = 'python-without-services',
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
        '//protobuf:python',
        ':services-krpc',
        ':services-testservice',
        ':services-spacecenter',
        ':services-drawing',
        ':services-infernalrobotics',
        ':services-kerbalalarmclock',
        ':services-remotetech',
        ':services-ui',
        ':services-lidar',
        ':services-dockingcamera'
    ] + glob(['krpc/**/*']),
    path_map = {
        'version.py': 'krpc/version.py',
        'client/python/': '',
        'protobuf/': 'krpc/schema/'
    }
)

clientgen_python(
    name = 'services-krpc',
    service = 'KRPC',
    defs = '//core:ServiceDefinitions',
    out = 'krpc/services/krpc.py'
)

clientgen_python(
    name = 'services-testservice',
    service = 'TestService',
    defs = '//tools/TestServer:ServiceDefinitions',
    out = 'krpc/services/testservice.py'
)

clientgen_python(
    name = 'services-spacecenter',
    service = 'SpaceCenter',
    defs = '//service/SpaceCenter:ServiceDefinitions',
    out = 'krpc/services/spacecenter.py'
)

clientgen_python(
    name = 'services-drawing',
    service = 'Drawing',
    defs = '//service/Drawing:ServiceDefinitions',
    out = 'krpc/services/drawing.py'
)

clientgen_python(
    name = 'services-infernalrobotics',
    service = 'InfernalRobotics',
    defs = '//service/InfernalRobotics:ServiceDefinitions',
    out = 'krpc/services/infernalrobotics.py'
)

clientgen_python(
    name = 'services-kerbalalarmclock',
    service = 'KerbalAlarmClock',
    defs = '//service/KerbalAlarmClock:ServiceDefinitions',
    out = 'krpc/services/kerbalalarmclock.py'
)

clientgen_python(
    name = 'services-remotetech',
    service = 'RemoteTech',
    defs = '//service/RemoteTech:ServiceDefinitions',
    out = 'krpc/services/remotetech.py'
)

clientgen_python(
    name = 'services-ui',
    service = 'UI',
    defs = '//service/UI:ServiceDefinitions',
    out = 'krpc/services/ui.py'
)

clientgen_python(
    name = 'services-lidar',
    service = 'LiDAR',
    defs = '//service/LiDAR:ServiceDefinitions',
    out = 'krpc/services/lidar.py'
)

clientgen_python(
    name = 'services-dockingcamera',
    service = 'DockingCamera',
    defs = '//service/DockingCamera:ServiceDefinitions',
    out = 'krpc/services/dockingcamera.py'
)

test_suite(
    name = 'test',
    tests = [':client', ':lint']
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
