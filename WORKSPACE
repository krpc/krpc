workspace(name = "krpc")

new_http_archive(
    name = 'protoc_linux_x86_32',
    build_file = 'tools/build/protobuf/protoc_linux_x86_32.BUILD',
    url = 'https://github.com/google/protobuf/releases/download/v3.1.0/protoc-3.1.0-linux-x86_32.zip',
    sha256 = 'ed83ac3226b7d4334054c712a911669351b0a65d88cff04f32d5251b3c1e1bc5'
)

new_http_archive(
    name = 'protoc_linux_x86_64',
    build_file = 'tools/build/protobuf/protoc_linux_x86_64.BUILD',
    url = 'https://github.com/google/protobuf/releases/download/v3.1.0/protoc-3.1.0-linux-x86_64.zip',
    sha256 = '7c98f9e8a3d77e49a072861b7a9b18ffb22c98e37d2a80650264661bfaad5b3a'
)

new_http_archive(
    name = 'protoc_osx_x86_32',
    build_file = 'tools/build/protobuf/protoc_osx_x86_32.BUILD',
    url = 'https://github.com/google/protobuf/releases/download/v3.1.0/protoc-3.1.0-osx-x86_32.zip',
    sha256 = '8291317f41253b9d8182d272d739b06febf25acd1d9787996a92fe5cae936898'
)

new_http_archive(
    name = 'protoc_win32',
    build_file = 'tools/build/protobuf/protoc_win32.BUILD',
    url = 'https://github.com/google/protobuf/releases/download/v3.1.0/protoc-3.1.0-win32.zip',
    sha256 = 'e46b3b7c5c99361bbdd1bbda93c67e5cbf2873b7098482d85ff8e587ff596b23'
)

http_file(
    name = 'csharp_nuget',
    url = 'https://dist.nuget.org/win-x86-commandline/v3.4.4/NuGet.exe',
    sha256 = 'c12d583dd1b5447ac905a334262e02718f641fca3877d0b6117fe44674072a27'
)

new_http_archive(
    name = 'csharp_protobuf',
    build_file = 'tools/build/csharp_protobuf.BUILD',
    url = 'https://www.nuget.org/api/v2/package/Google.Protobuf/3.1.0',
    type = 'zip',
    sha256 = '032057472194471ab61cc6e8fd9046f2e243338a1194d9cfa5c0e2aaaad42a0d'
)

http_file(
    name = 'csharp_protobuf_net35',
    url = 'https://github.com/djungelorm/protobuf/releases/download/v3.1.0-net35/Google.Protobuf.dll',
    sha256 = 'a2b8e37fcf7e27218073a7a21db456bcf48e9ec44473d6c1a5b4b2e32148044b'
)

new_http_archive(
    name = 'csharp_nunit',
    build_file = 'tools/build/csharp_nunit.BUILD',
    url = 'https://github.com/nunit/nunitv2/releases/download/2.6.4/NUnit-2.6.4.zip',
    sha256 = '1bd925514f31e7729ccde40a38a512c2accd86895f93465f3dfe6d0b593d7170',
    strip_prefix = 'NUnit-2.6.4'
)

new_http_archive(
    name = 'csharp_moq',
    build_file = 'tools/build/csharp_moq.BUILD',
    url = 'http://www.nuget.org/api/v2/package/Moq/4.2.1510.2205',
    type = 'zip',
    sha256 = '1f4978e0a3f5b8d82b41635eff8201e5e7021b1fc1aceae4d9caeb79506f3804'
)

new_http_archive(
    name = 'csharp_json',
    build_file = 'tools/build/csharp_json.BUILD',
    url = 'https://www.nuget.org/api/v2/package/Newtonsoft.Json/9.0.1',
    type = 'zip',
    sha256 = '23405c5a3814347fb952b74dec0d836b5b63832c02552e17e0b10f88ab555ee1'
)

new_http_archive(
    name = 'csharp_options',
    build_file = 'tools/build/csharp_options.BUILD',
    url = 'https://www.nuget.org/api/v2/package/NDesk.Options/0.2.1',
    type = 'zip',
    sha256 = 'f7cad7f76b9a738930496310ea47888529fbfd0a39896bdfd3cfd17fd385f53b'
)

