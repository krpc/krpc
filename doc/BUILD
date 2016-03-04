load('/tools/build/sphinx', 'sphinx_build', 'sphinx_spelling_test')
load('/tools/build/pkg', 'stage_files')
load('/tools/build/python', 'py_script')
load('/tools/build/image', 'png_images')
load('/tools/krpctools/docgen', 'docgen_multiple')
load('/doc/test', 'check_documented_test')
load('/config', 'version')

filegroup(
    name = 'doc',
    srcs = [
        ':html',
        ':latex'
    ]
)

stage_files(
    name = 'srcs',
    srcs = glob([
        'src/**/*.rst',
        'src/images/**/*.png',
        'src/crafts/*.craft',
        'src/scripts/*.cpp',
        'src/scripts/*.cs',
        'src/scripts/*.lua',
        'src/scripts/*.py',
        'src/scripts/*.java'
    ]) + [
        'src/conf.py',
        'src/_static/custom.css',
        'src/_templates/layout.html',
        'src/dictionary.txt',
        ':images',
        ':cpp-api',
        ':csharp-api',
        ':lua-api',
        ':python-api',
        ':java-api'
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
        '@python_sphinx_rtd_theme//file'
    ],
    visibility = ['//visibility:public']
)

sphinx_build(
    name = 'html',
    srcs = [':srcs'],
    sphinx_build = ':sphinx-build',
    builder = 'html',
    opts = {'version': version}
)

sphinx_build(
    name = 'latex',
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
    '//service/KerbalAlarmClock:ServiceDefinitions',
    '//service/InfernalRobotics:ServiceDefinitions'
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
    tests = ['spelling', 'check-documented']
)

sphinx_spelling_test(
    name = 'spelling',
    srcs = [':srcs'],
    sphinx_build = ':sphinx-build',
    opts = {'version': version},
    size = 'small'
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
