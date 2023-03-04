workspace(name = 'krpc')

load('@bazel_tools//tools/build_defs/repo:http.bzl', 'http_archive')
load('@bazel_tools//tools/build_defs/repo:http.bzl', 'http_file')
load('@bazel_tools//tools/build_defs/repo:maven_rules.bzl', 'maven_jar')

http_archive(
    name = 'bazel_skylib',
    url = 'https://github.com/bazelbuild/bazel-skylib/releases/download/1.4.1/bazel-skylib-1.4.1.tar.gz',
    sha256 = 'b8a1527901774180afc798aeb28c4634bdccf19c4d98e7bdd1ce79d1fe9aaad7'
)

http_archive(
    name = 'rules_cc',
    url = 'https://github.com/bazelbuild/rules_cc/releases/download/0.0.6/rules_cc-0.0.6.tar.gz',
    sha256 = '3d9e271e2876ba42e114c9b9bc51454e379cbf0ec9ef9d40e2ae4cec61a31b40',
    strip_prefix = 'rules_cc-0.0.6'
)

http_archive(
    name = 'rules_java',
    url = 'https://github.com/bazelbuild/rules_java/releases/download/5.4.1/rules_java-5.4.1.tar.gz',
    sha256 = 'a1f82b730b9c6395d3653032bd7e3a660f9d5ddb1099f427c1e1fe768f92e395'
)

http_archive(
    name = 'rules_proto',
    url = 'https://github.com/bazelbuild/rules_proto/archive/refs/tags/5.3.0-21.7.tar.gz',
    sha256 = 'dc3fb206a2cb3441b485eb1e423165b231235a1ea9b031b4433cf7bc1fa460dd',
    strip_prefix = 'rules_proto-5.3.0-21.7'
)

http_archive(
    name = 'rules_python',
    url = 'https://github.com/bazelbuild/rules_python/releases/download/0.18.1/rules_python-0.18.1.tar.gz',
    sha256 = '29a801171f7ca190c543406f9894abf2d483c206e14d6acbd695623662320097',
    strip_prefix = 'rules_python-0.18.1'
)

http_archive(
    name = 'rules_pkg',
    url = 'https://github.com/bazelbuild/rules_pkg/releases/download/0.8.1/rules_pkg-0.8.1.tar.gz',
    sha256 = '8c20f74bca25d2d442b327ae26768c02cf3c99e93fad0381f32be9aab1967675'
)

http_archive(
    name = 'rules_ruby',
    url = 'https://github.com/bazelruby/rules_ruby/archive/refs/tags/v0.6.0.tar.gz',
    sha256 = '5035393cb5043d49ca9de78acb9e8c8622a193f6463a57ad02383a622b6dc663',
    strip_prefix = 'rules_ruby-0.6.0'
)

http_archive(
    name = 'bazelruby_rules_ruby',
    url = 'https://github.com/bazelruby/rules_ruby/archive/refs/tags/v0.6.0.tar.gz',
    sha256 = '5035393cb5043d49ca9de78acb9e8c8622a193f6463a57ad02383a622b6dc663',
    strip_prefix = 'rules_ruby-0.6.0'
)

http_archive(
    name = 'com_google_protobuf',
    url = 'https://github.com/protocolbuffers/protobuf/releases/download/v22.0/protobuf-22.0.tar.gz',
    sha256 = 'e340f39fad1e35d9237540bcd6a2592ccac353e5d21d0f0521f6ab77370e0142',
    strip_prefix = 'protobuf-22.0'
)

http_archive(
    name = 'com_google_googletest',
    url = 'https://github.com/google/googletest/archive/refs/tags/v1.13.0.tar.gz',
    sha256 = 'ad7fdba11ea011c1d925b3289cf4af2c66a352e18d4c7264392fead75e919363',
    strip_prefix = 'googletest-1.13.0'
)

http_archive(
    name = 'com_google_absl',
    url = 'https://github.com/abseil/abseil-cpp/archive/refs/tags/20230125.1.tar.gz',
    sha256 = '81311c17599b3712069ded20cca09a62ab0bf2a89dfa16993786c8782b7ed145',
    strip_prefix = 'abseil-cpp-20230125.1'
)

http_archive(
    name = 'com_googlesource_code_re2',
    url = 'https://github.com/google/re2/archive/refs/tags/2023-02-01.tar.gz',
    sha256 = 'cbce8b7803e856827201a132862e41af386e7afd9cc6d9a9bc7a4fa4d8ddbdde',
    strip_prefix = 're2-2023-02-01'
)

http_archive(
    name = 'upb',
    url = 'https://github.com/protocolbuffers/upb/archive/c4b98ddfb5f9cb925ffb556f45c33e2f83c9578a.zip',
    sha256 = '8da22f8933e4e01fd5aacd17ae1d6bf82e57daf1f84ac7059fba2e01f0188cba',
    strip_prefix = 'upb-c4b98ddfb5f9cb925ffb556f45c33e2f83c9578a'
)

http_archive(
    name = 'utf8_range',
    url = 'https://github.com/protocolbuffers/utf8_range/archive/72c943dea2b9240cd09efde15191e144bc7c7d38.zip',
    sha256 = 'dffb52973f0226fe5df6d9ed40b0d1af1bb89f54beec6a64b66d25e7db9c4152',
    strip_prefix = 'utf8_range-72c943dea2b9240cd09efde15191e144bc7c7d38'
)

