cmake_minimum_required(VERSION 3.12)

project(isles)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED true)

set(BOX2D_BUILD_UNIT_TESTS OFF CACHE BOOL "Build the Box2D unit tests" FORCE)
set(BOX2D_BUILD_TESTBED OFF CACHE BOOL "Build the Box2D testbed" FORCE)

add_subdirectory(../../extern/box2d box2d)

add_library(isles lib.cpp)

target_link_libraries(isles PUBLIC box2d)
