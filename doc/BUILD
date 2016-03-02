load('/tools/build/image', 'png_images')
load('/tools/build/sphinx', 'sphinx_html', 'sphinx_latex')
load('/tools/docgen/build', 'docgen_multiple')
load('/config', 'version')
load('macros', 'cc_binary_multiple', 'csharp_binary_multiple', 'java_binary_multiple')

filegroup(
    name = 'doc',
    srcs = [
        ':html',
        ':latex'
    ]
)

doc_srcs = [
    ':conf',
    'src/_static/custom.css',
    'src/_templates/layout.html',
    'src/dictionary.txt',
    ':rst',
    ':images',
    ':scripts',
    ':cpp-api',
    ':csharp-api',
    ':lua-api',
    ':python-api',
    ':java-api'
]

sphinx_html(
    name = 'html',
    srcs = doc_srcs,
    path_map = {
        'doc/src/': ''
    },
    opts = {'version': version}
)

sphinx_latex(
    name = 'latex',
    srcs = doc_srcs,
    path_map = {
        'doc/src/': ''
    },
    opts = {'version': version},
    visibility = ['//:__pkg__']
)

genrule(
    name = 'conf',
    srcs = ['conf.py.tmpl'],
    outs = ['src/conf.py'],
    cmd = 'sed \'s/%VERSION%/'+version+'/g\' "$<" > "$@"'
)

filegroup(
    name = 'rst',
    srcs = glob(['src/**/*.rst'])
)

filegroup(
    name = 'images',
    srcs = ['generated-images'] + glob(['src/images/**/*.png'])
)

png_images(
    name = 'generated-images',
    srcs = glob(['src/images/**/*.svg'])
)

filegroup(
    name = 'scripts',
    srcs = glob([
        'src/crafts/*.craft',
        'src/scripts/*.cpp',
        'src/scripts/*.cs',
        'src/scripts/*.lua',
        'src/scripts/*.py',
        'src/scripts/*.java'
    ])
)

defs = [
    '//server:ServiceDefinitions',
    '//service/SpaceCenter:ServiceDefinitions',
    '//service/KerbalAlarmClock:ServiceDefinitions',
    '//service/InfernalRobotics:ServiceDefinitions'
]

docgen_multiple(
    name = 'cpp-api',
    outdir = 'src/cpp',
    language = 'cpp',
    srcs = glob(['api/**/*.tmpl']),
    defs = defs
)

docgen_multiple(
    name = 'csharp-api',
    outdir = 'src/csharp',
    language = 'csharp',
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

docgen_multiple(
    name = 'java-api',
    outdir = 'src/java',
    language = 'java',
    srcs = glob(['api/**/*.tmpl']),
    defs = defs
)

filegroup(
    name = 'compile-scripts',
    srcs = [
        ':compile-scripts-cpp',
        ':compile-scripts-csharp',
        ':compile-scripts-java'
    ]
)

cc_binary_multiple(
    name = 'compile-scripts-cpp',
    srcs = glob(['src/scripts/*.cpp']),
    deps = ['//client/cpp:krpc']
)

csharp_binary_multiple(
    name = 'compile-scripts-csharp',
    srcs = glob(['src/scripts/*.cs']),
    deps = [
        '//client/csharp:KRPC.Client',
        '//tools/build/mono-4.5:Google.Protobuf',
        '//tools/build/mono-4.5:mscorlib',
        '//tools/build/mono-4.5:System',
        '//tools/build/mono-4.5:System.Core',
        '//tools/build/mono-4.5:System.Runtime',
        '//tools/build/mono-4.5:System.IO',
        '//tools/build/mono-4.5:System.Xml',
        '//tools/build/mono-4.5:System.Xml.Linq'
    ]
)

java_binary_multiple(
    name = 'compile-scripts-java',
    srcs = glob(['src/scripts/*.java']),
    deps = ['//client/java:krpc-'+version, '@java_javatuples//:jar']
)
