workspace(name = "krpc")

new_http_archive(
    name = 'protoc_linux_x86_32',
    build_file = 'tools/build/protobuf/protoc_linux_x86_32.BUILD',
    url = 'https://github.com/google/protobuf/releases/download/v3.4.0/protoc-3.4.0-linux-x86_32.zip',
    sha256 = '6cacb05eb9aa7690b85db7fc3c4c9124751c4ecfb4f20d2e6f61eda2b1b789d3'
)

new_http_archive(
    name = 'protoc_linux_x86_64',
    build_file = 'tools/build/protobuf/protoc_linux_x86_64.BUILD',
    url = 'https://github.com/google/protobuf/releases/download/v3.4.0/protoc-3.4.0-linux-x86_64.zip',
    sha256 = 'e4b51de1b75813e62d6ecdde582efa798586e09b5beaebfb866ae7c9eaadace4'
)

new_http_archive(
    name = 'protoc_osx_x86_32',
    build_file = 'tools/build/protobuf/protoc_osx_x86_32.BUILD',
    url = 'https://github.com/google/protobuf/releases/download/v3.4.0/protoc-3.4.0-osx-x86_32.zip',
    sha256 = '8601d7c7afb727ca31c42597a7863a7071ebdf59d3d35b31320379eaa55e23f9'
)

new_http_archive(
    name = 'protoc_win32',
    build_file = 'tools/build/protobuf/protoc_win32.BUILD',
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
    build_file = 'tools/build/csharp_protobuf.BUILD',
    url = 'https://www.nuget.org/api/v2/package/Google.Protobuf/3.4.0',
    type = 'zip',
    sha256 = 'dfd91888be6ec88af5649407cae4897f0d6793344355c4ba6f3d056c7767409e'
)

http_file(
    name = 'csharp_protobuf_net35',
    url = 'https://s3.amazonaws.com/krpc/lib/protobuf-3.4.0-net35/Google.Protobuf.dll',
    sha256 = '496bf64ad9887c539cf7cc070d7e42edd4c8a8534286179431971ffa62ec3e4c'
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
    url = 'https://github.com/google/protobuf/releases/download/v3.4.0/protobuf-cpp-3.4.0.tar.gz',
    strip_prefix = 'protobuf-3.4.0',
    sha256 = '71434f6f836a1e479c44008bb033b2a8b2560ff539374dcdefb126be739e1635'
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
    url = 'https://repo1.maven.org/maven2/com/google/protobuf/protobuf-java/3.4.0/protobuf-java-3.4.0.jar',
    sha256 = 'dce7e66b32456a1b1198da0caff3a8acb71548658391e798c79369241e6490a4'
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
    sha256 = '7ae20f3b68e77e3be52fc95c147eccfaef33206a7985320061fb9352d8565741'
)

http_file(
    name = 'python_alabaster',
    url = 'https://pypi.python.org/packages/d0/a5/e3a9ad3ee86aceeff71908ae562580643b955ea1b1d4f08ed6f7e8396bd7/alabaster-0.7.10.tar.gz',
    sha256 = '37cdcb9e9954ed60912ebc1ca12a9d12178c26637abdf124e3cde2341c257fe0'
)

http_file(
    name = 'python_astroid',
    url = 'https://pypi.python.org/packages/7c/80/9122e452bb54640a67933d3ff586b6e03849dca086eed53542521b1cf894/astroid-1.4.8.tar.gz',
    sha256 = '5f064785a7e45ed519285f2eb30b795e58a4932a0736b32030da6fef3394ddb3'
)

http_file(
    name = 'python_babel',
    url = 'https://pypi.python.org/packages/92/22/643f3b75f75e0220c5ef9f5b72b619ccffe9266170143a4821d4885198de/Babel-2.4.0.tar.gz',
    sha256 = '8c98f5e5f8f5f088571f2c6bd88d530e331cbbcb95a7311a0db69d3dca7ec563'
)

http_file(
    name = 'python_backports_functools_lru_cache',
    url = 'https://pypi.python.org/packages/d4/40/0b1db94fdfd71353ae67ec444ff28e0a7ecc25212d1cb94c291b6cd226f9/backports.functools_lru_cache-1.3-py2.py3-none-any.whl',
    sha256 = 'ac661058b4b9c770c0f045a71cf3cafedd1be11071d6116201ee5d7245c61034'
)

http_file(
    name = 'python_beautifulsoup4',
    url = 'https://pypi.python.org/packages/fa/8d/1d14391fdaed5abada4e0f63543fef49b8331a34ca60c88bd521bcf7f782/beautifulsoup4-4.6.0.tar.gz',
    sha256 = '808b6ac932dccb0a4126558f7dfdcf41710dd44a4ef497a0bb59a77f9f078e89'
)

