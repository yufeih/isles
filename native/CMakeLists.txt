cmake_minimum_required(VERSION 3.10)

project(isles.native)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED true)
set(CMAKE_POSITION_INDEPENDENT_CODE ON)

# isles.native
set(ISLES_HEADER_FILES
    include/move.h
    include/navmesh.h)

set(ISLES_SOURCE_FILES
    src/move.cpp
    src/navmesh.cpp)

add_library(isles.native SHARED ${ISLES_SOURCE_FILES})
target_include_directories(isles.native PRIVATE ${CMAKE_CURRENT_SOURCE_DIR}/include)

set(CMAKE_RUNTIME_OUTPUT_DIRECTORY ${CMAKE_CURRENT_SOURCE_DIR}/out)

# Remove the Debug and Release directory added by MSVC
# https://stackoverflow.com/questions/7747857/in-cmake-how-do-i-work-around-the-debug-and-release-directories-visual-studio-2
if ( MSVC )
    set_target_properties( isles.native PROPERTIES RUNTIME_OUTPUT_DIRECTORY_DEBUG ${CMAKE_CURRENT_SOURCE_DIR}/out )
    set_target_properties( isles.native PROPERTIES RUNTIME_OUTPUT_DIRECTORY_RELEASE ${CMAKE_CURRENT_SOURCE_DIR}/out )
endif ( MSVC )

# box2d
set(BOX2D_BUILD_UNIT_TESTS OFF CACHE BOOL "Build the Box2D unit tests" FORCE)
set(BOX2D_BUILD_TESTBED OFF CACHE BOOL "Build the Box2D testbed" FORCE)

add_subdirectory(../extern/box2d box2d)
target_link_libraries(isles.native PUBLIC box2d)

# earcut.hpp
include_directories(../extern/earcut.hpp/include/mapbox)
