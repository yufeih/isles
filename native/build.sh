#!/usr/bin/env bash
rm -rf out
mkdir out
cd out
cmake -DCMAKE_BUILD_TYPE=Release ..
cmake --build .