http_file(
    name = 'python_certifi',
    url = 'https://pypi.python.org/packages/dd/0e/1e3b58c861d40a9ca2d7ea4ccf47271d4456ae4294c5998ad817bd1b4396/certifi-2017.4.17.tar.gz',
    sha256 = 'f7527ebf7461582ce95f7a9e03dd141ce810d40590834f4ec20cddd54234c10a'
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
    url = 'https://pypi.python.org/packages/05/25/7b5484aca5d46915493f1fd4ecb63c38c333bd32aa9ad6e19da8d08895ae/docutils-0.13.1.tar.gz',
    sha256 = '718c0f5fb677be0f34b781e04241c4067cbd9327b66bdd8e763201130f5175be'
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
    url = 'https://pypi.python.org/packages/d8/82/28a51052215014efc07feac7330ed758702fc0581347098a81699b5281cb/idna-2.5.tar.gz',
    sha256 = '3cb5ce08046c4e3a560fc02f138d0ac63e00f8ce5901a56b32ec8b7994082aab'
)

http_file(
    name = 'python_imagesize',
    url = 'https://pypi.python.org/packages/53/72/6c6f1e787d9cab2cc733cf042f125abec07209a58308831c9f292504e826/imagesize-0.7.1.tar.gz',
    sha256 = '0ab2c62b87987e3252f89d30b7cedbec12a01af9274af9ffa48108f2c13c6062'
)

http_file(
    name = 'python_isort',
    url = 'https://pypi.python.org/packages/70/65/49f66364f4ac551ec414e88537b02be439d1d9ea7e1fdd6d526fb8796bf9/isort-4.2.5.tar.gz',
    sha256 = '56b20044f43cf6e6783fe95d054e754acca52dd43fbe9277c1bdff835537ea5c'
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
    url = 'https://pypi.python.org/packages/65/63/b6061968b0f3c7c52887456dfccbd07bec2303296911757d8c1cc228afe6/lazy-object-proxy-1.2.2.tar.gz',
    sha256 = 'ddd4cf1c74279c349cb7b9c54a2efa5105854f57de5f2d35829ee93631564268'
)

http_file(
    name = 'python_lxml',
    url = 'https://pypi.python.org/packages/20/b3/9f245de14b7696e2d2a386c0b09032a2ff6625270761d6543827e667d8de/lxml-3.8.0.tar.gz',
    sha256 = '736f72be15caad8116891eb6aa4a078b590d231fdc63818c40c21624ac71db96'
)

http_file(
    name = 'python_markupsafe',
    url = 'https://pypi.python.org/packages/4d/de/32d741db316d8fdb7680822dd37001ef7a448255de9699ab4bfcbdf4172b/MarkupSafe-1.0.tar.gz',
    sha256 = 'a6be69091dac236ea9c6bc7d012beab42010fa914c459791d627dad4910eb665'
)

http_file(
    name = 'python_mccabe',
    url = 'https://pypi.python.org/packages/17/9c/66792b5f917a09f7e433dfd6e20ac12964006e1d794f799c2333afc10be1/mccabe-0.5.2-py2.py3-none-any.whl',
    sha256 = '91cc38b2c7636aaf1903e06d96ee960fb3dff9ca3afc595627c9a638f8e86d2b'
)

http_file(
    name = 'python_pbr',
    url = 'https://pypi.python.org/packages/2b/56/fd3015212c8f546c632a65b1018e8f065eff1b173d11739bb73c64cc5683/pbr-3.1.0.tar.gz',
    sha256 = 'b8af6ec309f4f3ab419b998b22073d66da55b36414e0b729cb04a408f6d73697'
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
    url = 'https://pypi.python.org/packages/73/73/49f95fe636ab3deed0ef1e3b9087902413bcdf74ec00298c3059e660cfbb/pyenchant-1.6.8.tar.gz',
    sha256 = '7ead2ee74f1a4fc2a7199b3d6012eaaaceea03fbcadcb5df67d2f9d0d51f050a'
)

http_file(
    name = 'python_pygments',
    url = 'https://pypi.python.org/packages/71/2a/2e4e77803a8bd6408a2903340ac498cb0a2181811af7c9ec92cb70b0308a/Pygments-2.2.0.tar.gz',
    sha256 = 'dbae1046def0efb574852fab9e90209b23f556367b5a320c0bcb871c77c3e8cc'
)

http_file(
    name = 'python_pylint',
    url = 'https://pypi.python.org/packages/92/f3/41deb50322d579517f779c3421b92f84133ddb6d954791bbd37aca1b5854/pylint-1.6.4-py2.py3-none-any.whl',
    sha256 = 'eeeeb81c8095586b417ea0602c01f53d1c87694fcf3c866f8681457f94875a8e'
)

