workspace(name = "krpc")

new_http_archive(
    name = 'protoc_linux_x86_32',
    build_file_content = "exports_files(['bin/protoc'])",
    url = 'https://github.com/google/protobuf/releases/download/v3.4.0/protoc-3.4.0-linux-x86_32.zip',
    sha256 = '6cacb05eb9aa7690b85db7fc3c4c9124751c4ecfb4f20d2e6f61eda2b1b789d3'
)

new_http_archive(
    name = 'protoc_linux_x86_64',
    build_file_content = "exports_files(['bin/protoc'])",
    url = 'https://github.com/google/protobuf/releases/download/v3.4.0/protoc-3.4.0-linux-x86_64.zip',
    sha256 = 'e4b51de1b75813e62d6ecdde582efa798586e09b5beaebfb866ae7c9eaadace4'
)

new_http_archive(
    name = 'protoc_osx_x86_32',
    build_file_content = "exports_files(['bin/protoc'])",
    url = 'https://github.com/google/protobuf/releases/download/v3.4.0/protoc-3.4.0-osx-x86_32.zip',
    sha256 = '8601d7c7afb727ca31c42597a7863a7071ebdf59d3d35b31320379eaa55e23f9'
)

new_http_archive(
    name = 'protoc_win32',
    build_file_content = "exports_files(['bin/protoc.exe'])",
    url = 'https://github.com/google/protobuf/releases/download/v3.4.0/protoc-3.4.0-win32.zip',
    sha256 = '7d8a42ae38fec3ca09833ea16f1d83a049f0580929c3b057042e006105ad864b'
)

http_file(
    name = 'csharp_nuget',
    url = 'https://dist.nuget.org/win-x86-commandline/v3.4.4/NuGet.exe',
    sha256 = 'c12d583dd1b5447ac905a334262e02718f641fca3877d0b6117fe44674072a27'
)

new_http_archive(
    name = 'csharp_protobuf',
    build_file_content = "exports_files(['lib/net45/Google.Protobuf.dll'])",
    url = 'https://www.nuget.org/api/v2/package/Google.Protobuf/3.4.1',
    type = 'zip',
    sha256 = '3506470bf07fa10dc53bc50c9275d6018a391eda549ef62514f1be12f4ecf2e6'
)

http_file(
    name = 'csharp_protobuf_net35',
    url = 'https://s3.amazonaws.com/krpc/lib/protobuf-3.4.0-net35/Google.Protobuf.dll',
    sha256 = '496bf64ad9887c539cf7cc070d7e42edd4c8a8534286179431971ffa62ec3e4c'
)

http_file(
    name = 'csharp_krpc_io_ports',
    url = 'https://github.com/krpc/krpc-io-ports/releases/download/v1.0.0/KRPC.IO.Ports.dll',
    sha256 = '558b0c1649fbc44b518d9de8957fe30e7c9c42d73c62d63d165f6f136fab3ec5'
)

new_http_archive(
    name = 'csharp_nunit',
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
    srcs = glob(['bin/framework/*.dll']),
    visibility = ['//visibility:public'],
)
""",
    url = 'https://github.com/nunit/nunitv2/releases/download/2.6.4/NUnit-2.6.4.zip',
    sha256 = '1bd925514f31e7729ccde40a38a512c2accd86895f93465f3dfe6d0b593d7170',
    strip_prefix = 'NUnit-2.6.4'
)

new_http_archive(
    name = 'csharp_moq',
    build_file_content = "exports_files(['lib/net40/Moq.dll'])",
    url = 'http://www.nuget.org/api/v2/package/Moq/4.2.1510.2205',
    type = 'zip',
    sha256 = '1f4978e0a3f5b8d82b41635eff8201e5e7021b1fc1aceae4d9caeb79506f3804'
)

new_http_archive(
    name = 'csharp_json',
    build_file_content = "exports_files(['lib/net35/Newtonsoft.Json.dll', 'lib/net45/Newtonsoft.Json.dll'])",
    url = 'https://www.nuget.org/api/v2/package/Newtonsoft.Json/9.0.1',
    type = 'zip',
    sha256 = '23405c5a3814347fb952b74dec0d836b5b63832c02552e17e0b10f88ab555ee1'
)

new_http_archive(
    name = 'csharp_options',
    build_file_content = "exports_files(['lib/NDesk.Options.dll'])",
    url = 'https://www.nuget.org/api/v2/package/NDesk.Options/0.2.1',
    type = 'zip',
    sha256 = 'f7cad7f76b9a738930496310ea47888529fbfd0a39896bdfd3cfd17fd385f53b'
)

http_archive(
    name = 'cpp_protobuf',
    url = 'https://github.com/google/protobuf/releases/download/v3.4.1/protobuf-cpp-3.4.1.tar.gz',
    strip_prefix = 'protobuf-3.4.1',
    sha256 = '2bb34b4a8211a30d12ef29fd8660995023d119c99fbab2e5fe46f17528c9cc78'
)

new_http_archive(
    name = 'cpp_asio',
    build_file_content = """
