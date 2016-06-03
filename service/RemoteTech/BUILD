load('/tools/build/csharp', 'csharp_library', 'csharp_assembly_info')
load('/tools/build/python', 'py_lint_test')
load('/tools/ServiceDefinitions/build', 'service_definitions')
load('/config', 'assembly_version', 'author')

filegroup(
    name = 'RemoteTech',
    srcs = [':KRPC.RemoteTech', ':ServiceDefinitions', 'CHANGES.txt'],
    visibility = ['//:__pkg__']
)

csharp_assembly_info(
    name = 'AssemblyInfo',
    title = 'KRPC.RemoteTech',
    description = 'RemoteTech service for kRPC',
    version = assembly_version,
    copyright = author
)

csharp_library(
    name = 'KRPC.RemoteTech',
    srcs = glob(['src/**/*.cs']) + [':AssemblyInfo'],
    deps = [
        '//server:KRPC',
        '//service/SpaceCenter:KRPC.SpaceCenter',
        '//tools/build/ksp:Google.Protobuf',
        '//tools/build/ksp:Assembly-CSharp',
        '//tools/build/ksp:Assembly-CSharp-firstpass',
        '//tools/build/ksp:UnityEngine',
        '//tools/build/ksp:UnityEngine.UI',
        '//tools/build/ksp:KSPUtil',
        '//tools/build/ksp:mscorlib',
        '//tools/build/ksp:System',
        '//tools/build/ksp:System.Core',
        '//tools/build/ksp:System.Xml',
        '//tools/build/ksp:System.Xml.Linq'
    ],
    visibility = ['//visibility:public']
)

service_definitions(
    name = 'ServiceDefinitions',
    assemblies = [':KRPC.RemoteTech', '//service/SpaceCenter:KRPC.SpaceCenter'],
    service = 'RemoteTech',
    out = 'KRPC.RemoteTech.json',
    visibility = ['//visibility:public']
)

test_suite(
    name = 'test',
    tests = [':lint']
)

py_lint_test(
    name = 'lint',
    srcs = glob(['test/*.py']),
    rcfile = 'pylint.rc',
    deps = ['//client/python', '//tools/krpctest'],
    size = 'small'
)