http_file(
    name = 'python_pytz',
    url = 'https://pypi.python.org/packages/a4/09/c47e57fc9c7062b4e83b075d418800d322caa87ec0ac21e6308bd3a2d519/pytz-2017.2.zip',
    sha256 = 'f5c056e8f62d45ba8215e5cb8f50dfccb198b4b9fbea8500674f3443e4689589'
)

http_file(
    name = 'python_requests',
    url = 'https://pypi.python.org/packages/2c/b5/2b6e8ef8dd18203b6399e9f28c7d54f6de7b7549853fe36d575bd31e29a7/requests-2.18.1.tar.gz',
    sha256 = 'c6f3bdf4a4323ac7b45d01e04a6f6c20e32a052cd04de81e05103abc049ad9b9'
)

http_file(
    name = 'python_setuptools',
    url = 'https://pypi.python.org/packages/a9/23/720c7558ba6ad3e0f5ad01e0d6ea2288b486da32f053c73e259f7c392042/setuptools-36.0.1.zip',
    sha256 = 'e17c4687fddd6d70a6604ac0ad25e33324cec71b5137267dd5c45e103c4b288a'
)

http_file(
    name = 'python_setuptools_git',
    url = 'https://pypi.python.org/packages/d9/c5/396c2c06cc89d4ce2d8ccf1d7e6cf31b33d4466a7c65a67a992adb3c6f29/setuptools-git-1.2.tar.gz',
    sha256 = 'ff64136da01aabba76ae88b050e7197918d8b2139ccbf6144e14d472b9c40445'
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
    name = 'python_sphinx',
    url = 'https://pypi.python.org/packages/72/68/ad424cd0caf5ea9d3b53b0ed5653eb09c4abde99b0c0ec84141935281775/Sphinx-1.6.2.tar.gz',
    sha256 = '67527d767dff9a2e2159c501265cff47b6d96d39e036e8b971d6c143ff303197'
)

http_file(
    name = 'python_sphinx_csharp',
    url = 'https://pypi.python.org/packages/bc/c2/32c2a2218e8eb98ef796ceeebccd55c93d3ef7e3be131ed799c71ef47f88/sphinx-csharp-0.1.5.tar.gz',
    sha256 = 'a90887627c9218d5ee48f1603b6d00209d6d518dbdea6b26c2d4d484f9a3b16c'
)

http_file(
    name = 'python_sphinx_lua',
    url = 'https://github.com/djungelorm/sphinx-lua/releases/download/0.1.3/sphinx-lua-0.1.3.tar.gz',
    sha256 = 'ba8b9c81176d28ce26eb3f3c02a3b232c60f3a96ae68b2d945d75e8bf7be903c'
)

http_file(
    name = 'python_sphinx_tabs',
    url = 'https://pypi.python.org/packages/a8/06/17658a054864be05fe53b793aa037434d969f576ff120823f72cc20b4f9d/sphinx-tabs-1.0.0.tar.gz',
    sha256 = 'b98f2b8cff9291dac94a2b2cc57c42cd5824847fb1f4d35b99ec07c4693398c4'
)

http_file(
    name = 'python_sphinx_rtd_theme',
    url = 'https://pypi.python.org/packages/99/b5/249a803a428b4fd438dd4580a37f79c0d552025fb65619d25f960369d76b/sphinx_rtd_theme-0.1.9.tar.gz',
    sha256 = '273846f8aacac32bf9542365a593b495b68d8035c2e382c9ccedcac387c9a0a1'
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
    url = 'https://pypi.python.org/packages/17/75/3698d7992a828ad6d7be99c0a888b75ed173a9280e53dbae67326029b60e/typing-3.6.1.tar.gz',
    sha256 = 'c36dec260238e7464213dcd50d4b5ef63a507972f5780652e835d0228d0edace'
)

http_file(
    name = 'python_urllib3',
    url = 'https://pypi.python.org/packages/96/d9/40e4e515d3e17ed0adbbde1078e8518f8c4e3628496b56eb8f026a02b9e4/urllib3-1.21.1.tar.gz',
    sha256 = 'b14486978518ca0901a76ba973d7821047409d7f726f22156b24e83fd71382a5'
)

http_file(
    name = 'python_wrapt',
    url = 'https://pypi.python.org/packages/00/dd/dc22f8d06ee1f16788131954fc69bc4438f8d0125dd62419a43b86383458/wrapt-1.10.8.tar.gz',
    sha256 = '4ea17e814e39883c6cf1bb9b0835d316b2f69f0f0882ffe7dad1ede66ba82c73'
)

http_file(
    name = 'python_websocket_client',
    url = 'https://pypi.python.org/packages/a7/2b/0039154583cb0489c8e18313aa91ccd140ada103289c5c5d31d80fd6d186/websocket_client-0.40.0.tar.gz',
    sha256 = '40ac14a0c54e14d22809a5c8d553de5a2ae45de3c60105fae53bcb281b3fe6fb'
)