cc_library(
    name = 'asio',
    hdrs = glob(['include/*', 'include/**/*']),
    includes = ['include'],
    visibility = ['//visibility:public']
)
""",
    url = 'https://s3.amazonaws.com/krpc/lib/asio/asio-1.10.6.tar.gz',
    strip_prefix = 'asio-1.10.6',
    sha256 = '70345ca9e372a119c361be5e7f846643ee90997da8f88ec73f7491db96e24bbe'
)

new_http_archive(
    name = 'cpp_googletest',
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
""",
    url = 'https://github.com/google/googletest/archive/release-1.8.0.zip',
    strip_prefix = 'googletest-release-1.8.0',
    sha256 = 'f3ed3b58511efd272eb074a3a6d6fb79d7c2e6a0e374323d1e6bcbcc1ef141bf'
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
    artifact = 'com.google.protobuf:protobuf-java:3.4.0',
    sha1 = 'b32aba0cbe737a4ca953f71688725972e3ee927c'
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

new_http_archive(
    name = 'protoc_lua',
    build_file_content = """
filegroup(
    name = 'plugin',
    srcs = [
        'protoc-plugin/protoc-gen-lua',
        'protoc-plugin/plugin_pb2.py'
    ],
    visibility = ['//visibility:public']
)""",
    url = 'https://github.com/djungelorm/protobuf-lua/archive/v1.1.1.tar.gz',
    sha256 = 'bccdd9c65970c42fd29b87084db83777cad75780a67c5107b68f96603b5788a8',
    strip_prefix = 'protobuf-lua-1.1.1'
)

http_file(
    name = 'lua_protobuf',
    url = 'https://github.com/djungelorm/protobuf-lua/releases/download/v1.1.1/protobuf-1.1.1-0.src.rock',
    sha256 = 'eec6a738cd6acbf0ae695c95bac6f89036cd37f23335272cb3717b01834f1dbb'
)

http_file(
    name = 'lua_luasocket',
    url = 'https://luarocks.org/manifests/luarocks/luasocket-3.0rc1-2.src.rock',
    sha256 = '3882f2a1e1c6145ceb43ead385b861b97fa2f8d487e8669ec5b747406ab251c7'
)

http_file(
    name = 'lua_luafilesystem',
    url = 'https://luarocks.org/manifests/hisham/luafilesystem-1.6.3-2.src.rock',
    sha256 = '872914421d4585f37ce72be40003e2bfdd22e017b55e87b0f89c467cc0df30e0'
)

http_file(
    name = 'lua_penlight',
    url = 'http://luarocks.org/repositories/rocks/penlight-1.3.1-1.src.rock',
    sha256 = '13c6fcc5058a998505ddc4b52496f591d7d37ed2efa9a46a2c39db6183f38783'
)

http_file(
    name = 'lua_luaunit',
    url = 'https://luarocks.org/manifests/bluebird75/luaunit-3.2.1-1.src.rock',
    sha256 = '7ae20f3b68e77e3be52fc95c147eccfaef33206a7985320061fb9352d8565741'
)

http_file(
    name = 'python_alabaster',
    url = 'https://pypi.python.org/packages/d0/a5/e3a9ad3ee86aceeff71908ae562580643b955ea1b1d4f08ed6f7e8396bd7/alabaster-0.7.10.tar.gz',
    sha256 = '37cdcb9e9954ed60912ebc1ca12a9d12178c26637abdf124e3cde2341c257fe0'
)

http_file(
    name = 'python_astroid',
    url = 'https://pypi.python.org/packages/d7/b7/112288f75293d6f12b1e41bac1e822fd0f29b0f88e2c4378cdd295b9d838/astroid-1.5.3.tar.gz',
    sha256 = '492c2a2044adbf6a84a671b7522e9295ad2f6a7c781b899014308db25312dd35'
)