http_archive(
    name = 'zlib',
    url = 'https://zlib.net/zlib-1.2.13.tar.gz',
    build_file = '@com_google_protobuf//:third_party/zlib.BUILD',
    sha256 = 'b3a24de97a8fdbc835b9833169501030b8977031bcb54b3b3ac13740f846ab30',
    strip_prefix = 'zlib-1.2.13'
)

http_archive(
    name = 'protoc_linux_x86_32',
    url = 'https://github.com/protocolbuffers/protobuf/releases/download/v22.0/protoc-22.0-linux-x86_32.zip',
    sha256 = 'fdb8aea58cc156989f500d12cba50625dd1718f48c4c29f29300e5dcb8fd653e',
    build_file_content = "exports_files(['bin/protoc'])"
)

http_archive(
    name = 'protoc_linux_x86_64',
    url = 'https://github.com/protocolbuffers/protobuf/releases/download/v22.0/protoc-22.0-linux-x86_64.zip',
    sha256 = '9ceff6c3945d521d1d0f42f9f57f6ef7cf3f581a9d303a027ba19b192045d1a2',
    build_file_content = "exports_files(['bin/protoc'])"
)

http_archive(
    name = 'protoc_osx_x86_32',
    url = 'https://github.com/protocolbuffers/protobuf/releases/download/v22.0/protoc-22.0-osx-x86_64.zip',
    sha256 = '1e0ad38fcf20a4b1cdeffe40f9188c4d1c30a9dd515cf92c8b57f629227f0eb3',
    build_file_content = "exports_files(['bin/protoc'])"
)

http_archive(
    name = 'protoc_win32',
    url = 'https://github.com/protocolbuffers/protobuf/releases/download/v22.0/protoc-22.0-win32.zip',
    sha256 = '1cf031ba53b6963de475fcd07a2dbcada6c4b74ef3f8e587346603a940bbf772',
    build_file_content = "exports_files(['bin/protoc.exe'])"
)

http_archive(
    name = 'protoc_3.9.1_linux_x86_32',
    url = 'https://github.com/protocolbuffers/protobuf/releases/download/v3.9.1/protoc-3.9.1-linux-x86_32.zip',
    sha256 = '1094d7896f93b8987b0e05c110c0635bab7cf63aa24592c5d34cd37b590b5aeb',
    build_file_content = "exports_files(['bin/protoc'])"
)

http_archive(
    name = 'protoc_3.9.1_linux_x86_64',
    url = 'https://github.com/protocolbuffers/protobuf/releases/download/v3.9.1/protoc-3.9.1-linux-x86_64.zip',
    sha256 = '77410d08e9a3c1ebb68afc13ee0c0fb4272c01c20bfd289adfb51b1c622bab07',
    build_file_content = "exports_files(['bin/protoc'])"
)

http_archive(
    name = 'protoc_3.9.1_osx_x86_32',
    url = 'https://github.com/protocolbuffers/protobuf/releases/download/v3.9.1/protoc-3.9.1-osx-x86_32.zip',
    sha256 = 'e7b7377917f6b9ec22c80188936c60380edc684e5bdc96c2993fc79e3e54c042',
    build_file_content = "exports_files(['bin/protoc'])"
)

http_archive(
    name = 'protoc_3.9.1_win32',
    url = 'https://github.com/protocolbuffers/protobuf/releases/download/v3.9.1/protoc-3.9.1-win32.zip',
    sha256 = '6543fe3fffb6caeb9c8a091afeefbb1a7e7112bc0e00d7b7e89e69e3a1844069',
    build_file_content = "exports_files(['bin/protoc.exe'])"
)

http_archive(
    name = 'protoc_nanopb',
    url = 'https://jpa.kapsi.fi/nanopb/download/nanopb-0.4.7-linux-x86.tar.gz',
    sha256 = 'e8a154d3b6631696cb42e3acba338ab738509af56571ebc9c35d7a754d6e5b48',
    strip_prefix = 'nanopb-0.4.7-linux-x86',
    build_file_content = "filegroup(name = 'plugin', srcs = ['generator'], visibility = ['//visibility:public'])"
)

http_archive(
    name = 'c_nanopb',
    url = 'https://jpa.kapsi.fi/nanopb/download/nanopb-0.4.7-linux-x86.tar.gz',
    sha256 = 'e8a154d3b6631696cb42e3acba338ab738509af56571ebc9c35d7a754d6e5b48',
    strip_prefix = 'nanopb-0.4.7-linux-x86',
    build_file_content = """
exports_files([
    'LICENSE.txt', 'pb.h', 'pb_common.h', 'pb_common.c', 'pb_encode.h', 'pb_encode.c', 'pb_decode.h', 'pb_decode.c'
])

cc_library(
    name = 'nanopb',
    srcs = ['pb.h', 'pb_common.h', 'pb_common.c', 'pb_encode.h', 'pb_encode.c', 'pb_decode.h', 'pb_decode.c'],
    hdrs = ['pb.h', 'pb_common.h', 'pb_encode.h', 'pb_decode.h'],
    includes = ['./'],
    include_prefix = 'krpc_cnano', # FIXME: don't do this here
    visibility = ['//visibility:public']
)

filegroup(
    name = 'srcs',
    srcs = glob(['*.h', '*.c']),
    visibility = ['//visibility:public']
)
"""
)

http_file(
    name = 'csharp_nuget',
    url = 'https://dist.nuget.org/win-x86-commandline/v4.7.1/nuget.exe',
    sha256 = '82e3aa0205415cd18d8ae34613911717dad3ed4e8ac58143e55ca432a5bf3c0a'
)

