# A program built against the installed krpc_cnano package, to verify that the package config and
# its targets work end-to-end. The build-test scripts point cmake at this directory once per
# scenario they cover, so that every one of them is checked the same way.
cmake_minimum_required(VERSION 3.15)
project(krpc_cnano_consumer_test LANGUAGES C)
set(CMAKE_C_STANDARD 99)
find_package(krpc_cnano CONFIG REQUIRED)
add_executable(test_app main.c)
target_link_libraries(test_app PRIVATE krpc_cnano::krpc_cnano)
