# MeshTopologyToolkit.TrimGenerator 

MeshTopologyToolkit.TrimGenerator is a command line utlity to generate textures and meshes inspired by Insomniac's Ultimate Trim.

To install the tool run the following command in a terminal:
```bash
dotnet tool install --global MeshTopologyToolkit.TrimGenerator
```

Then you can run the command from a terminal using short name:
```bash
>trimgen

Usage: trimgen [command]

MeshTopologyToolkit.TrimGenerator

Commands:
  normalmap    Generate trim normal map from trim height data.
  checkermap   Generate checker map.

Options:
  -h, --help    Show help message
  --version     Show version
```

## Normal map texture generation

Normal map texture generator for given trim heights.

```bash
Usage: trimgen normalmap [--trim-height <Int32>...] [--width <Int32>] [--width-in-units <Single>] [--bevel-width <Int32>] [--output <String>] [--help]

Generate trim normal map from trim height data.

Options:
  -t, --trim-height <Int32>...    Trim height in pixels (Required)
  -w, --width <Int32>             Texture width in pixels (Default: 1024)
  --width-in-units <Single>        (Default: 5)
  -b, --bevel-width <Int32>       Bevel width in pixels (Default: 8)
  -o, --output <String>           Output file name
  -h, --help                      Show help message
```

For example running
```bash
trimgen normalmap -t 4 -t 8 -t 16 -t 32 -t 64 -w 128
```

generates the following normal map:

![Normal Map Example](https://raw.githubusercontent.com/MeshTopologyToolkit/MeshTopologyToolkit/main/src/MeshTopologyToolkit.TrimGenerator/docs/images/normals.png)

## Box model generation

Box model generator generates a box model with given dimensions and trim heights.

```bash
Usage: trimgen box [--trim-height <Int32>...] [--width <Int32>] [--width-in-units <Single>] [--bevel-width <Int32>] [--size-x <Single>] [--size-y <Single>] [--size-z <Single>] [--max-deviation <Single>] [--normal-map] [--checker-map] [--albedo <String>] [--output <String>] [--help]

Generate trim normal map from trim height data.

Options:
  -t, --trim-height <Int32>...    Trim height in pixels (Required)
  -w, --width <Int32>             Texture width in pixels (Default: 1024)
  --width-in-units <Single>       Full trim width in world units (Default: 5)
  -b, --bevel-width <Int32>       Bevel width in pixels (Default: 8)
  --size-x <Single>               Box size along X dimention (Default: 1)
  --size-y <Single>               Box size along Y dimention (Default: 1)
  --size-z <Single>               Box size along Z dimention (Default: 1)
  -m, --max-deviation <Single>    Max deviation from the scale in percents (Default: 10)
  -n, --normal-map                Add normal map
  -c, --checker-map               Add checker map as base color (albedo)
  -a, --albedo <String>           Base color (albedo) texture file name
  -o, --output <String>           Output file name
  -h, --help                      Show help message
```

For example this command line builds the following box model:
```bash
trimgen box --trim-height 8 --trim-height 16 --trim-height 32 --trim-height 64 --trim-height 128 --trim-height 256 --trim-height 448 --width 1024 --bevel-width 8 --width-in-units 5 --size-x 0.5 --size-y 0.99 --size-z 1.99 -m 10 -o 3.glb --normal-map --checker-map
```

![Box Example](https://raw.githubusercontent.com/MeshTopologyToolkit/MeshTopologyToolkit/main/src/MeshTopologyToolkit.TrimGenerator/docs/images/box.png)

## Box palette generation

Box palette generator generates set of boxes with all combinations of available trim sizes matching exactly each trim size.

```bash
Usage: trimgen box-palette [--trim-height <Int32>...] [--width <Int32>] [--width-in-units <Single>] [--bevel-width <Int32>] [--normal-map] [--checker-map] [--albedo <String>] [--output <String>] [--help]

Generate palette of boxes that combine all trim sizes.

Options:
  -t, --trim-height <Int32>...    Trim height in pixels (Required)
  -w, --width <Int32>             Texture width in pixels (Default: 1024)
  --width-in-units <Single>       Full trim width in world units (Default: 5)
  -b, --bevel-width <Int32>       Bevel width in pixels (Default: 8)
  -n, --normal-map                Add normal map
  -c, --checker-map               Add checker map as base color (albedo)
  -a, --albedo <String>           Base color (albedo) texture file name
  -o, --output <String>           Output file name
  -h, --help                      Show help message
```

For example this command line builds the following box palette:
```bash
trimgen box-palette --trim-height 8 --trim-height 16 --trim-height 32 --trim-height 64 --trim-height 128 --trim-height 256 --trim-height 448 --width 1024 --bevel-width 8 --width-in-units 5 --normal-map --checker-map
```

![Box Palette Example](https://raw.githubusercontent.com/MeshTopologyToolkit/MeshTopologyToolkit/main/src/MeshTopologyToolkit.TrimGenerator/docs/images/box-palette.png)

## Checkerboard texture generation

Checkerboard is useful to test that your texture projection isn't distorted.

```bash
Usage: trimgen checkermap [--width <Int32>] [--height <Int32>] [--levels <Int32>] [--cell-size <Int32>] [--grid-levels <Int32>] [--output <String>] [--help]

Generate checker map.

Options:
  -w, --width <Int32>          Texture width in pixels (Default: 1024)
  -h, --height <Int32>         Texture height in pixels (Default: 1024)
  -s, --levels <Int32>         Maximum number of shades of gray, rounded to next power of 2 (Default: 8)
  -c, --cell-size <Int32>      Cell size in pixels (Default: 0)
  -g, --grid-levels <Int32>    Maximum number of grid levels, rounded to next power of 2 (Default: 0)
  -o, --output <String>        Output file name
  --help                       Show help message
```