http_archive(
    name = 'csharp_protobuf',
    url = 'https://www.nuget.org/api/v2/package/Google.Protobuf/3.22.0',
    sha256 = 'c7c6700c8cbeba874cff61f65385684857bca37e4b237f87034bcadc30ed5df2',
    type = 'zip',
    build_file_content = "exports_files(['lib/net45/Google.Protobuf.dll'])"
)

http_archive(
    name = 'csharp_protobuf_3.9.1',
    url = 'https://www.nuget.org/api/v2/package/Google.Protobuf/3.9.1',
    sha256 = 'b4363bb9d1c2b6721624571936e3e1f14ebdf2ecd8788d2584b549c6dce8348b',
    type = 'zip',
    build_file_content = "exports_files(['lib/net45/Google.Protobuf.dll'])"
)

http_file(
    name = 'csharp_protobuf_3.9.1_net35',
    url = 'https://s3.amazonaws.com/krpc/lib/protobuf-3.9.1-net35/Google.Protobuf.dll',
    sha256 = 'd0ddb80510810fa53ee124afbd57845e657eaa9016ed7a6edd4d8ecffedf66b5'
)

http_file(
    name = 'csharp_krpc_io_ports',
    url = 'https://github.com/krpc/krpc-io-ports/releases/download/v1.0.0/KRPC.IO.Ports.dll',
    sha256 = '558b0c1649fbc44b518d9de8957fe30e7c9c42d73c62d63d165f6f136fab3ec5'
)

http_file(
    name = 'csharp_krpc_io_ports_license',
    url = 'https://raw.githubusercontent.com/krpc/krpc-io-ports/master/LICENSE',
    sha256 = 'a6b8912947cb14e02cefb704859d12a03d3c8792344fcf5831ef27c1efcd6d20'
)

http_archive(
    name = 'csharp_nunit',
    url = 'https://github.com/nunit/nunitv2/releases/download/2.6.4/NUnit-2.6.4.zip',
    sha256 = '1bd925514f31e7729ccde40a38a512c2accd86895f93465f3dfe6d0b593d7170',
    strip_prefix = 'NUnit-2.6.4',
    build_file_content = """
filegroup(
    name = 'nunit_exe',
    srcs = ['bin/nunit-console.exe'],
    visibility = ['//visibility:public'],
)

filegroup(
    name = 'nunit_exe_libs',
    srcs = glob(['bin/lib/*.dll']),
    visibility = ['//visibility:public'],
)

filegroup(
    name = 'nunit_framework',
    srcs = ['bin/framework/nunit.framework.dll'],
    visibility = ['//visibility:public'],
)
"""
)

http_archive(
    name = 'csharp_moq',
    url = 'http://www.nuget.org/api/v2/package/Moq/4.2.1510.2205',
    sha256 = '7a86f2ed0e134601e75a4fa28c7f7c399f6abc33f091dbc024ad8b212b8c3c85',
    type = 'zip',
    build_file_content = "exports_files(['lib/net40/Moq.dll'])"
)

http_archive(
    name = 'csharp_json',
    url = 'https://www.nuget.org/api/v2/package/Newtonsoft.Json/13.0.2',
    sha256 = '112ca3b7f47bcbd743befcf949fb68ce4f9eff73ad9f7f1b39c2c61a1ebd3add',
    type = 'zip',
    build_file_content = "exports_files(['lib/net45/Newtonsoft.Json.dll'])"
)

http_archive(
    name = 'csharp_options',
    url = 'https://www.nuget.org/api/v2/package/NDesk.Options/0.2.1',
    sha256 = '0fa76d0ed1eb9fba757b0aa677903e1b8873735eec136a51dde24eda57d10c61',
    type = 'zip',
    build_file_content = "exports_files(['lib/NDesk.Options.dll'])"
)

http_archive(
    name = 'cpp_asio',
    url = 'https://s3.amazonaws.com/krpc/lib/asio/asio-1.24.0.tar.gz',
    strip_prefix = 'asio-1.24.0',
    sha256 = '2f23ef6eada06ecc1472af5df6365ed4f15452ccd07dc0a6851fa20d571dba94',
    build_file_content = """
cc_library(
    name = 'asio',
    hdrs = glob(['include/*', 'include/**/*']),
    includes = ['include'],
    visibility = ['//visibility:public']
)
"""
)

http_archive(
    name = 'cpp_googletest',
    url = 'https://github.com/google/googletest/archive/refs/tags/v1.13.0.tar.gz',
    strip_prefix = 'googletest-1.13.0',
    sha256 = 'ad7fdba11ea011c1d925b3289cf4af2c66a352e18d4c7264392fead75e919363',
    build_file_content = """
cc_library(
    name = 'gtest',
    srcs = glob(['googletest/src/*.cc'], exclude = ['googletest/src/gtest-all.cc']),
    hdrs = glob(['**/*.h', 'googletest/src/*.cc']),
    includes = [
        './',
        'googletest',
        'googletest/include',
        'include'
    ],
    linkopts = ['-pthread'],
    visibility = ['//visibility:public'],
)

cc_library(
    name = 'gmock',
    srcs = glob(['googlemock/src/*.cc'], exclude = ['googlemock/src/gmock-all.cc']),
    hdrs = glob(['**/*.h', 'googlemock/src/*.cc']),
    includes = [
        './',
        'googlemock',
        'googlemock/include',
        'include'
    ],
    deps = [':gtest'],
    linkopts = ['-pthread'],
    visibility = ['//visibility:public'],
)
"""
)

