# FindAsio.cmake — locate standalone ASIO (header-only, no official CMake config)
#
# Sets Asio_FOUND and Asio_VERSION, and creates imported target asio::asio.
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

# ASIO states its version as a single number: the major times 100000, plus the minor
# times 100, plus the sub-minor.
if(Asio_INCLUDE_DIR AND EXISTS "${Asio_INCLUDE_DIR}/asio/version.hpp")
  file(STRINGS "${Asio_INCLUDE_DIR}/asio/version.hpp" _asio_version_define
       REGEX "^#define ASIO_VERSION ")
  if(_asio_version_define MATCHES "([0-9]+)")
    math(EXPR _asio_major "${CMAKE_MATCH_1} / 100000")
    math(EXPR _asio_minor "${CMAKE_MATCH_1} / 100 % 1000")
    math(EXPR _asio_sub_minor "${CMAKE_MATCH_1} % 100")
    set(Asio_VERSION "${_asio_major}.${_asio_minor}.${_asio_sub_minor}")
    unset(_asio_major)
    unset(_asio_minor)
    unset(_asio_sub_minor)
  endif()
  unset(_asio_version_define)
endif()

include(FindPackageHandleStandardArgs)
find_package_handle_standard_args(Asio
  REQUIRED_VARS Asio_INCLUDE_DIR
  VERSION_VAR Asio_VERSION)

if(Asio_FOUND AND NOT TARGET asio::asio)
  add_library(asio::asio INTERFACE IMPORTED)
  set_target_properties(asio::asio PROPERTIES
    INTERFACE_INCLUDE_DIRECTORIES "${Asio_INCLUDE_DIR}")
endif()

mark_as_advanced(Asio_INCLUDE_DIR)
