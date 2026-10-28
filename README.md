# Bézier Surface Renderer

Assignment 2 — Bézier surfaces. A WinForms application that triangulates bicubic Bézier surfaces read from
a text file and renders them with a software scan-line filler (bucket-sorted edge table), per-pixel
Lambert + specular lighting, a Z-buffer, textures and normal maps. Full user and technical
documentation: [src/BezierSurfaceRenderer.App/Docs/Help.md](src/BezierSurfaceRenderer.App/Docs/Help.md)
(also shown inside the application with **F1**).

## Requirements
- Windows 10/11 with the .NET 8 SDK (Visual Studio 2022 17.8+ or `dotnet` CLI).

## Build and run
```
dotnet run --project src/BezierSurfaceRenderer.App
```
or open `BezierSurfaceRenderer.sln` in Visual Studio and start `BezierSurfaceRenderer.App`.
Optional argument: path to another surface file (`BezierSurfaceRenderer.exe path\to\surfaces.txt`).

## Tests
The UI-independent core (maths, filler, lighting, rendering) is covered by unit tests that run on any OS:
```
dotnet test
```

## Layout
| Path | Content |
|---|---|
| `src/BezierSurfaceRenderer.Core` | Geometry, rotation, edge-table filler, Z-buffer, lighting, light path (no UI) |
| `src/BezierSurfaceRenderer.App` | WinForms UI, assets (`surfaces.txt`, texture, normal maps), documentation |
| `tests/BezierSurfaceRenderer.Core.Tests` | xUnit tests |
| `tools/gen_assets.py` | Regenerates the default texture and normal maps |