http_file(
    name = 'm4_stdcxx',
    url = 'http://git.savannah.gnu.org/gitweb/?p=autoconf-archive.git;a=blob_plain;f=m4/ax_cxx_compile_stdcxx.m4;hb=34104ea9e635fae5551fd1d6495a80f8041c4adc',
    sha256 = 'a6f7cdef49579d995976baece6e605aca1f2c8b0cb771bbae4d7b816710dcb4c'
)

http_file(
    name = 'm4_stdcxx_11',
    url = 'http://git.savannah.gnu.org/gitweb/?p=autoconf-archive.git;a=blob_plain;f=m4/ax_cxx_compile_stdcxx_11.m4;hb=34104ea9e635fae5551fd1d6495a80f8041c4adc',
    sha256 = '98a0053e6b3fda3243cca0a40e7d7b496cb05ce4716cf6f1663e86c8ad36f1e8'
)

maven_jar(
    name = 'java_protobuf',
    artifact = 'com.google.protobuf:protobuf-java:3.22.0',
    sha1 = 'aa58e31e88e9974452f0498e237532df5732257a'
)

maven_jar(
    name = 'java_junit',
    artifact = 'junit:junit:4.12',
    sha1 = '2973d150c0dc1fefe998f834810d68f278ea58ec'
)

maven_jar(
    name = 'java_hamcrest',
    artifact = 'org.hamcrest:hamcrest-core:1.3',
    sha1 = '42a25dc3219429f0e5d060061f71acb49bf010a0'
)

maven_jar(
    name = 'java_checkstyle',
    artifact = 'com.puppycrawl.tools:checkstyle:7.1.2',
    sha1 = 'a140779aa6cf2dbe25187ad22b28e14e57e77f14'
)

maven_jar(
    name = 'java_apache_commons_beanutils',
    artifact = 'commons-beanutils:commons-beanutils:1.9.3',
    sha1 = 'c845703de334ddc6b4b3cd26835458cb1cba1f3d'
)

maven_jar(
    name = 'java_apache_commons_cli',
    artifact = 'commons-cli:commons-cli:1.3.1',
    sha1 = '1303efbc4b181e5a58bf2e967dc156a3132b97c0'
)

maven_jar(
    name = 'java_apache_commons_collections',
    artifact = 'commons-collections:commons-collections:3.2.2',
    sha1 = '8ad72fe39fa8c91eaaf12aadb21e0c3661fe26d5'
)

maven_jar(
    name = 'java_apache_commons_logging',
    artifact = 'commons-logging:commons-logging:1.2',
    sha1 = '4bfc12adfe4842bf07b657f0369c4cb522955686'
)

maven_jar(
    name = 'java_antlr2',
    artifact = 'antlr:antlr:2.7.7',
    sha1 = '83cd2cd674a217ade95a4bb83a8a14f351f48bd0'
)

maven_jar(
    name = 'java_antlr4_runtime',
    artifact = 'org.antlr:antlr4-runtime:4.5.3',
    sha1 = '2609e36f18f7e8d593cc1cddfb2ac776dc96b8e0'
)

maven_jar(
    name = 'java_guava',
    artifact = 'com.google.guava:guava:19.0',
    sha1 = '6ce200f6b23222af3d8abb6b6459e6c44f4bb0e9'
)

maven_jar(
    name = 'java_javatuples',
    artifact = 'org.javatuples:javatuples:1.2',
    sha1 = '507312ac4b601204a72a83380badbca82683dd36'
)

http_archive(
    name = 'protoc_lua',
    url = 'https://github.com/djungelorm/protobuf-lua/archive/v1.1.2.tar.gz',
    sha256 = '28f4daa026effb81cebfdf580b0fc5732e520c0f4ade53e940052d89cddf4264',
    strip_prefix = 'protobuf-lua-1.1.2',
    build_file_content = """
filegroup(
    name = 'plugin',
    srcs = [
        'protoc-plugin/protoc-gen-lua'
    ],
    visibility = ['//visibility:public']
)"""
)

http_file(
    name = 'lua_protobuf',
    url = 'https://github.com/djungelorm/protobuf-lua/releases/download/v1.1.2/protobuf-1.1.2-0.src.rock',
    sha256 = 'bae53a6fdfef5e7e99fc7db07eb958002878c768b2951af93e47f40da1724005',
    downloaded_file_path = 'protobuf-1.1.2-0.src.rock'
)

http_file(
    name = 'lua_luasocket',
    url = 'https://luarocks.org/manifests/luasocket/luasocket-3.0rc1-2.src.rock',
    sha256 = '3882f2a1e1c6145ceb43ead385b861b97fa2f8d487e8669ec5b747406ab251c7',
    downloaded_file_path = 'luasocket-3.0rc1-2.src.rock'
)

http_file(
    name = 'lua_luafilesystem',
    url = 'https://luarocks.org/manifests/hisham/luafilesystem-1.6.3-2.src.rock',
    sha256 = '872914421d4585f37ce72be40003e2bfdd22e017b55e87b0f89c467cc0df30e0',
    downloaded_file_path = 'luafilesystem-1.6.3-2.src.rock'
)

http_file(
    name = 'lua_penlight',
    url = 'http://luarocks.org/repositories/rocks/penlight-1.3.1-1.src.rock',
    sha256 = '13c6fcc5058a998505ddc4b52496f591d7d37ed2efa9a46a2c39db6183f38783',
    downloaded_file_path = 'penlight-1.3.1-1.src.rock'
)