http_file(
    name = 'python_babel',
    url = 'https://pypi.python.org/packages/5a/22/63f1dbb8514bb7e0d0c8a85cc9b14506599a075e231985f98afd70430e1f/Babel-2.5.1.tar.gz',
    sha256 = '6007daf714d0cd5524bbe436e2d42b3c20e68da66289559341e48d2cd6d25811'
)

http_file(
    name = 'python_backports_functools_lru_cache',
    url = 'https://pypi.python.org/packages/02/0b/91573feec859f794689fa46a62240526f4f1db829271ac2d98cf04a8efa2/backports.functools_lru_cache-1.4-py2.py3-none-any.whl',
    sha256 = '4ba998e881f285c1d1b73f5b6e3766539b4e162320f9589334400c5ddc35198c'
)

http_file(
    name = 'python_beautifulsoup4',
    url = 'https://pypi.python.org/packages/fa/8d/1d14391fdaed5abada4e0f63543fef49b8331a34ca60c88bd521bcf7f782/beautifulsoup4-4.6.0.tar.gz',
    sha256 = '808b6ac932dccb0a4126558f7dfdcf41710dd44a4ef497a0bb59a77f9f078e89'
)

http_file(
    name = 'python_certifi',
    url = 'https://pypi.python.org/packages/20/d0/3f7a84b0c5b89e94abbd073a5f00c7176089f526edb056686751d5064cbd/certifi-2017.7.27.1.tar.gz',
    sha256 = '40523d2efb60523e113b44602298f0960e900388cf3bb6043f645cf57ea9e3f5'
)

http_file(
    name = 'python_chardet',
    url = 'https://pypi.python.org/packages/fc/bb/a5768c230f9ddb03acc9ef3f0d4a3cf93462473795d18e9535498c8f929d/chardet-3.0.4.tar.gz',
    sha256 = '84ab92ed1c4d4f16916e05906b6b75a6c0fb5db821cc65e70cbd64a3e2a5eaae'
)

http_file(
    name = 'python_configparser',
    url = 'https://pypi.python.org/packages/7c/69/c2ce7e91c89dc073eb1aa74c0621c3eefbffe8216b3f9af9d3885265c01c/configparser-3.5.0.tar.gz',
    sha256 = '5308b47021bc2340965c371f0f058cc6971a04502638d4244225c49d80db273a'
)

http_file(
    name = 'python_cpplint',
    url = 'https://pypi.python.org/packages/95/42/27a16ef7fc609aba82bec923e2d29a1fa163bc95a267eaf1acc780e949fc/cpplint-1.3.0.tar.gz',
    sha256 = '6876139c3944c6dc84cc9095b6c4be3c5397b534b0c00230ba59c4b893936719'
)

http_file(
    name = 'python_docutils',
    url = 'https://pypi.python.org/packages/84/f4/5771e41fdf52aabebbadecc9381d11dea0fa34e4759b4071244fa094804c/docutils-0.14.tar.gz',
    sha256 = '51e64ef2ebfb29cae1faa133b3710143496eca21c530f3f71424d77687764274'
)

http_file(
    name = 'python_enum34',
    url = 'https://pypi.python.org/packages/bf/3e/31d502c25302814a7c2f1d3959d2a3b3f78e509002ba91aea64993936876/enum34-1.1.6.tar.gz',
    sha256 = '8ad8c4783bf61ded74527bffb48ed9b54166685e4230386a9ed9b1279e2df5b1'
)

http_file(
    name = 'python_future',
    url = 'https://pypi.python.org/packages/00/2b/8d082ddfed935f3608cc61140df6dcbf0edea1bc3ab52fb6c29ae3e81e85/future-0.16.0.tar.gz',
    sha256 = 'e39ced1ab767b5936646cedba8bcce582398233d6a627067d4c6a454c90cfedb'
)

http_file(
    name = 'python_idna',
    url = 'https://pypi.python.org/packages/f4/bd/0467d62790828c23c47fc1dfa1b1f052b24efdf5290f071c7a91d0d82fd3/idna-2.6.tar.gz',
    sha256 = '2c6a5de3089009e3da7c5dde64a141dbc8551d5b7f6cf4ed7c2568d0cc520a8f'
)

