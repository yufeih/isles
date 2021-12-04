REM Build native code

pushd native
rmdir /s /q out
mkdir out
cd out
cmake -DCMAKE_BUILD_TYPE=Release ..
cmake --build --config Release .
popd

dotnet publish src/isles -c Release -o out