http_file(
    name = 'lua_luaunit',
    url = 'https://luarocks.org/manifests/bluebird75/luaunit-3.2.1-1.src.rock',
    sha256 = '7ae20f3b68e77e3be52fc95c147eccfaef33206a7985320061fb9352d8565741',
    downloaded_file_path = 'luaunit-3.2.1-1.src.rock'
)

http_file(
    name = 'python_alabaster',
    url = 'https://files.pythonhosted.org/packages/94/71/a8ee96d1fd95ca04a0d2e2d9c4081dac4c2d2b12f7ddb899c8cb9bfd1532/alabaster-0.7.13.tar.gz',
    sha256 = 'a27a4a084d5e690e16e01e03ad2b2e552c61a65469419b907243193de1a84ae2',
    downloaded_file_path = 'alabaster-0.7.13.tar.gz'
)

http_file(
    name = 'python_astroid',
    url = 'https://files.pythonhosted.org/packages/15/e5/7dea50225cd8b44f1488ae83a243467fe6d2a3c4f611d865085b4bba67e5/astroid-2.14.2.tar.gz',
    sha256 = 'a3cf9f02c53dd259144a7e8f3ccd75d67c9a8c716ef183e0c1f291bc5d7bb3cf',
    downloaded_file_path = 'astroid-2.14.2.tar.gz'
)

http_file(
    name = 'python_babel',
    url = 'https://files.pythonhosted.org/packages/61/7b/a57e328fb3001da93b523454314e5eca32bfb0ef25682409420b1884bb47/Babel-2.12.0.tar.gz',
    sha256 = '468e6cd1e2b571a1663110fc737e3a7d9069d038e0c9c4a7f158caeeafe4089c',
    downloaded_file_path = 'Babel-2.12.0.tar.gz'
)

http_file(
    name = 'python_certifi',
    url = 'https://files.pythonhosted.org/packages/37/f7/2b1b0ec44fdc30a3d31dfebe52226be9ddc40cd6c0f34ffc8923ba423b69/certifi-2022.12.7.tar.gz',
    sha256 = '35824b4c3a97115964b408844d64aa14db1cc518f6562e8d7261699d1350a9e3',
    downloaded_file_path = 'certifi-2022.12.7.tar.gz'
)

http_file(
    name = 'python_charset_normalizer',
    url = 'https://files.pythonhosted.org/packages/96/d7/1675d9089a1f4677df5eb29c3f8b064aa1e70c1251a0a8a127803158942d/charset-normalizer-3.0.1.tar.gz',
    sha256 = 'ebea339af930f8ca5d7a699b921106c6e29c617fe9606fa7baa043c1cdae326f',
    downloaded_file_path = 'charset-normalizer-3.0.1.tar.gz'
)

http_file(
    name = 'python_cpplint',
    url = 'https://files.pythonhosted.org/packages/18/72/ea0f4035bcf35d8f8df053657d7f3370d56ff4d4e6617021b6544b9958d4/cpplint-1.6.1.tar.gz',
    sha256 = 'd430ce8f67afc1839340e60daa89e90de08b874bc27149833077bba726dfc13a',
    downloaded_file_path = 'cpplint-1.6.1.tar.gz'
)

http_file(
    name = 'python_dill',
    url = 'https://files.pythonhosted.org/packages/7c/e7/364a09134e1062d4d5ff69b853a56cf61c223e0afcc6906b6832bcd51ea8/dill-0.3.6.tar.gz',
    sha256 = 'e5db55f3687856d8fbdab002ed78544e1c4559a130302693d839dfe8f93f2373',
    downloaded_file_path = 'dill-0.3.6.tar.gz'
)

http_file(
    name = 'python_docutils',
    url = 'https://files.pythonhosted.org/packages/6b/5c/330ea8d383eb2ce973df34d1239b3b21e91cd8c865d21ff82902d952f91f/docutils-0.19.tar.gz',
    sha256 = '33995a6753c30b7f577febfc2c50411fec6aac7f7ffeb7c4cfe5991072dcf9e6',
    downloaded_file_path = 'docutils-0.19.tar.gz'
)

http_file(
    name = 'python_idna',
    url = 'https://files.pythonhosted.org/packages/8b/e1/43beb3d38dba6cb420cefa297822eac205a277ab43e5ba5d5c46faf96438/idna-3.4.tar.gz',
    sha256 = '814f528e8dead7d329833b91c5faa87d60bf71824cd12a7530b5526063d02cb4',
    downloaded_file_path = 'idna-3.4.tar.gz'
)

http_file(
    name = 'python_imagesize',
    url = 'https://files.pythonhosted.org/packages/a7/84/62473fb57d61e31fef6e36d64a179c8781605429fd927b5dd608c997be31/imagesize-1.4.1.tar.gz',
    sha256 = '69150444affb9cb0d5cc5a92b3676f0b2fb7cd9ae39e947a5e11a36b4497cd4a',
    downloaded_file_path = 'imagesize-1.4.1.tar.gz'
)

http_file(
    name = 'python_isort',
    url = 'https://files.pythonhosted.org/packages/a9/c4/dc00e42c158fc4dda2afebe57d2e948805c06d5169007f1724f0683010a9/isort-5.12.0.tar.gz',
    sha256 = '8bef7dde241278824a6d83f44a544709b065191b95b6e50894bdc722fcba0504',
    downloaded_file_path = 'isort-5.12.0.tar.gz'
)

http_file(
    name = 'python_javalang',
    # Custom build of javasphinx to remove six dependency
    url = 'https://krpc.s3.amazonaws.com/lib/javasphinx/javalang-0.13.1.tar.gz',
    sha256 = 'd7e95268fff9e7a88091d5e5c95307cda1fcaf3bac3aba1d1ece6c1bcba91dd1',
    downloaded_file_path = 'javalang-0.13.1.tar.gz'
)