http_file(
    name = 'python_imagesize',
    url = 'https://pypi.python.org/packages/53/72/6c6f1e787d9cab2cc733cf042f125abec07209a58308831c9f292504e826/imagesize-0.7.1.tar.gz',
    sha256 = '0ab2c62b87987e3252f89d30b7cedbec12a01af9274af9ffa48108f2c13c6062'
)

http_file(
    name = 'python_isort',
    url = 'https://pypi.python.org/packages/4d/d5/7c8657126a43bcd3b0173e880407f48be4ac91b4957b51303eab744824cf/isort-4.2.15.tar.gz',
    sha256 = '79f46172d3a4e2e53e7016e663cc7a8b538bec525c36675fcfd2767df30b3983'
)

http_file(
    name = 'python_javalang',
    url = 'https://pypi.python.org/packages/39/51/fc4d3cdcf8f46509887d8771ce18ca6cfafd1d02eb429d69da95866a0b5e/javalang-0.11.0.tar.gz',
    sha256 = '3fcab8c0d4a1c51512bc7de1f4aaf9de8fb582833746b572478da6c0ac318a0b'
)

http_file(
    name = 'python_javasphinx',
    url = 'https://pypi.python.org/packages/34/ea/08bc47c6aafcf5ebf06784c0ec60aa0e8bd130bc96f923755f061471a3c8/javasphinx-0.9.15.tar.gz',
    sha256 = '165f787172a99ceaedd0230a69b44de19cebd3a103e970b89bf667210ae6b65b'
)

http_file(
    name = 'python_jinja2',
    url = 'https://pypi.python.org/packages/90/61/f820ff0076a2599dd39406dcb858ecb239438c02ce706c8e91131ab9c7f1/Jinja2-2.9.6.tar.gz',
    sha256 = 'ddaa01a212cd6d641401cb01b605f4a4d9f37bfc93043d7f760ec70fb99ff9ff'
)

http_file(
    name = 'python_lazy_object_proxy',
    url = 'https://pypi.python.org/packages/55/08/23c0753599bdec1aec273e322f277c4e875150325f565017f6280549f554/lazy-object-proxy-1.3.1.tar.gz',
    sha256 = 'eb91be369f945f10d3a49f5f9be8b3d0b93a4c2be8f8a5b83b0571b8123e0a7a'
)

http_file(
    name = 'python_lxml',
    url = 'https://pypi.python.org/packages/07/76/9f14811d3fb91ed7973a798ded15eda416070bbcb1aadc6a5af9d691d993/lxml-4.0.0.tar.gz',
    sha256 = 'f7bc9f702500e205b1560d620f14015fec76dcd6f9e889a946a2ddcc3c344fd0'
)

http_file(
    name = 'python_markupsafe',
    url = 'https://pypi.python.org/packages/4d/de/32d741db316d8fdb7680822dd37001ef7a448255de9699ab4bfcbdf4172b/MarkupSafe-1.0.tar.gz',
    sha256 = 'a6be69091dac236ea9c6bc7d012beab42010fa914c459791d627dad4910eb665'
)

http_file(
    name = 'python_mccabe',
    url = 'https://pypi.python.org/packages/87/89/479dc97e18549e21354893e4ee4ef36db1d237534982482c3681ee6e7b57/mccabe-0.6.1-py2.py3-none-any.whl',
    sha256 = 'ab8a6258860da4b6677da4bd2fe5dc2c659cff31b3ee4f7f5d64e79735b80d42'
)

http_file(
    name = 'python_pbr',
    url = 'https://pypi.python.org/packages/d5/d6/f2bf137d71e4f213b575faa9eb426a8775732432edb67588a8ee836ecb80/pbr-3.1.1.tar.gz',
    sha256 = '05f61c71aaefc02d8e37c0a3eeb9815ff526ea28b3b76324769e6158d7f95be1'
)

http_file(
    name = 'python_pep8',
    url = 'https://pypi.python.org/packages/3e/b5/1f717b85fbf5d43d81e3c603a7a2f64c9f1dabc69a1e7745bd394cc06404/pep8-1.7.0.tar.gz',
    sha256 = 'a113d5f5ad7a7abacef9df5ec3f2af23a20a28005921577b15dd584d099d5900'
)

