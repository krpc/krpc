load('//tools/build/protobuf:lua.bzl', 'protoc_lua_env')
load('//tools/build/protobuf:nanopb.bzl', 'protoc_nanopb_env')
package(default_visibility = ['//visibility:public'])
exports_files(['LICENSE'])
protoc_lua_env(name = 'protoc-lua-env')
protoc_nanopb_env(name = 'protoc-nanopb-env')

filegroup(
    name = 'protoc',
    srcs = select({
        ':windows_mingw': ['@protoc_win32//:bin/protoc.exe'],
        ':windows_msys64_mingw64': ['@protoc_win32//:bin/protoc.exe'],
        ':windows_msys64': ['@protoc_win32//:bin/protoc.exe'],
        ':windows_clang': ['@protoc_win32//:bin/protoc.exe'],
        ':darwin': ['@protoc_osx_x86_32//:bin/protoc'],
        ':k8': ['@protoc_linux_x86_64//:bin/protoc'],
        ':piii': ['@protoc_linux_x86_32//:bin/protoc'],
        ':freebsd': ['@protoc_linux_x86_32//:bin/protoc']
    }),
    visibility = ['//visibility:public']
)

config_setting(
    name = 'windows_mingw',
    values = {'compiler': 'windows_mingw'},
)

config_setting(
    name = 'windows_msys64',
    values = {'compiler': 'windows_msys64'},
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
    values = {'host_cpu': 'darwin'},
)

config_setting(
    name = 'k8',
    values = {'host_cpu': 'k8'},
)

config_setting(
    name = 'piii',
    values = {'host_cpu': 'piii'},
)

config_setting(
    name = 'arm',
    values = {'host_cpu': 'arm'},
)

config_setting(
    name = 'freebsd',
    values = {'host_cpu': 'freebsd'},
)

config_setting(
    name = 's390x',
    values = {'host_cpu': 's390x'},
)
