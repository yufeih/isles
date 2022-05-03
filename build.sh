#!/usr/bin/env bash

# Build native code
mkdir build
pushd build
cmake \
  -DCMAKE_BUILD_TYPE=Release \
  -DBOX2D_BUILD_TESTBED=OFF \
  -DBOX2D_BUILD_UNIT_TESTS=OFF \
  -DgRPC_BUILD_GRPC_OBJECTIVE_C_PLUGIN=OFF \
  -DgRPC_BUILD_GRPC_PHP_PLUGIN=OFF \
  -DgRPC_BUILD_GRPC_RUBY_PLUGIN=OFF \
  -DgRPC_BUILD_GRPC_PYTHON_PLUGIN=OFF \
  ..
popd

cmake --build build

dotnet publish src/isles -c Release -o out