http_file(
    name = 'python_protobuf',
    url = 'https://pypi.python.org/packages/89/45/3214bb758646a1a30459ca0f5b8f8164d6893f24725c58b632e663565f44/protobuf-3.4.0.tar.gz',
    sha256 = 'ef02609ef445987976a3a26bff77119c518e0915c96661c3a3b17856d0ef6374'
)

http_file(
    name = 'python_pyenchant',
    url = 'https://pypi.python.org/packages/f3/00/c04496277b1e681d0f500baf7ac8f3c7f1d21b9ea97ed951ed4ac5635fda/pyenchant-1.6.11.tar.gz',
    sha256 = '27d3307aa3d3cd413c20eb1fd977446c133cae47d7329d8f846cd3d8ddae6278'
)

http_file(
    name = 'python_pygments',
    url = 'https://pypi.python.org/packages/71/2a/2e4e77803a8bd6408a2903340ac498cb0a2181811af7c9ec92cb70b0308a/Pygments-2.2.0.tar.gz',
    sha256 = 'dbae1046def0efb574852fab9e90209b23f556367b5a320c0bcb871c77c3e8cc'
)

http_file(
    name = 'python_pylint',
    url = 'https://pypi.python.org/packages/87/8a/07782ece0b9db20341393f9913fb5368f9e7e4553f17c0bc91eda633f942/pylint-1.7.4-py2.py3-none-any.whl',
    sha256 = '948679535a28afc54afb9210dabc6973305409042ece8e5768ca1409910c1ed8'
)

http_file(
    name = 'python_pytz',
    url = 'https://pypi.python.org/packages/a4/09/c47e57fc9c7062b4e83b075d418800d322caa87ec0ac21e6308bd3a2d519/pytz-2017.2.zip',
    sha256 = 'f5c056e8f62d45ba8215e5cb8f50dfccb198b4b9fbea8500674f3443e4689589'
)

http_file(
    name = 'python_requests',
    url = 'https://pypi.python.org/packages/b0/e1/eab4fc3752e3d240468a8c0b284607899d2fbfb236a56b7377a329aa8d09/requests-2.18.4.tar.gz',
    sha256 = '9c443e7324ba5b85070c4a818ade28bfabedf16ea10206da1132edaa6dda237e'
)

http_file(
    name = 'python_serialio',
    url = 'https://pypi.python.org/packages/cc/74/11b04703ec416717b247d789103277269d567db575d2fd88f25d9767fe3d/pyserial-3.4.tar.gz',
    sha256 = 'e17c4687fddd6d70a6604ac0ad25e33324cec71b5137267dd5c45e103c4b288a'
)

http_file(
    name = 'python_setuptools',
    url = 'https://pypi.python.org/packages/a4/c8/9a7a47f683d54d83f648d37c3e180317f80dc126a304c45dc6663246233a/setuptools-36.5.0.zip',
    sha256 = 'ce2007c1cea3359870b80657d634253a0765b0c7dc5a988d77ba803fc86f2c64'
)

http_file(
    name = 'python_setuptools_git',
    url = 'https://pypi.python.org/packages/d9/c5/396c2c06cc89d4ce2d8ccf1d7e6cf31b33d4466a7c65a67a992adb3c6f29/setuptools-git-1.2.tar.gz',
    sha256 = 'ff64136da01aabba76ae88b050e7197918d8b2139ccbf6144e14d472b9c40445'
)

http_file(
    name = 'python_singledispatch',
    url = 'https://pypi.python.org/packages/d9/e9/513ad8dc17210db12cb14f2d4d190d618fb87dd38814203ea71c87ba5b68/singledispatch-3.4.0.3.tar.gz',
    sha256 = '5b06af87df13818d14f08a028e42f566640aef80805c3b50c5056b086e3c2b9c'
)

http_file(
    name = 'python_six',
    url = 'https://pypi.python.org/packages/16/d8/bc6316cf98419719bd59c91742194c111b6f2e85abac88e496adefaf7afe/six-1.11.0.tar.gz',
    sha256 = '70e8a77beed4562e7f14fe23a786b54f6296e34344c23bc42f07b15018ff98e9'
)

http_file(
    name = 'python_snowballstemmer',
    url = 'https://pypi.python.org/packages/20/6b/d2a7cb176d4d664d94a6debf52cd8dbae1f7203c8e42426daa077051d59c/snowballstemmer-1.2.1.tar.gz',
    sha256 = '919f26a68b2c17a7634da993d91339e288964f93c274f1343e3bbbe2096e1128'
)

