## Isles: An open source 3D real-time strategy game

![build status](https://github.com/yufeih/isles/actions/workflows/build.yml/badge.svg)

This project aims to build an open source multiplayer 3D real-time strategy game platform that enables the community to create customized content and gameplay.

The game is playable today with 1 map and 1 race. Video footage showcasing the current gameplay:

[![Isles gameplay](https://img.youtube.com/vi/rdRk1brPLQc/0.jpg)](https://www.youtube.com/watch?v=rdRk1brPLQc)

## Installation

To install the game, download the latest artifact from the main branch GitHub action.

Supported platforms: _Windows_, _MacOS_, _Linux_.

## Build from Source

Prerequisite:
- [git](https://git-scm.com/)
- [.NET Core 6 SDK](https://dotnet.microsoft.com/download/dotnet/6.0)

1. Run `git submodule update --init --recursive` to fetch all dependencies.
2. Run `./build.ps1` on Windows and `./build/sh` on other systems to produce binary files under `out` folder.
3. Run `dotnet build` for C# only changes.

> The only way to rebuild  shaders (`*.fx` files) today is using Visual Studio on Windows due to dependency on `Microsoft.HLSL.CSharpVB`.
