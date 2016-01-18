load('/tools/build/inkscape', 'png_images')
load('/tools/build/sphinx', 'sphinx_html', 'sphinx_latex')
load('/doc/generate', 'generate', 'generate_multiple')
load('/config', 'version')

filegroup(
    name = 'doc',
    srcs = [
        ':html',
        ':latex'
    ]
)

doc_srcs = [
    'src/conf.py',
    'src/_static/custom.css',
    'src/_templates/layout.html',
    'src/dictionary.txt',
    ':rst',
    ':images',
    ':scripts',
    ':python-api',
    ':cpp-api',
    ':lua-api'
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
        'src/scripts/*.py',
        'src/scripts/*.cpp',
        'src/scripts/*.lua'
    ])
)

defs = [
    '//server:ServiceDefinitions',
    '//service/SpaceCenter:ServiceDefinitions',
    '//service/KerbalAlarmClock:ServiceDefinitions',
    '//service/InfernalRobotics:ServiceDefinitions'
]

generate_multiple(
    name = 'python-api',
    outdir = 'src/python',
    language = 'python',
    srcs = glob(['api/**/*.tmpl']),
    defs = defs
)

generate_multiple(
    name = 'cpp-api',
    outdir = 'src/cpp',
    language = 'cpp',
    srcs = glob(['api/**/*.tmpl']),
    defs = defs
)

generate_multiple(
    name = 'lua-api',
    outdir = 'src/lua',
    language = 'lua',
    srcs = glob(['api/**/*.tmpl']),
    defs = defs
)

# TODO move elsewhere
filegroup(
    name = 'generate_lib',
    srcs = glob(['lib/*.py', 'lib/*.tmpl'])
)
