load('/tools/build/sphinx', 'sphinx_build', 'sphinx_spelling_test', 'sphinx_linkcheck_test')
load('/tools/build/pkg', 'stage_files')
load('/tools/build/python', 'py_script', 'py_lint_test')
load('/tools/build/image', 'png_images')
load('/tools/krpctools/docgen', 'docgen_multiple')
load('/doc/test', 'check_documented_test')
load('/config', 'version')
load('macros', 'csharp_binary_multiple', 'csharp_library_multiple', 'cc_binary_multiple', 'java_binary_multiple')

filegroup(
    name = 'doc',
    srcs = [
        ':html',
        ':pdf'
    ]
)

stage_files(
    name = 'srcs',
    srcs = glob([
        'src/**/*.rst',
        'src/images/**/*.png',
        'src/crafts/*.craft',
        'src/scripts/*.cs',
        'src/scripts/*.cpp',
        'src/scripts/*.java',
        'src/scripts/*.lua',
        'src/scripts/*.py'
    ]) + [
        ':conf',
        'src/_static/custom.css',
        'src/_templates/layout.html',
        'src/dictionary.txt',
        ':images',
        ':csharp-api',
        ':cpp-api',
        ':java-api',
        ':lua-api',
        ':python-api'
    ],
    path_map = {
        'doc/src/': ''
    }
)

py_script(
    name = 'sphinx-build',
    script = 'sphinx-build',
    pkg = '@python_sphinx//file',
    deps = [
        '@python_pbr//file',
        '@python_alabaster//file',
        '@python_babel//file',
        '@python_beautifulsoup4//file',
        '@python_docutils//file',
        '@python_javalang//file',
        '@python_jinja2//file',
        '@python_lxml//file',
        '@python_markupsafe//file',
        '@python_pyenchant//file',
        '@python_pygments//file',
        '@python_pytz//file',
        '@python_six//file',
        '@python_snowballstemmer//file',
        '@python_sphinxcontrib_spelling//file',
        '@python_sphinx_csharp//file',
        '@python_sphinx_java//file',
        '@python_sphinx_lua//file',
        '@python_sphinx_rtd_theme//file',
        '@python_sphinx_tabs//file'
    ],
    visibility = ['//visibility:public']
)

genrule(
    name = 'conf',
    srcs = ['conf.py.tmpl'],
    outs = ['src/conf.py'],
    cmd = 'sed \'s/%VERSION%/'+version+'/g\' "$<" > "$@"'
)

sphinx_build(
    name = 'html',
    out = 'html.zip',
    srcs = [':srcs'],
    sphinx_build = ':sphinx-build',
    builder = 'html',
    opts = {'version': version}
)

sphinx_build(
    name = 'pdf',
    out = 'kRPC.pdf',
    srcs = [':srcs'],
    sphinx_build = ':sphinx-build',
    builder = 'latex',
    opts = {'version': version},
    visibility = ['//:__pkg__']
)

png_images(
    name = 'images',
    srcs = glob(['src/images/**/*.svg'])
)

defs = [
    '//server:ServiceDefinitions',
    '//service/SpaceCenter:ServiceDefinitions',
    '//service/Drawing:ServiceDefinitions',
    '//service/InfernalRobotics:ServiceDefinitions',
    '//service/KerbalAlarmClock:ServiceDefinitions',
    '//service/RemoteTech:ServiceDefinitions',
    '//service/UI:ServiceDefinitions'
]

docgen_multiple(
    name = 'csharp-api',
    outdir = 'src/csharp',
    language = 'csharp',
    srcs = glob(['api/**/*.tmpl']),
    defs = defs
)

docgen_multiple(
    name = 'cpp-api',
    outdir = 'src/cpp',
    language = 'cpp',
    srcs = glob(['api/**/*.tmpl']),
    defs = defs
)

docgen_multiple(
    name = 'java-api',
    outdir = 'src/java',
    language = 'java',
    srcs = glob(['api/**/*.tmpl']),
    defs = defs
)

docgen_multiple(
    name = 'lua-api',
    outdir = 'src/lua',
    language = 'lua',
    srcs = glob(['api/**/*.tmpl']),
    defs = defs
)

docgen_multiple(
    name = 'python-api',
    outdir = 'src/python',
    language = 'python',
    srcs = glob(['api/**/*.tmpl']),
    defs = defs
)

