load('/tools/build/csharp', 'csharp_reference')
load('/tools/build/protobuf/lua', 'protoc_lua_env')

exports_files(['LICENSE'])

package(default_visibility = ['//visibility:public'])

filegroup(
    name = 'protoc',
    srcs = select({
        ':windows_mingw': ['protoc-windows-x86_32.exe'],
        ':windows_msys64_mingw64': ['protoc-windows-x86_64.exe'],
        ':windows_clang': ['protoc-windows-x86_32.exe'],
        ':darwin': ['protoc-osx-x86_32.exe'],
        ':k8': ['protoc-linux-x86_64.exe'],
        ':piii': ['protoc-linux-x86_32.exe'],
        ':arm': ['protoc-linux-arm32.exe'],
        ':freebsd': ['protoc-linux-x86_32.exe'],
    }),
)

java_import(
    name = 'protobuf',
    jars = ['protobuf-java-3.0.0-beta-2.jar'],
)

# For bootstrapping JavaBuilder
filegroup(
    name = 'protobuf-jars',
    srcs = ['protobuf-java-3.0.0-beta-2.jar'],
)

config_setting(
    name = 'windows_mingw',
    values = {'compiler': 'windows_mingw'},
)

config_setting(
    name = 'windows_msys64_mingw64',
    values = {'compiler': 'windows_msys64_mingw64'},
)

config_setting(
    name = 'windows_clang',
    values = {'compiler': 'windows_clang'},
)

config_setting(
    name = 'darwin',
    values = {'host_cpu': 'darwin'}
)

config_setting(
    name = 'k8',
    values = {'host_cpu': 'k8'}
)

config_setting(
    name = 'piii',
    values = {'host_cpu': 'piii'}
)

config_setting(
    name = 'arm',
    values = {'host_cpu': 'arm'}
)

config_setting(
    name = 'freebsd',
    values = {'host_cpu': 'freebsd'}
)

# python env for running protoc-lua plugin
protoc_lua_env(name = 'protoc-lua-env')
