## Running project

This project uses a [custom version of Godot Engine](https://github.com/RomanMykush/godot), so in order to run the project, you need to build a custom build of Godot.

### Bulding custom version of Godot Engine

The compilation instructions will cover the case of compiling the source code on Ubuntu.

#### Dependancy instalation

```
sudo apt-get update
sudo apt-get install -y build-essential scons pkg-config libx11-dev libxcursor-dev libxinerama-dev libgl1-mesa-dev libglu1-mesa-dev libasound2-dev libpulse-dev libudev-dev libxi-dev libxrandr-dev libwayland-dev dotnet8
```

If you compile for Windows, install this dependency:

```
apt install mingw-w64
```

After what, you need to specify it in MINGW_PREFIX environment variable, by adding this line to `~/.bashrc` file:

```
export MINGW_PREFIX="/usr/bin/mingw"
```

#### Compilation itself

After successful git cloning of engine source code, execute those commands:

1. For Windows build:
```
scons platform=windows target=editor module_mono_enabled=yes
```
2. For Linux build:
```
scons platform=linuxbsd target=editor module_mono_enabled=yes
```

After that, you need to build the NuGet package:

1. For Windows build:
```
bin/godot.windows.editor.x86_64.mono --headless --generate-mono-glue modules/mono/glue
./modules/mono/build_scripts/build_assemblies.py --godot-output-dir=./bin —godot-platform=windows
```
2. For Linux build:
```
bin/godot.linuxbsd.editor.x86_64.mono --headless --generate-mono-glue modules/mono/glue
./modules/mono/build_scripts/build_assemblies.py --godot-output-dir=./bin --godot-platform=linuxbsd
```

To install the built NuGet package on the target machine, run this command:

1. On Windows:
```
dotnet nuget add source "%CD%\GodotSharp\Tools\nupkgs" --name "Godot Packages v4.4.1-custom1"
```
2. On Linux:
```
dotnet nuget add source "$(pwd)/GodotSharp/Tools/nupkgs" --name "Godot Packages v4.4.1-custom1"
```

Now you can open a project with compiled editor files.

## Test algoritms

In order to open the menu of tests, you need to execute this command in the root directory of the project:

```
godot ++ --tests
```

It is worth noting that you must first make the Godot executable file globally available (or at least specify the full path to it).

To make it globally available, you can execute those commands:

```
cd /usr/bin
ln -s -f <full path to file> godot
```

## Debugging

For debugging in VS Code, `GODOT4` environment variable must point to the Godot executable file.
