# Build native code

pushd native
if (test-path out) {
    rmdir -r -Force out
}
mkdir out
cd out
cmake ..
cmake --build . --config Release
popd

dotnet publish src/isles -c Release -o out
