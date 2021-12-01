rmdir /s /q out
mkdir out
cd out
cmake -DCMAKE_BUILD_TYPE=Release ..
cmake --build . --config Release
