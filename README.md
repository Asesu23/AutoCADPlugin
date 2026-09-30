# Jpegger

[![Build](https://github.com/Asesu23/Jpegger/actions/workflows/release.yml/badge.svg)](https://github.com/Asesu23/Jpegger/actions/workflows/release.yml)
[![Release](https://img.shields.io/github/v/release/Asesu23/Jpegger)](https://github.com/Asesu23/Jpegger/releases/latest)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE.txt)

Jpegger is an AutoCAD plugin that exports a grid of sheet areas from a drawing to numbered JPG files in one go. Select the first area once, say how many identical areas follow to the right and downwards, and the plugin plots each of them to PDF and converts it to a 300 DPI JPG.

## Features

- Dialog owned by AutoCAD, so it is not a separate window: it stays open while you pan and zoom the drawing and hides only while you pick the area
- Pick the first area with two corner clicks, the same area size is repeated across the drawing
- Live preview of the whole export grid with color-coded file numbers drawn in the model before anything is exported
- Batch export along X and Y with a configurable start number (`1.jpg`, `2.jpg`, ...)
- Portrait or landscape output is chosen from the shape of the selected area
- Progress in the AutoCAD status bar, optional opening of the output folder when done
- Settings are remembered between sessions
- Intermediate PDFs are removed after conversion
- Ribbon button on the Home tab and the `Jpegger` command
- Windows installer that detects installed AutoCAD versions and registers the plugin as an Autodesk application bundle
- Update check against GitHub Releases on startup

## Installation

1. Download `Jpegger_Setup.exe` from the [latest release](https://github.com/Asesu23/Jpegger/releases/latest).
2. Close AutoCAD and run the installer. It is not code-signed, so Windows SmartScreen may show a warning.
3. Start AutoCAD. The button is on the **Home** tab, in the **Jpegger** panel.

Requirements: Windows x64 and AutoCAD. The plugin is built against the AutoCAD 2022 .NET API.

## Usage

1. Click the **Export** button in the **Jpegger** panel or run the `Jpegger` command. The interface is currently in Russian.
2. Press `Указать область <`. The dialog hides, click two opposite corners of the first area, then the dialog returns with the area size.
3. Set the grid. A preview of every area with its file number appears in the drawing and follows your changes. The dialog stays open, so you can pan and zoom to check it:

   | Field | Meaning |
   | --- | --- |
   | `Колонок (X)` | How many areas to export along X, to the right |
   | `Рядов (Y)` | How many areas to export along Y, going down |
   | `Начать с №` | Number of the first file |

4. Choose the output folder (default: `C:\AutocadJpgResult\`) and press `Экспорт`. The button is enabled once an area is selected.

For every step to the right the plot window moves by the area width, for every step down by the area height. Files are numbered row by row. Each area is plotted with the built-in `AutoCAD PDF (General Documentation)` plotter on A4 paper, scaled to fit and centered.

## Building from source

Requirements: Visual Studio 2022 or Build Tools with .NET Framework 4.8 support, and [Inno Setup 6](https://jrsoftware.org/isinfo.php) for the installer. AutoCAD itself is not needed to compile, the API comes from the `AutoCAD.NET` NuGet package.

```
msbuild AutoCADPlugin\AutoCADPlugin.csproj /restore /p:Configuration=Release /p:Platform=x64
ISCC installer\stp.iss
```

The installer is written to `installer\Output\Jpegger_Setup.exe`.

## Releasing

Releases are built by GitHub Actions. A pushed tag like `v4.1.0` triggers a build that publishes a GitHub Release with `Jpegger_Setup.exe` and `version.txt`. The plugin reads `version.txt` to decide whether an update is available.

```
.\release.ps1 4.1.0 "feat: describe the change"
```

The script commits pending changes with the given message, pushes `master`, then creates and pushes the tag.

## Project layout

```
AutoCADPlugin/               plugin source: ribbon, commands, UI
installer/                   Inno Setup script and Autodesk bundle manifest
.github/workflows/           build and release workflow
release.ps1                  one-command release helper
```

## Tech stack

C#, .NET Framework 4.8, AutoCAD .NET API, WinForms, PdfiumViewer, Inno Setup, GitHub Actions.

## License

[MIT](LICENSE.txt)
