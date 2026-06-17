# FindAsio.cmake — locate standalone ASIO (header-only, no official CMake config)
#
# Sets Asio_FOUND and creates imported target asio::asio.
# Honour ASIO_ROOT as a hint (set via -DASIO_ROOT=... or environment).

find_path(Asio_INCLUDE_DIR
  NAMES asio.hpp
  HINTS
    ${ASIO_ROOT}
    $ENV{ASIO_ROOT}
    ${ASIO_ROOT}/include
    $ENV{ASIO_ROOT}/include
  PATHS
    /usr/include
    /usr/local/include
    /opt/local/include
)

include(FindPackageHandleStandardArgs)
find_package_handle_standard_args(Asio
  REQUIRED_VARS Asio_INCLUDE_DIR)

if(Asio_FOUND AND NOT TARGET asio::asio)
  add_library(asio::asio INTERFACE IMPORTED)
  set_target_properties(asio::asio PROPERTIES
    INTERFACE_INCLUDE_DIRECTORIES "${Asio_INCLUDE_DIR}")
endif()

mark_as_advanced(Asio_INCLUDE_DIR)
