# Build native code
mkdir -p build
pushd build
cmake -DCMAKE_BUILD_TYPE=Release ..
popd

cmake --build build

dotnet publish src/isles -c Release -o out
