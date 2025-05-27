cd ..

proj_name="Steampunk DnD"

if [ -d ./builds/artifacts/windows ]; then
    echo "Clearing previous artifacts..."
    rm -rf ./builds/artifacts/windows
fi
mkdir -p ./builds/artifacts/windows/server

echo "Building Windows client..."
godot --headless --export-release "Windows Client" "./builds/artifacts/windows/$proj_name.exe" > /dev/null
if [ $? -ne 0 ]; then
    echo "Build failed."
    exit 1
fi

echo "Building Windows server..."
godot --headless --export-release "Windows Server" ./builds/artifacts/windows/server/server.exe > /dev/null
if [ $? -ne 0 ]; then
    echo "Build failed."
    exit 1
fi

echo "Successfully built artifacts."
