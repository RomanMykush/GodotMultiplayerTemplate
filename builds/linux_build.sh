cd ..

proj_name="Steampunk DnD"

if [ -d ./builds/artifacts/linux ]; then
    echo "Clearing previous artifacts..."
    rm -rf ./builds/artifacts/linux
fi
mkdir -p ./builds/artifacts/linux/server

echo "Building Linux client..."
godot --headless --export-release "Linux Client" "./builds/artifacts/linux/$proj_name.x86_64" > /dev/null
if [ $? -ne 0 ]; then
    echo "Build failed."
    exit 1
fi

echo "Building Linux server..."
godot --headless --export-release "Linux Server" ./builds/artifacts/linux/server/server.x86_64 > /dev/null
if [ $? -ne 0 ]; then
    echo "Build failed."
    exit 1
fi

echo "Successfully built artifacts."
