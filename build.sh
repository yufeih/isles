#!/usr/bin/env bash

# Build native code
pushd native
rm -rf out
mkdir out
cd out
cmake -DCMAKE_BUILD_TYPE=Release ..
cmake --build .
popd

dotnet publish src/isles -c Release -o out