http_file(
    name = 'python_javasphinx',
    # Built from https://github.com/mathijs81/javasphinx
    url = 'https://krpc.s3.amazonaws.com/lib/javasphinx/javasphinx-0.9.16.tar.gz',
    sha256 = '97de0522b584fe2ba7d9ef5981f494509fd1a0a1fe7bf1d07c045a5f7d8f5287',
    downloaded_file_path = 'javasphinx-0.9.16.tar.gz'
)

http_file(
    name = 'python_jinja2',
    url = 'https://files.pythonhosted.org/packages/7a/ff/75c28576a1d900e87eb6335b063fab47a8ef3c8b4d88524c4bf78f670cce/Jinja2-3.1.2.tar.gz',
    sha256 = '31351a702a408a9e7595a8fc6150fc3f43bb6bf7e319770cbc0db9df9437e852',
    downloaded_file_path = 'Jinja2-3.1.2.tar.gz'
)

http_file(
    name = 'python_lazy_object_proxy',
    url = 'https://files.pythonhosted.org/packages/20/c0/8bab72a73607d186edad50d0168ca85bd2743cfc55560c9d721a94654b20/lazy-object-proxy-1.9.0.tar.gz',
    sha256 = '659fb5809fa4629b8a1ac5106f669cfc7bef26fbb389dda53b3e010d1ac4ebae',
    downloaded_file_path = 'lazy-object-proxy-1.9.0.tar.gz'
)

http_file(
    name = 'python_markupsafe',
    url = 'https://files.pythonhosted.org/packages/95/7e/68018b70268fb4a2a605e2be44ab7b4dd7ce7808adae6c5ef32e34f4b55a/MarkupSafe-2.1.2.tar.gz',
    sha256 = 'abcabc8c2b26036d62d4c746381a6f7cf60aafcc653198ad678306986b09450d',
    downloaded_file_path = 'MarkupSafe-2.1.2.tar.gz'
)

http_file(
    name = 'python_mccabe',
    url = 'https://files.pythonhosted.org/packages/e7/ff/0ffefdcac38932a54d2b5eed4e0ba8a408f215002cd178ad1df0f2806ff8/mccabe-0.7.0.tar.gz',
    sha256 = '348e0240c33b60bbdf4e523192ef919f28cb2c3d7d5c7794f74009290f236325',
    downloaded_file_path = 'mccabe-0.7.0.tar.gz'
)

http_file(
    name = 'python_packaging',
    url = 'https://files.pythonhosted.org/packages/47/d5/aca8ff6f49aa5565df1c826e7bf5e85a6df852ee063600c1efa5b932968c/packaging-23.0.tar.gz',
    sha256 = 'b6ad297f8907de0fa2fe1ccbd26fdaf387f5f47c7275fedf8cce89f99446cf97',
    downloaded_file_path = 'packaging-23.0.tar.gz'
)

http_file(
    name = 'python_platformdirs',
    url = 'https://files.pythonhosted.org/packages/11/39/702094fc1434a4408783b071665d9f5d8a1d0ba4dddf9dadf3d50e6eb762/platformdirs-3.0.0.tar.gz',
    sha256 = '8a1228abb1ef82d788f74139988b137e78692984ec7b08eaa6c65f1723af28f9',
    downloaded_file_path = 'platformdirs-3.0.0.tar.gz'
)

http_file(
    name = 'python_protobuf',
    url = 'https://files.pythonhosted.org/packages/f6/95/797a257a5db4a91dc2bc864c487ead56440014d741933a28c86d966b949e/protobuf-4.22.0.tar.gz',
    sha256 = '652d8dfece122a24d98eebfef30e31e455d300efa41999d1182e015984ac5930',
    downloaded_file_path = 'protobuf-4.22.0.tar.gz'
)

http_file(
    name = 'python_pycodestyle',
    url = 'https://files.pythonhosted.org/packages/06/6b/5ca0d12ef7dcf7d20dfa35287d02297f3e0f9e515da5183654c03a9636ce/pycodestyle-2.10.0.tar.gz',
    sha256 = '347187bdb476329d98f695c213d7295a846d1152ff4fe9bacb8a9590b8ee7053',
    downloaded_file_path = 'pycodestyle-2.10.0.tar.gz'
)

http_file(
    name = 'python_pyenchant',
    url = 'https://files.pythonhosted.org/packages/b1/a3/86763b6350727ca81c8fcc5bb5bccee416e902e0085dc7a902c81233717e/pyenchant-3.2.2.tar.gz',
    sha256 = '1cf830c6614362a78aab78d50eaf7c6c93831369c52e1bb64ffae1df0341e637',
    downloaded_file_path = 'pyenchant-3.2.2.tar.gz'
)

http_file(
    name = 'python_pygments',
    url = 'https://files.pythonhosted.org/packages/da/6a/c427c06913204e24de28de5300d3f0e809933f376e0b7df95194b2bb3f71/Pygments-2.14.0.tar.gz',
    sha256 = 'b3ed06a9e8ac9a9aae5a6f5dbe78a8a58655d17b43b93c078f094ddc476ae297',
    downloaded_file_path = 'Pygments-2.14.0.tar.gz'
)

http_file(
    name = 'python_pylint',
    url = 'https://files.pythonhosted.org/packages/96/d2/192ac213f4a61118eacc79efbc7441460b5d5be39e821e2ee282ef6c68a5/pylint-2.16.2.tar.gz',
    sha256 = '13b2c805a404a9bf57d002cd5f054ca4d40b0b87542bdaba5e05321ae8262c84',
    downloaded_file_path = 'pylint-2.16.2.tar.gz'
)