http_file(
    name = 'python_sphinx',
    url = 'https://pypi.python.org/packages/90/84/850bda5df345bbccaf21d389d360c07b8499b47bc136cdf53e96d840a55f/Sphinx-1.6.4.tar.gz',
    sha256 = 'f101efd87fbffed8d8aca6ef307fec57693334f39d32efcbc2fc96ed129f4a3e'
)

http_file(
    name = 'python_sphinx_csharp',
    url = 'https://pypi.python.org/packages/bc/c2/32c2a2218e8eb98ef796ceeebccd55c93d3ef7e3be131ed799c71ef47f88/sphinx-csharp-0.1.5.tar.gz',
    sha256 = 'a90887627c9218d5ee48f1603b6d00209d6d518dbdea6b26c2d4d484f9a3b16c'
)

http_file(
    name = 'python_sphinx_lua',
    url = 'https://github.com/djungelorm/sphinx-lua/releases/download/0.1.4/sphinx-lua-0.1.4.tar.gz',
    sha256 = 'ebfd6a228fe99f2349d07c9cb0a6f411132b1726ad203238feaa44c67b2aad95'
)

http_file(
    name = 'python_sphinx_tabs',
    url = 'https://pypi.python.org/packages/fa/97/9d5d9b345423eea201c6d59bf1359ac6098ee702fc2eca95ac9d7e113a65/sphinx-tabs-1.1.5.tar.gz',
    sha256 = '06ca23c1fb5590398c46f0c4d06d9e3c01bab129bbbad6e50c41a01704b07698'
)

http_file(
    name = 'python_sphinx_rtd_theme',
    url = 'https://pypi.python.org/packages/59/e4/9e3a74a3271e6734911d3f549e8439db53b8ac29adf10c8f698e6c86246b/sphinx_rtd_theme-0.2.5b1.tar.gz',
    sha256 = 'd99513e7f2f8b9da8fdc189ad83df926b83d7fb15ad7ed07f24665d1f29d38da'
)

http_file(
    name = 'python_sphinxcontrib_spelling',
    url = 'https://pypi.python.org/packages/d8/6c/8dfaebcbc3f82a06bfeea0c98678b5db177ec35abdcfcf4e702e901aa67c/sphinxcontrib-spelling-2.3.0.tar.gz',
    sha256 = '008ec060f312367222992824abed00124cce0dd31c375a89b2053010df0e3be8'
)

http_file(
    name = 'python_sphinxcontrib_websupport',
    url = 'https://pypi.python.org/packages/c5/6b/f0630436b931ad4f8331a9399ca18a7d447f0fcc0c7178fb56b1aee68d01/sphinxcontrib-websupport-1.0.1.tar.gz',
    sha256 = '7a85961326aa3a400cd4ad3c816d70ed6f7c740acd7ce5d78cd0a67825072eb9'
)

http_file(
    name = 'python_typing',
    url = 'https://pypi.python.org/packages/ca/38/16ba8d542e609997fdcd0214628421c971f8c395084085354b11ff4ac9c3/typing-3.6.2.tar.gz',
    sha256 = 'd514bd84b284dd3e844f0305ac07511f097e325171f6cc4a20878d11ad771849'
)

http_file(
    name = 'python_urllib3',
    url = 'https://pypi.python.org/packages/ee/11/7c59620aceedcc1ef65e156cc5ce5a24ef87be4107c2b74458464e437a5d/urllib3-1.22.tar.gz',
    sha256 = 'cc44da8e1145637334317feebd728bd869a35285b93cbb4cca2577da7e62db4f'
)

http_file(
    name = 'python_wrapt',
    url = 'https://pypi.python.org/packages/a0/47/66897906448185fcb77fc3c2b1bc20ed0ecca81a0f2f88eda3fc5a34fc3d/wrapt-1.10.11.tar.gz',
    sha256 = 'd4d560d479f2c21e1b5443bbd15fe7ec4b37fe7e53d335d3b9b0a7b1226fe3c6'
)

http_file(
    name = 'python_websocket_client',
    url = 'https://pypi.python.org/packages/06/19/f00725a8aee30163a7f257092e356388443034877c101757c1466e591bf8/websocket_client-0.44.0.tar.gz',
    sha256 = '15f585566e2ea7459136a632b9785aa081093064391878a448c382415e948d72'
)
