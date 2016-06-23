workspace(name = "krpc")

http_archive(
    name = 'protobuf',
    url = 'https://github.com/google/protobuf/archive/v3.0.0-beta-3.tar.gz',
    strip_prefix = 'protobuf-3.0.0-beta-3',
    sha256 = 'd8d11564ff4085e7095cf5601fdc094946e6dbb0085863829668eb3a50b1ae0d'
)

http_file(
    name = 'csharp_nuget',
    url = 'https://dist.nuget.org/win-x86-commandline/v3.3.0/nuget.exe',
    sha256 = 'af8ee5c2299a7d71f4bfefe046701af551c348b8c9f6c10302598262f16d42aa'
)

new_http_archive(
    name = 'csharp_protobuf',
    build_file = 'tools/build/csharp_protobuf.BUILD',
    url = 'https://www.nuget.org/api/v2/package/Google.Protobuf/3.0.0-beta3',
    type = 'zip',
    sha256 = '0b647895f3cbb9f1ed2be601aacc1c7fb724d14f3aaa82a2af81ac5317a39fc9'
)

http_file(
    name = 'csharp_protobuf_net35',
    url = 'https://github.com/djungelorm/protobuf/releases/download/v3.0.0-beta-3-net35/Google.Protobuf.dll',
    sha256 = '9031bd7ece9a80ad0efd9bb0ed837b1933ccb01c7acd180b5456baf55ebd5d9e'
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
    url = 'https://www.nuget.org/api/v2/package/Newtonsoft.Json/8.0.3',
    type = 'zip',
    sha256 = '210e42a1bad0928188ef35e1ecdc9e0f3468b8f9153db268cdaa2c2d5c9b2197'
)

new_http_archive(
    name = 'csharp_options',
    build_file = 'tools/build/csharp_options.BUILD',
    url = 'https://www.nuget.org/api/v2/package/NDesk.Options/0.2.1',
    type = 'zip',
    sha256 = 'f7cad7f76b9a738930496310ea47888529fbfd0a39896bdfd3cfd17fd385f53b'
)

new_http_archive(
    name = 'cpp_asio',
    build_file = 'tools/build/cpp_asio.BUILD',
    url = 'http://downloads.sourceforge.net/project/asio/asio/1.10.6%20%28Stable%29/asio-1.10.6.tar.gz',
    strip_prefix = 'asio-1.10.6',
    sha256 = '70345ca9e372a119c361be5e7f846643ee90997da8f88ec73f7491db96e24bbe'
)

new_http_archive(
    name = 'cpp_gmock',
    build_file = 'tools/build/cpp_gmock.BUILD',
    url = 'https://googlemock.googlecode.com/files/gmock-1.7.0.zip',
    strip_prefix = 'gmock-1.7.0',
    sha256 = '26fcbb5925b74ad5fc8c26b0495dfc96353f4d553492eb97e85a8a6d2f43095b'
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
    url = 'https://repo1.maven.org/maven2/com/google/protobuf/protobuf-java/3.0.0-beta-3/protobuf-java-3.0.0-beta-3.jar',
    sha256 = '9a99ae680c1e5682ed2bfee834d6f18f7772e6b7d338d38b210bf94b44247044'
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
    url = 'https://luarocks.org/manifests/hisham/luafilesystem-1.6.3-1.src.rock',
    sha256 = '70121e78b8ef9365265b85027729d0520c1163f5609abfa9554b215a672f4e7a'
)

http_file(
    name = 'lua_penlight',
    url = 'http://luarocks.org/repositories/rocks/penlight-1.3.1-1.src.rock',
    sha256 = '13c6fcc5058a998505ddc4b52496f591d7d37ed2efa9a46a2c39db6183f38783'
)

http_file(
    name = 'lua_luaunit',
    url = 'https://raw.githubusercontent.com/bluebird75/luaunit/LUAUNIT_V3_1/luaunit.lua',
    sha256 = '77e00531fb9c1a54fc6d8a8a55691328f18f4f0cde0da0a49a00272ceae67dd0'
)

http_file(
    name = 'python_protobuf',
    url = 'https://pypi.python.org/packages/52/f8/1b0d57028ca6144a03e1fdb5eeca6bd10194dcbfc2405d920c47bb7a79ca/protobuf-3.0.0b3.tar.gz',
    sha256 = 'b4f0a326f1776f874152243bea10ba924278bf76b7b9e10991c7f8d17eb71525'
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
    url = 'https://pypi.python.org/packages/source/s/sphinxcontrib-spelling/sphinxcontrib-spelling-2.1.2.tar.gz',
    sha256='c5ac488141408564cb60f355c50efd90b826a9fc7723738a07ab907a0384f086'
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
    name = 'python_javalang',
    url = 'https://pypi.python.org/packages/source/j/javalang/javalang-0.10.0.tar.gz',
    sha256 = 'f18095855f8b8ed90907c1900197eea54020db3f20aa5a16d5cb0276a751c87b'
)

http_file(
    name = 'python_lxml',
    url = 'https://pypi.python.org/packages/source/l/lxml/lxml-3.6.0.tar.gz',
    sha256 = '9c74ca28a7f0c30dca8872281b3c47705e21217c8bc63912d95c9e2a7cac6bdf'
)

