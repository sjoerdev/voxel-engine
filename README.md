## Sjoerd's Voxel Engine
This engine tries to ray-trace pure voxels as fast as possible without sacrificing dynamic geometry.

## Features:
- Fully dynamic geometry
- Heavily optimized software ray tracing
- Support for older graphics cards
- Support for magica voxel models
- Volumetric ambient occlusion
- Runs on windows and linux

## System Requirements:
- The .NET 9 SDK
- Visual C++ Redistributable

## Gallery:
<img width="720" src="https://github.com/user-attachments/assets/bb284e3e-a679-4039-aa6d-ea38b602639c">
<img width="720" src="https://github.com/user-attachments/assets/f84cac27-800a-49a6-bd16-bb4c33ca6244">
<img width="720" src="https://github.com/user-attachments/assets/0e3ffc01-bed3-4e48-8e44-761d525dd76c">

## Building:

Download .NET 9: https://dotnet.microsoft.com/en-us/download

Restore dependencies: ``dotnet restore``

Run in debug mode: ``dotnet run``

Build for windows: ``dotnet publish -o ./build/windows --sc True -r win-x64``

Build for linux: ``dotnet publish -o ./build/linux --sc True -r linux-x64``