test_suite(
    name = 'test',
    tests = [':spelling', ':check-documented', ':lint'] # ':linkcheck'
)

test_suite(
    name = 'ci-test',
    tests = [':spelling', ':check-documented']
)

sphinx_spelling_test(
    name = 'spelling',
    srcs = [':srcs'],
    sphinx_build = ':sphinx-build',
    opts = {'version': version},
    size = 'small'
)

sphinx_linkcheck_test(
    name = 'linkcheck',
    srcs = [':srcs'],
    sphinx_build = ':sphinx-build',
    opts = {'version': version},
    tags = ['requires-network']
)

test_suite(
    name = 'check-documented',
    tests = [
        ':check-documented-csharp',
        ':check-documented-cpp',
        ':check-documented-java',
        ':check-documented-lua',
        ':check-documented-python'
    ]
)

check_documented_test(
    name = 'check-documented-csharp',
    srcs = [':csharp-api'],
    members = 'order.txt',
    size = 'small'
)

check_documented_test(
    name = 'check-documented-cpp',
    srcs = [':cpp-api'],
    members = 'order.txt',
    size = 'small'
)

check_documented_test(
    name = 'check-documented-java',
    srcs = [':java-api'],
    members = 'order.txt',
    size = 'small'
)

check_documented_test(
    name = 'check-documented-lua',
    srcs = [':lua-api'],
    members = 'order.txt',
    size = 'small'
)

check_documented_test(
    name = 'check-documented-python',
    srcs = [':python-api'],
    members = 'order.txt',
    size = 'small'
)

filegroup(
    name = 'compile-scripts',
    srcs = [
        ':compile-scripts-csharp',
        ':compile-scripts-cpp',
        ':compile-scripts-java'
    ]
)

filegroup(
    name = 'compile-scripts-csharp',
    srcs = [
        ':compile-scripts-csharp-exes',
        ':compile-scripts-csharp-libs'
    ]
)

csharp_binary_multiple(
    name = 'compile-scripts-csharp-exes',
    srcs = glob(['src/scripts/*.cs'], exclude=['src/scripts/*.lib.cs']),
    deps = [
        '//client/csharp:KRPC.Client',
        '//tools/build/mono-4.5:Google.Protobuf',
        '//tools/build/mono-4.5:mscorlib',
        '//tools/build/mono-4.5:System',
        '//tools/build/mono-4.5:System.Core',
        '//tools/build/mono-4.5:System.Runtime',
        '//tools/build/mono-4.5:System.IO',
        '//tools/build/mono-4.5:System.Xml'
    ]
)

csharp_library_multiple(
    name = 'compile-scripts-csharp-libs',
    srcs = glob(['src/scripts/*.lib.cs']),
    deps = [
        '//server:KRPC',
        '//tools/build/ksp:Google.Protobuf',
        '//tools/build/ksp:Assembly-CSharp',
        '//tools/build/ksp:Assembly-CSharp-firstpass',
        '//tools/build/ksp:UnityEngine',
        '//tools/build/ksp:UnityEngine.UI',
        '//tools/build/ksp:mscorlib',
        '//tools/build/ksp:System',
        '//tools/build/ksp:System.Core',
        '//tools/build/ksp:System.Xml'
    ]
)

cc_binary_multiple(
    name = 'compile-scripts-cpp',
    srcs = glob(['src/scripts/*.cpp']),
    deps = ['//client/cpp:krpc']
)

java_import(
    name = 'javatuples',
    jars = ['@java_javatuples//file']
)

java_import(
    name = 'javaprotobuf',
    jars = ['@java_protobuf//file']
)

java_binary_multiple(
    name = 'compile-scripts-java',
    srcs = glob(['src/scripts/*.java']),
    deps = [
        '//client/java:krpc-jar',
        ':javaprotobuf',
        ':javatuples'
    ]
)

test_suite(
    name = 'lint',
    tests = [
        ':lint-python'
    ]
)

py_lint_test(
    name = 'lint-python',
    srcs = glob(['src/scripts/*.py'], exclude=['src/scripts/communication-protocol.py']),
    pep8_config = 'src/scripts/pep8.ini',
    pylint_config = 'src/scripts/pylint.rc',
    size = 'small'
)