http_file(
    name = 'python_beautifulsoup4',
    url = 'https://pypi.python.org/packages/source/b/beautifulsoup4/beautifulsoup4-4.4.1.tar.gz',
    sha256 = '87d4013d0625d4789a4f56b8d79a04d5ce6db1152bb65f1d39744f7709a366b4'
)

http_file(
    name = 'python_alabaster',
    url = 'https://pypi.python.org/packages/source/a/alabaster/alabaster-0.7.7.tar.gz',
    sha256 = 'f416a84e0d0ddbc288f6b8f2c276d10b40ca1238562cd9ed5a751292ec647b71'
)

http_file(
    name = 'python_babel',
    url = 'https://pypi.python.org/packages/source/B/Babel/Babel-2.2.0.tar.gz',
    sha256 = 'd8cb4c0e78148aee89560f9fe21587aa57739c975bb89ff66b1e842cc697428f'
)

http_file(
    name = 'python_docutils',
    url = 'https://pypi.python.org/packages/source/d/docutils/docutils-0.12.tar.gz',
    sha256 = 'c7db717810ab6965f66c8cf0398a98c9d8df982da39b4cd7f162911eb89596fa'
)

http_file(
    name = 'python_enum34',
    url = 'https://pypi.python.org/packages/source/e/enum34/enum34-1.1.2.tar.gz',
    sha256 = '2475d7fcddf5951e92ff546972758802de5260bf409319a9f1934e6bbc8b1dc7'
)

http_file(
    name = 'python_markupsafe',
    url = 'https://pypi.python.org/packages/source/M/MarkupSafe/MarkupSafe-0.23.tar.gz',
    sha256 = 'a4ec1aff59b95a14b45eb2e23761a0179e98319da5a7eb76b56ea8cdc7b871c3'
)

http_file(
    name = 'python_pbr',
    url = 'https://pypi.python.org/packages/source/p/pbr/pbr-1.8.1.tar.gz',
    sha256 = 'e2127626a91e6c885db89668976db31020f0af2da728924b56480fc7ccf09649'
)

http_file(
    name = 'python_pyenchant',
    url = 'https://pypi.python.org/packages/source/p/pyenchant/pyenchant-1.6.6.tar.gz',
    sha256 = '25c9d2667d512f8fc4410465fdd2e868377ca07eb3d56e2b6e534a86281d64d3'
)

http_file(
    name = 'python_pygments',
    url = 'https://pypi.python.org/packages/source/P/Pygments/Pygments-2.1.3.tar.gz',
    sha256 = '88e4c8a91b2af5962bfa5ea2447ec6dd357018e86e94c7d14bd8cacbc5b55d81'
)

http_file(
    name = 'python_pytz',
    url = 'https://pypi.python.org/packages/source/p/pytz/pytz-2016.3.tar.bz2',
    sha256 = 'c193dfa167ac32c8cb96f26cbcd92972591b22bda0bac3effdbdb04de6cc55d6'
)

http_file(
    name = 'python_six',
    url = 'https://pypi.python.org/packages/source/s/six/six-1.10.0.tar.gz',
    sha256 = '105f8d68616f8248e24bf0e9372ef04d3cc10104f1980f54d57b2ce73a5ad56a'
)

http_file(
    name = 'python_snowballstemmer',
    url = 'https://pypi.python.org/packages/source/s/snowballstemmer/snowballstemmer-1.2.1.tar.gz',
    sha256 = '919f26a68b2c17a7634da993d91339e288964f93c274f1343e3bbbe2096e1128'
)

http_file(
    name = 'python_pylint',
    url = 'https://pypi.python.org/packages/source/p/pylint/pylint-1.5.5.tar.gz',
    sha256 = '15e949bbeda6c0a66799f34f720ab15e38d0a128e752cff5e74168527e5399c7'
)

http_file(
    name = 'python_astroid',
    url = 'https://pypi.python.org/packages/source/a/astroid/astroid-1.4.5.tar.gz',
    sha256 = '729b986aa59fb77af533707c385021b04e60d136b5f21cc766618556d0816cf6'
)

http_file(
    name = 'python_wrapt',
    url = 'https://pypi.python.org/packages/source/w/wrapt/wrapt-1.10.8.tar.gz',
    sha256 = '4ea17e814e39883c6cf1bb9b0835d316b2f69f0f0882ffe7dad1ede66ba82c73'
)

http_file(
    name = 'python_lazy_object_proxy',
    url = 'https://pypi.python.org/packages/source/l/lazy-object-proxy/lazy-object-proxy-1.2.2.tar.gz',
    sha256 = 'ddd4cf1c74279c349cb7b9c54a2efa5105854f57de5f2d35829ee93631564268'
)

http_file(
    name = 'python_cpplint',
    url = 'https://pypi.python.org/packages/29/56/f80296456e320ce88a52189b0b08a890520fa88049d4b6cec0f6a3d55fb3/cpplint-1.2.2.tar.gz',
    sha256 = 'b2979ff630299293f23c52096e408f2b359e2e26cb5cdf24aed4ce53e4293468'
)
