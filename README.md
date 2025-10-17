# MeshTopologyToolkit

MeshTopologyToolkit is a lightweight, extensible C# library designed for developers working with 3D mesh data.
It provides utilities for loading, saving, and processing meshes that use either single (unified) or multi-index topologies — enabling seamless conversion, optimization, and manipulation of complex 3D geometry.

✨ Key Features

### Dual topology support
Handle both single-index meshes (shared indices for positions, normals, UVs, etc.) and multi-index meshes (dedicated index sets per attribute).

### Flexible import/export
Read and write common mesh formats (planned extensibility for others).

### Mesh editing utilities
Recalculate normals, merge vertices, remap attributes, and convert between topology types.

### Lightweight and modular
Built with clean, dependency-minimal C# design — ideal for integration into rendering engines, game tools, or CAD pipelines.

### Extensible architecture
Plug in custom attribute types, codecs, or topology converters without modifying the core.
