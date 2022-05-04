cmake_minimum_required(VERSION 3.10)

project(isles.native)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED true)
set(CMAKE_POSITION_INDEPENDENT_CODE ON)

# isles.native
set(ISLES_SOURCE_FILES
    src/isles.native/main.cpp
    src/isles.native/move.cpp
    src/isles.native/http.cpp)

add_library(isles.native SHARED ${ISLES_SOURCE_FILES})
target_include_directories(isles.native PRIVATE ${CMAKE_CURRENT_SOURCE_DIR}/include)

set(CMAKE_RUNTIME_OUTPUT_DIRECTORY ${CMAKE_CURRENT_SOURCE_DIR}/build)

# Remove the Debug and Release directory added by MSVC
# https://stackoverflow.com/questions/7747857/in-cmake-how-do-i-work-around-the-debug-and-release-directories-visual-studio-2
if ( MSVC )
    set_target_properties( isles.native PROPERTIES RUNTIME_OUTPUT_DIRECTORY_DEBUG ${CMAKE_CURRENT_SOURCE_DIR}/build )
    set_target_properties( isles.native PROPERTIES RUNTIME_OUTPUT_DIRECTORY_RELEASE ${CMAKE_CURRENT_SOURCE_DIR}/build )
endif ( MSVC )

# box2d
add_subdirectory(extern/box2d box2d EXCLUDE_FROM_ALL)
target_link_libraries(isles.native PRIVATE box2d)

# boost
include(ExternalProject)
ExternalProject_Add(
    boost
    URL https://boostorg.jfrog.io/artifactory/main/release/1.79.0/source/boost_1_79_0.zip
    CONFIGURE_COMMAND ""
    BUILD_COMMAND ""
    INSTALL_COMMAND ""
)

ExternalProject_Get_Property(boost SOURCE_DIR)
target_include_directories(isles.native PRIVATE ${SOURCE_DIR})
add_dependencies(isles.native boost)