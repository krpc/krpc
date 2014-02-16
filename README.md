krpc
====

Remote Procedure Call server for Kerbal Space Program

A work-in-progress.


### Compiling the Plugin

Note: these instructions are for compiling the server plugin for KSP. See [python/README.md](python/README.md) for instruction on building the python client library.

#### Using the Makefile

1. Install the required dependencies. Libraries required by the plugin are already included in `lib` and the C# Protocol Buffers compiler is already included in `tools`, but you will need to install the .proto to .protobin Protocol Buffers compiler.

 On Linux, you should be able to use your package manager. For example using apt:
 
 `apt-get install protobuf-compiler`
 
 Or you can install it from source: https://code.google.com/p/protobuf/

2. Compile the plugin binaries. This will compile the protocol buffer .proto files into C# classes and compile the C# project(s) using the Mono compiler

 `$ make build`

3. Collect together all the binaries and other plugin files, and copy them into `$KSP_DIR/GameData/kRPC`

 `$ make install KSP_DIR=/home/djungelorm/KerbalSpaceProgram`

 Alternatively, you can have the plugin files copied into directory `dist` in the root of the source tree using `make dist`, then manually copy them over to your copy of KSP.

#### Using an IDE

1. Install all the required dependencies, as above.

2. Compile the protocol buffer .proto files into C# classes:

 `$ make protobuf`

3. Open the solution file (`src/kRPC.sln`) in your IDE.

### Testing

The kRPCTest project contains nunit unit tests.

The plugin can be built, installed (along with the TestingTools.dll to auto-load the default save) and run using:

`$ make ksp`

This speeds up manual building and testing significantly!

### Dependencies

 * protobuf (https://code.google.com/p/protobuf/)
 * protobuf-csharp-port (http://code.google.com/p/protobuf-csharp-port/)
