Compiling kRPC from Source
==========================

## Compiling the Server Plugin

### Using the Makefile

1. Install the required dependencies. Libraries required by the plugin are already included in `lib` and the C# Protocol Buffers compiler is already included in `tools`. You will however need to install the MonoDevelop command line tool `mdtool`, Mono runtime and development libraries, the Protocol Buffers compiler and Inkscape to convert icons to png.

 On Linux, you should be able to use your package manager. For example using apt:

 `apt-get install mono-complete monodevelop protobuf-compiler inkscape`

 You can also install the Protocol Buffers compiler from source: https://code.google.com/p/protobuf/

2. Compile the plugin binaries. The following command compiles the protocol buffer .proto files into C# classes and compiles kRPC.dll and kRPCServices.dll using the MonoDevelop command line tool:

 `make build`

3. Collect together all the plugin files, and copy them into `$KSP_DIR/GameData/kRPC`. This also installs the Toolbar plugin to `$KSP_DIR/GameData/000_Toolbar` (although this can be removed as kRPC will work without it).

 `make install KSP_DIR=/home/djungelorm/KerbalSpaceProgram`

 Alternatively, you can have the plugin files copied into directory `dist` in the root of the source tree using `make dist`, then manually copy them over to your GameData directory.

### Using an IDE

1. Install the Protocol Buffer compiler (as above).

2. Compile the protocol buffer .proto files into C# classes:

 `make protobuf-csharp`

3. Open the solution file `src/kRPC.sln` in your IDE.

## Compiling the Python Client Library

See [python/README.md](python/README.md) for instructions.

## Testing

The kRPCTest project contains nunit unit tests. They can be built and run using `make test`.

The plugin can also be built, installed (along with the TestingTools.dll to auto-load the default save) and run using:

`$ make ksp`

This speeds up manual building and testing significantly!

## Dependencies

 * protobuf (https://code.google.com/p/protobuf/)
 * protobuf-csharp-port (http://code.google.com/p/protobuf-csharp-port/)
