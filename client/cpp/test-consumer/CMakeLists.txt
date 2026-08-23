# A program built against the installed krpc package, to verify that the package config and its
# targets work end-to-end. The build-test scripts point cmake at this directory once per scenario
# they cover, so that every one of them is checked the same way.
cmake_minimum_required(VERSION 3.15)
project(krpc_consumer_test LANGUAGES CXX)
set(CMAKE_CXX_STANDARD 17)
find_package(krpc CONFIG REQUIRED)
add_executable(test_app main.cpp)
target_link_libraries(test_app PRIVATE krpc::krpc)