http_file(
    name = 'python_pytz',
    url = 'https://files.pythonhosted.org/packages/03/3e/dc5c793b62c60d0ca0b7e58f1fdd84d5aaa9f8df23e7589b39cc9ce20a03/pytz-2022.7.1.tar.gz',
    sha256 = '01a0681c4b9684a28304615eba55d1ab31ae00bf68ec157ec3708a8182dbbcd0',
    downloaded_file_path = 'pytz-2022.7.1.tar.gz'
)

http_file(
    name = 'python_requests',
    url = 'https://files.pythonhosted.org/packages/9d/ee/391076f5937f0a8cdf5e53b701ffc91753e87b07d66bae4a09aa671897bf/requests-2.28.2.tar.gz',
    sha256 = '98b1b2782e3c6c4904938b84c0eb932721069dfdb9134313beff7c83c2df24bf',
    downloaded_file_path = 'requests-2.28.2.tar.gz'
)

http_file(
    name = 'python_snowballstemmer',
    url = 'https://files.pythonhosted.org/packages/44/7b/af302bebf22c749c56c9c3e8ae13190b5b5db37a33d9068652e8f73b7089/snowballstemmer-2.2.0.tar.gz',
    sha256 = '09b16deb8547d3412ad7b590689584cd0fe25ec8db3be37788be3810cbf19cb1',
    downloaded_file_path = 'snowballstemmer-2.2.0.tar.gz'
)

http_file(
    name = 'python_sphinx',
    url = 'https://files.pythonhosted.org/packages/db/0b/a0f60c4abd8a69bd5b0d20edde8a8d8d9d4ca825bbd920d328d248fd0290/Sphinx-6.1.3.tar.gz',
    sha256 = '0dac3b698538ffef41716cf97ba26c1c7788dba73ce6f150c1ff5b4720786dd2',
    downloaded_file_path = 'Sphinx-6.1.3.tar.gz'
)

http_file(
    name = 'python_sphinx_csharp',
    url = 'https://files.pythonhosted.org/packages/a1/b4/4fd40fafe1c6ba3ade17e4e26e301691db890db0f4ead43467d8f69d0e3d/sphinx-csharp-0.1.8.tar.gz',
    sha256 = 'b6aaab9057187f3e8a0c83c400d2d16ca21254a0d2f9af1d141d7f1cf7cfaf34',
    downloaded_file_path = 'sphinx-csharp-0.1.8.tar.gz'
)

http_file(
    name = 'python_sphinx_rtd_theme',
    url = 'https://files.pythonhosted.org/packages/35/b4/40faec6790d4b08a6ef878feddc6ad11c3872b75f52273f1418c39f67cd6/sphinx_rtd_theme-1.2.0.tar.gz',
    sha256 = 'a0d8bd1a2ed52e0b338cbe19c4b2eef3c5e7a048769753dac6a9f059c7b641b8',
    downloaded_file_path = 'sphinx_rtd_theme-1.2.0.tar.gz'
)

http_file(
    name = 'python_sphinx_tabs',
    url = 'https://files.pythonhosted.org/packages/aa/9b/a54949728ff067e4d0997c934e97569dbf3bb4e9c0d63ff3377be4cc3831/sphinx-tabs-3.4.1.tar.gz',
    sha256 = 'd2a09f9e8316e400d57503f6df1c78005fdde220e5af589cc79d493159e1b832',
    downloaded_file_path = 'sphinx-tabs-3.4.1.tar.gz'
)

http_file(
    name = 'python_sphinxcontrib_applehelp',
    url = 'https://files.pythonhosted.org/packages/32/df/45e827f4d7e7fcc84e853bcef1d836effd762d63ccb86f43ede4e98b478c/sphinxcontrib-applehelp-1.0.4.tar.gz',
    sha256 = '828f867945bbe39817c210a1abfd1bc4895c8b73fcaade56d45357a348a07d7e',
    downloaded_file_path = 'sphinxcontrib-applehelp-1.0.4.tar.gz'
)

http_file(
    name = 'python_sphinxcontrib_devhelp',
    url = 'https://files.pythonhosted.org/packages/98/33/dc28393f16385f722c893cb55539c641c9aaec8d1bc1c15b69ce0ac2dbb3/sphinxcontrib-devhelp-1.0.2.tar.gz',
    sha256 = 'ff7f1afa7b9642e7060379360a67e9c41e8f3121f2ce9164266f61b9f4b338e4',
    downloaded_file_path = 'sphinxcontrib-devhelp-1.0.2.tar.gz'
)

http_file(
    name = 'python_sphinxcontrib_htmlhelp',
    url = 'https://files.pythonhosted.org/packages/b3/47/64cff68ea3aa450c373301e5bebfbb9fce0a3e70aca245fcadd4af06cd75/sphinxcontrib-htmlhelp-2.0.1.tar.gz',
    sha256 = '0cbdd302815330058422b98a113195c9249825d681e18f11e8b1f78a2f11efff',
    downloaded_file_path = 'sphinxcontrib-htmlhelp-2.0.1.tar.gz'
)

http_file(
    name = 'python_sphinxcontrib_jsmath',
    url = 'https://files.pythonhosted.org/packages/b2/e8/9ed3830aeed71f17c026a07a5097edcf44b692850ef215b161b8ad875729/sphinxcontrib-jsmath-1.0.1.tar.gz',
    sha256 = 'a9925e4a4587247ed2191a22df5f6970656cb8ca2bd6284309578f2153e0c4b8',
    downloaded_file_path = 'sphinxcontrib-jsmath-1.0.1.tar.gz'
)

