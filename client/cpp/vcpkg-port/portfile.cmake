vcpkg_download_distfile(ARCHIVE
    URLS "https://github.com/djungelorm/krpc/releases/download/v${VERSION}/krpc-cpp-${VERSION}.zip"
    FILENAME "krpc-cpp-${VERSION}.zip"
    SHA512 0  # update with sha512sum after cutting the release
)

vcpkg_extract_source_archive(SOURCE_PATH
    ARCHIVE "${ARCHIVE}"
    SOURCE_BASE "krpc-cpp-${VERSION}"
)

vcpkg_cmake_configure(
    SOURCE_PATH "${SOURCE_PATH}"
    OPTIONS
        -DKRPC_FETCH_PROTOBUF=OFF
        -DKRPC_FETCH_ASIO=OFF
        -DKRPC_FETCH_ABSL=OFF
)

vcpkg_cmake_install()
vcpkg_cmake_config_fixup(CONFIG_PATH lib/cmake/krpc)
vcpkg_copy_pdbs()

file(REMOVE_RECURSE "${CURRENT_PACKAGES_DIR}/debug/include")
file(INSTALL "${CMAKE_CURRENT_LIST_DIR}/usage" DESTINATION "${CURRENT_PACKAGES_DIR}/share/krpc")
vcpkg_install_copyright(FILE_LIST "${SOURCE_PATH}/COPYING.LESSER")
