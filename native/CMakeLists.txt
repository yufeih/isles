cmake_minimum_required(VERSION 3.12)

project(isles.native)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED true)

set(BOX2D_BUILD_UNIT_TESTS OFF CACHE BOOL "Build the Box2D unit tests" FORCE)
set(BOX2D_BUILD_TESTBED OFF CACHE BOOL "Build the Box2D testbed" FORCE)

add_subdirectory(../extern/box2d box2d)

set(ISLES_HEADER_FILES
    include/isles.h)

set(ISLES_SOURCE_FILES
    src/lib.cpp)

add_library(isles.native SHARED ${ISLES_SOURCE_FILES})

target_include_directories(isles.native PRIVATE ${CMAKE_CURRENT_SOURCE_DIR}/include)

target_link_libraries(isles.native PUBLIC box2d)
