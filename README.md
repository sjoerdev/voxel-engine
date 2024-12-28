## Sjoerd's Voxel Engine
This engine tries to ray-trace pure voxels as fast as possible without sacrificing dynamic geometry.

## Features:
- Fully dynamic geometry
- Heavily optimized software ray tracing
- Support for magica voxel models
- Volumetric ambient occlusion
- Runs on windows and linux

## Gallery:
<img width="720" src="https://github.com/user-attachments/assets/bb284e3e-a679-4039-aa6d-ea38b602639c">
<img width="720" src="https://github.com/user-attachments/assets/f84cac27-800a-49a6-bd16-bb4c33ca6244">
<img width="720" src="https://github.com/user-attachments/assets/0e3ffc01-bed3-4e48-8e44-761d525dd76c">

## System Requirements:
- Visual C++ Redistributable ([Download](https://aka.ms/vs/17/release/vc_redist.x64.exe))

## Building:

Download .NET 9: https://dotnet.microsoft.com/en-us/download

Building for Windows:
1. Run this command: ``dotnet publish -o ./build/windows --sc true -r win-x64 -c release``
2. Copy the ``res/`` folder the the ``build/windows/`` directory

Building for Linux:
1. Run this command: ``dotnet publish -o ./build/linux --sc true -r linux-x64 -c release``
2. Copy the ``res/`` folder the the ``build/linux/`` directory