http_archive(
    name = 'cpp_protobuf',
    url = 'https://github.com/google/protobuf/releases/download/v3.1.0/protobuf-cpp-3.1.0.tar.gz',
    strip_prefix = 'protobuf-3.1.0',
    sha256 = '51ceea9957c875bdedeb1f64396b5b0f3864fe830eed6a2d9c066448373ea2d6'
)

new_http_archive(
    name = 'cpp_asio',
    build_file = 'tools/build/cpp_asio.BUILD',
    url = 'http://downloads.sourceforge.net/project/asio/asio/1.10.6%20%28Stable%29/asio-1.10.6.tar.gz',
    strip_prefix = 'asio-1.10.6',
    sha256 = '70345ca9e372a119c361be5e7f846643ee90997da8f88ec73f7491db96e24bbe'
)

new_http_archive(
    name = 'cpp_googletest',
    build_file = 'tools/build/cpp_googletest.BUILD',
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

http_file(
    name = 'java_protobuf',
    url = 'https://repo1.maven.org/maven2/com/google/protobuf/protobuf-java/3.1.0/protobuf-java-3.1.0.jar',
    sha256 = '8d7ec605ca105747653e002bfe67bddba90ab964da697aaa5daa1060923585db'
)

http_file(
    name = 'java_junit',
    url = 'http://central.maven.org/maven2/junit/junit/4.12/junit-4.12.jar',
    sha256 = '59721f0805e223d84b90677887d9ff567dc534d7c502ca903c0c2b17f05c116a'
)

http_file(
    name = 'java_hamcrest',
    url = 'http://central.maven.org/maven2/org/hamcrest/hamcrest-core/1.3/hamcrest-core-1.3.jar',
    sha256 = '66fdef91e9739348df7a096aa384a5685f4e875584cce89386a7a47251c4d8e9'
)

http_file(
    name = 'java_checkstyle',
    url = 'https://repo1.maven.org/maven2/com/puppycrawl/tools/checkstyle/7.1.2/checkstyle-7.1.2.jar',
    sha256 = 'a3feec7285bda388a227da3bd19bffccc5dba8935c0a89abab82411dfb4f038c'
)

new_http_archive(
    name = 'java_apache_commons_beanutils',
    build_file = 'tools/build/java_apache_commons_beanutils.BUILD',
    url = 'http://mirror.olnevhost.net/pub/apache/commons/beanutils/binaries/commons-beanutils-1.9.3-bin.zip',
    sha256 = 'c0186fc504970019654466882e7daa64650cb53f7f303bd2546afb87c047049a',
    strip_prefix = 'commons-beanutils-1.9.3'
)

new_http_archive(
    name = 'java_apache_commons_cli',
    build_file = 'tools/build/java_apache_commons_cli.BUILD',
    url = 'http://mirror.olnevhost.net/pub/apache/commons/cli/binaries/commons-cli-1.3.1-bin.zip',
    sha256 = '294437b1958b49dc171104cfff6ab90a85fdea473679304ee860a2b3b486f384',
    strip_prefix = 'commons-cli-1.3.1'
)

new_http_archive(
    name = 'java_apache_commons_collections',
    build_file = 'tools/build/java_apache_commons_collections.BUILD',
    url = 'http://mirror.olnevhost.net/pub/apache/commons/collections/binaries/commons-collections-3.2.2-bin.zip',
    sha256 = '45c1981cab4a831336bba38903aaa184856b303dfba640083e9103d61d3507b6',
    strip_prefix = 'commons-collections-3.2.2'
)

new_http_archive(
    name = 'java_apache_commons_logging',
    build_file = 'tools/build/java_apache_commons_logging.BUILD',
    url = 'http://mirror.olnevhost.net/pub/apache/commons/logging/binaries/commons-logging-1.2-bin.zip',
    sha256 = '8a5bbf1bc8916a1f99ee7584be494bd9ec069025a345a0f0c78eea7407e395ca',
    strip_prefix = 'commons-logging-1.2'
)

http_file(
    name = 'java_antlr2',
    url = 'http://central.maven.org/maven2/antlr/antlr/2.7.7/antlr-2.7.7.jar',
    sha256 = '88fbda4b912596b9f56e8e12e580cc954bacfb51776ecfddd3e18fc1cf56dc4c'
)

http_file(
    name = 'java_antlr4_runtime',
    url = 'http://www.antlr.org/download/antlr-runtime-4.5.3.jar',
    sha256 = '93bca08ec995caeaaf60bdf80035a0be8507fcdabd3c2618fd8c5aab4444a752'
)

http_file(
    name = 'java_guava',
    url = 'http://central.maven.org/maven2/com/google/guava/guava/19.0/guava-19.0.jar',
    sha256 = '58d4cc2e05ebb012bbac568b032f75623be1cb6fb096f3c60c72a86f7f057de4'
)

http_file(
    name = 'java_javatuples',
    url = 'http://central.maven.org/maven2/org/javatuples/javatuples/1.2/javatuples-1.2.jar',
    sha256 = '2eda5b19d9820e1cc2f69fcd01639a715a673c11f8507e3d1ed593cf765d5e0a'
)

new_http_archive(
    name = 'protoc_lua',
    build_file = 'tools/build/protobuf/protoc_lua.BUILD',
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
    sha256 = ''
)

http_file(
    name = 'python_protobuf',
    url = 'https://pypi.python.org/packages/a5/bb/11821bdc46cb9aad8e18618715e5e93eef44abb642ed862c4b080c474183/protobuf-3.1.0.post1-py2.py3-none-any.whl',
    sha256 = '42315e73409eaefdcc11e216695ff21f87dc483ad0595c57999baddf7f841180'
)

http_file(
    name = 'python_jinja2',
    url = 'https://pypi.python.org/packages/source/J/Jinja2/Jinja2-2.8.tar.gz',
    sha256 = 'bc1ff2ff88dbfacefde4ddde471d1417d3b304e8df103a7a9437d47269201bf4'
)

http_file(
    name = 'python_sphinx',
    url = 'https://pypi.python.org/packages/source/S/Sphinx/Sphinx-1.3.5.tar.gz',
    sha256 = 'b7d133bb4990d010a2ad934c319b52d8a2156cb0491484f5e2a558619bc9ae04'
)

http_file(
    name = 'python_sphinx_rtd_theme',
    url = 'https://pypi.python.org/packages/source/s/sphinx_rtd_theme/sphinx_rtd_theme-0.1.9.tar.gz',
    sha256 = '273846f8aacac32bf9542365a593b495b68d8035c2e382c9ccedcac387c9a0a1'
)

http_file(
    name = 'python_sphinxcontrib_spelling',
    url = 'https://pypi.python.org/packages/c6/3e/7e90a4bbecbf973d453a3dbf4d3d61079d438e242f39b14bb0913865c089/sphinxcontrib-spelling-2.2.0.tar.gz',
    sha256='7fb44eef674d7272c1db8fc89b6976b3330529f1ea25b981c6704f3273f50eb8'
)

http_file(
    name = 'python_sphinx_lua',
    url = 'https://github.com/djungelorm/sphinx-lua/releases/download/0.1.2/sphinx-lua-0.1.2.tar.gz',
    sha256 = '6907a309d6d71046222edde1e8cec683b875d7adc478edb97aeb2570c36b145f'
)

http_file(
    name = 'python_sphinx_csharp',
    url = 'https://pypi.python.org/packages/47/f1/3f3273ff3b537f3ec036d3ca7acc606213451040b0652c5eba59cdd12225/sphinx-csharp-0.1.3.tar.gz',
    sha256 = '5547f2241b9b52df24db793fda850f504eadee08c03969d5471cbf2762bf29b4'
)

http_file(
    name = 'python_sphinx_java',
    url = 'https://pypi.python.org/packages/source/j/javasphinx/javasphinx-0.9.13.tar.gz',
    sha256 = '49de72c304271c7e1efb3e12e5a8f5535e1d485797a78e2e27bd402d0f92bd27'
)

http_file(
    name = 'python_sphinx_tabs',
    url = 'https://pypi.python.org/packages/b2/82/b867824daeb4f4c159e26cee72994015ce8ed4cfd4a54a078033bc8397fa/sphinx-tabs-0.2.0.tar.gz',
    sha256 = '6904e12da3db7044fb0883196befda8ff8b2547e41475e235fda5376b11f85ce'
)

http_file(
    name = 'python_javalang',
    url = 'https://pypi.python.org/packages/e4/99/499d1eee94c53e7708c447e2f91c94c7789814ef0098b3259752c520c76a/javalang-0.10.1.tar.gz',
    sha256 = 'db8c63133e7b0ad0969212d0dda726d13b2bc9fb5c2c95e3fe2449d06f20a4c5'
)

http_file(
    name = 'python_lxml',
    url = 'https://pypi.python.org/packages/4f/3f/cf6daac551fc36cddafa1a71ed48ea5fd642e5feabd3a0d83b8c3dfd0cb4/lxml-3.6.4.tar.gz',
    sha256 = '61d5d3e00b5821e6cda099b3b4ccfea4527bf7c595e0fb3a7a760490cedd6172'
)

http_file(
    name = 'python_beautifulsoup4',
    url = 'https://pypi.python.org/packages/86/ea/8e9fbce5c8405b9614f1fd304f7109d9169a3516a493ce4f7f77c39435b7/beautifulsoup4-4.5.1.tar.gz',
    sha256 = '3c9474036afda9136aac6463def733f81017bf9ef3510d25634f335b0c87f5e1'
)

http_file(
    name = 'python_alabaster',
    url = 'https://pypi.python.org/packages/71/c3/70da7d8ac18a4f4c502887bd2549e05745fa403e2cd9d06a8a9910a762bc/alabaster-0.7.9.tar.gz',
    sha256 = '47afd43b08a4ecaa45e3496e139a193ce364571e7e10c6a87ca1a4c57eb7ea08'
)

http_file(
    name = 'python_babel',
    url = 'https://pypi.python.org/packages/6e/96/ba2a2462ed25ca0e651fb7b66e7080f5315f91425a07ea5b34d7c870c114/Babel-2.3.4.tar.gz',
    sha256 = 'c535c4403802f6eb38173cd4863e419e2274921a01a8aad8a5b497c131c62875'
)

http_file(
    name = 'python_docutils',
    url = 'https://pypi.python.org/packages/37/38/ceda70135b9144d84884ae2fc5886c6baac4edea39550f28bcd144c1234d/docutils-0.12.tar.gz',
    sha256 = 'c7db717810ab6965f66c8cf0398a98c9d8df982da39b4cd7f162911eb89596fa'
)

http_file(
    name = 'python_enum34',
    url = 'https://pypi.python.org/packages/bf/3e/31d502c25302814a7c2f1d3959d2a3b3f78e509002ba91aea64993936876/enum34-1.1.6.tar.gz',
    sha256 = '8ad8c4783bf61ded74527bffb48ed9b54166685e4230386a9ed9b1279e2df5b1'
)

http_file(
    name = 'python_markupsafe',
    url = 'https://pypi.python.org/packages/c0/41/bae1254e0396c0cc8cf1751cb7d9afc90a602353695af5952530482c963f/MarkupSafe-0.23.tar.gz',
    sha256 = 'a4ec1aff59b95a14b45eb2e23761a0179e98319da5a7eb76b56ea8cdc7b871c3'
)

http_file(
    name = 'python_pbr',
    url = 'https://pypi.python.org/packages/c3/2c/63275fab26a0fd8cadafca71a3623e4d0f0ee8ed7124a5bb128853d178a7/pbr-1.10.0.tar.gz',
    sha256 = '186428c270309e6fdfe2d5ab0949ab21ae5f7dea831eab96701b86bd666af39c'
)

http_file(
    name = 'python_pyenchant',
    url = 'https://pypi.python.org/packages/73/73/49f95fe636ab3deed0ef1e3b9087902413bcdf74ec00298c3059e660cfbb/pyenchant-1.6.8.tar.gz',
    sha256 = '7ead2ee74f1a4fc2a7199b3d6012eaaaceea03fbcadcb5df67d2f9d0d51f050a'
)

http_file(
    name = 'python_pygments',
    url = 'https://pypi.python.org/packages/b8/67/ab177979be1c81bc99c8d0592ef22d547e70bb4c6815c383286ed5dec504/Pygments-2.1.3.tar.gz',
    sha256 = '88e4c8a91b2af5962bfa5ea2447ec6dd357018e86e94c7d14bd8cacbc5b55d81'
)

http_file(
    name = 'python_pytz',
    url = 'https://pypi.python.org/packages/1d/ff/84f49d2f318383c634b0709aae6c5671e65f63017a9742dee760fd4d897d/pytz-2016.7.zip',
    sha256 = '054d9814d00254571cff84f6faedb25c046008322cbe51a6d5664c912b4f2929'
)

http_file(
    name = 'python_six',
    url = 'https://pypi.python.org/packages/b3/b2/238e2590826bfdd113244a40d9d3eb26918bd798fc187e2360a8367068db/six-1.10.0.tar.gz',
    sha256 = '105f8d68616f8248e24bf0e9372ef04d3cc10104f1980f54d57b2ce73a5ad56a'
)

http_file(
    name = 'python_snowballstemmer',
    url = 'https://pypi.python.org/packages/20/6b/d2a7cb176d4d664d94a6debf52cd8dbae1f7203c8e42426daa077051d59c/snowballstemmer-1.2.1.tar.gz',
    sha256 = '919f26a68b2c17a7634da993d91339e288964f93c274f1343e3bbbe2096e1128'
)

http_file(
    name = 'python_pep8',
    url = 'https://pypi.python.org/packages/3e/b5/1f717b85fbf5d43d81e3c603a7a2f64c9f1dabc69a1e7745bd394cc06404/pep8-1.7.0.tar.gz',
    sha256 = 'a113d5f5ad7a7abacef9df5ec3f2af23a20a28005921577b15dd584d099d5900'
)

http_file(
    name = 'python_pylint',
    url = 'https://pypi.python.org/packages/92/f3/41deb50322d579517f779c3421b92f84133ddb6d954791bbd37aca1b5854/pylint-1.6.4-py2.py3-none-any.whl',
    sha256 = 'eeeeb81c8095586b417ea0602c01f53d1c87694fcf3c866f8681457f94875a8e'
)

http_file(
    name = 'python_astroid',
    url = 'https://pypi.python.org/packages/7c/80/9122e452bb54640a67933d3ff586b6e03849dca086eed53542521b1cf894/astroid-1.4.8.tar.gz',
    sha256 = '5f064785a7e45ed519285f2eb30b795e58a4932a0736b32030da6fef3394ddb3'
)

http_file(
    name = 'python_backports_functools_lru_cache',
    url = 'https://pypi.python.org/packages/d4/40/0b1db94fdfd71353ae67ec444ff28e0a7ecc25212d1cb94c291b6cd226f9/backports.functools_lru_cache-1.3-py2.py3-none-any.whl',
    sha256 = 'ac661058b4b9c770c0f045a71cf3cafedd1be11071d6116201ee5d7245c61034'
)

http_file(
    name = 'python_configparser',
    url = 'https://pypi.python.org/packages/7c/69/c2ce7e91c89dc073eb1aa74c0621c3eefbffe8216b3f9af9d3885265c01c/configparser-3.5.0.tar.gz',
    sha256 = '5308b47021bc2340965c371f0f058cc6971a04502638d4244225c49d80db273a'
)

http_file(
    name = 'python_isort',
    url = 'https://pypi.python.org/packages/70/65/49f66364f4ac551ec414e88537b02be439d1d9ea7e1fdd6d526fb8796bf9/isort-4.2.5.tar.gz',
    sha256 = '56b20044f43cf6e6783fe95d054e754acca52dd43fbe9277c1bdff835537ea5c'
)

http_file(
    name = 'python_lazy_object_proxy',
    url = 'https://pypi.python.org/packages/65/63/b6061968b0f3c7c52887456dfccbd07bec2303296911757d8c1cc228afe6/lazy-object-proxy-1.2.2.tar.gz',
    sha256 = 'ddd4cf1c74279c349cb7b9c54a2efa5105854f57de5f2d35829ee93631564268'
)

http_file(
    name = 'python_mccabe',
    url = 'https://pypi.python.org/packages/17/9c/66792b5f917a09f7e433dfd6e20ac12964006e1d794f799c2333afc10be1/mccabe-0.5.2-py2.py3-none-any.whl',
    sha256 = '91cc38b2c7636aaf1903e06d96ee960fb3dff9ca3afc595627c9a638f8e86d2b'
)

http_file(
    name = 'python_wrapt',
    url = 'https://pypi.python.org/packages/00/dd/dc22f8d06ee1f16788131954fc69bc4438f8d0125dd62419a43b86383458/wrapt-1.10.8.tar.gz',
    sha256 = '4ea17e814e39883c6cf1bb9b0835d316b2f69f0f0882ffe7dad1ede66ba82c73'
)

http_file(
    name = 'python_cpplint',
    url = 'https://pypi.python.org/packages/95/42/27a16ef7fc609aba82bec923e2d29a1fa163bc95a267eaf1acc780e949fc/cpplint-1.3.0.tar.gz',
    sha256 = '6876139c3944c6dc84cc9095b6c4be3c5397b534b0c00230ba59c4b893936719'
)