http_file(
    name = 'python_sphinxcontrib_luadomain',
    url = 'https://files.pythonhosted.org/packages/54/15/eb8f5c1b2d8cbdbc9eb0444a5aa72b564b1640573b5132b1ec1b79efc06d/sphinxcontrib-luadomain-1.1.2.tar.gz',
    sha256 = 'c3286ffdb3157350ca7a345addc3b4a6531008b9d8b2b03ead2d64943b33d141',
    downloaded_file_path = 'sphinxcontrib-luadomain-1.1.2.tar.gz'
)

http_file(
    name = 'python_sphinxcontrib_qthelp',
    url = 'https://files.pythonhosted.org/packages/b1/8e/c4846e59f38a5f2b4a0e3b27af38f2fcf904d4bfd82095bf92de0b114ebd/sphinxcontrib-qthelp-1.0.3.tar.gz',
    sha256 = '4c33767ee058b70dba89a6fc5c1892c0d57a54be67ddd3e7875a18d14cba5a72',
    downloaded_file_path = 'sphinxcontrib-qthelp-1.0.3.tar.gz'
)

http_file(
    name = 'python_sphinxcontrib_serializinghtml',
    url = 'https://files.pythonhosted.org/packages/b5/72/835d6fadb9e5d02304cf39b18f93d227cd93abd3c41ebf58e6853eeb1455/sphinxcontrib-serializinghtml-1.1.5.tar.gz',
    sha256 = 'aa5f6de5dfdf809ef505c4895e51ef5c9eac17d0f287933eb49ec495280b6952',
    downloaded_file_path = 'sphinxcontrib-serializinghtml-1.1.5.tar.gz'
)

http_file(
    name = 'python_sphinxcontrib_spelling',
    url = 'https://files.pythonhosted.org/packages/38/88/d8d0e4ff3087199db984bd03d1d17c413bcdcdde0f5120d3cc0b4c8806b3/sphinxcontrib-spelling-8.0.0.tar.gz',
    sha256 = '199d0a16902ad80c387c2966dc9eb10f565b1fb15ccce17210402db7c2443e5c',
    downloaded_file_path = 'sphinxcontrib-spelling-8.0.0.tar.gz'
)

http_file(
    name = 'python_tomli',
    url = 'https://files.pythonhosted.org/packages/c0/3f/d7af728f075fb08564c5949a9c95e44352e23dee646869fa104a3b2060a3/tomli-2.0.1.tar.gz',
    sha256 = 'de526c12914f0c550d15924c62d72abc48d6fe7364aa87328337a31007fe8a4f',
    downloaded_file_path = 'tomli-2.0.1.tar.gz'
)

http_file(
    name = 'python_tomlkit',
    url = 'https://files.pythonhosted.org/packages/ff/04/58b4c11430ed4b7b8f1723a5e4f20929d59361e9b17f0872d69681fd8ffd/tomlkit-0.11.6.tar.gz',
    sha256 = '71b952e5721688937fb02cf9d354dbcf0785066149d2855e44531ebdd2b65d73',
    downloaded_file_path = 'tomlkit-0.11.6.tar.gz'
)

http_file(
    name = 'python_typing_extensions',
    url = 'https://files.pythonhosted.org/packages/d3/20/06270dac7316220643c32ae61694e451c98f8caf4c8eab3aa80a2bedf0df/typing_extensions-4.5.0.tar.gz',
    sha256 = '5cb5f4a79139d699607b3ef622a1dedafa84e115ab0024e0d9c044a9479ca7cb',
    downloaded_file_path = 'typing_extensions-4.5.0.tar.gz'
)

http_file(
    name = 'python_urllib3',
    url = 'https://files.pythonhosted.org/packages/c5/52/fe421fb7364aa738b3506a2d99e4f3a56e079c0a798e9f4fa5e14c60922f/urllib3-1.26.14.tar.gz',
    sha256 = '076907bf8fd355cde77728471316625a4d2f7e713c125f51953bb5b3eecf4f72',
    downloaded_file_path = 'urllib3-1.26.14.tar.gz'
)

http_file(
    name = 'python_wrapt',
    url = 'https://files.pythonhosted.org/packages/f8/7d/73e4e3cdb2c780e13f9d87dc10488d7566d8fd77f8d68f0e416bfbd144c7/wrapt-1.15.0.tar.gz',
    sha256 = 'd06730c6aed78cee4126234cf2d071e01b44b915e725a6cb439a879ec9754a3a',
    downloaded_file_path = 'wrapt-1.15.0.tar.gz'
)

http_file(
    name = 'python_websocket_client',
    url = 'https://files.pythonhosted.org/packages/8b/94/696484b0c13234c91b316bc3d82d432f9b589a9ef09d016875a31c670b76/websocket-client-1.5.1.tar.gz',
    sha256 = '3f09e6d8230892547132177f575a4e3e73cfdf06526e20cc02aa1c3b47184d40',
    downloaded_file_path = 'websocket-client-1.5.1.tar.gz'
)

http_file(
    name = 'module_manager',
    url = 'https://ksp.sarbian.com/jenkins/job/ModuleManager/162/artifact/ModuleManager.4.2.2.dll',
    sha256 = 'c7c3f7c7193dbf9477422720d338b6d1977149d9b1e8f6d46acb89af18f40026'
)
