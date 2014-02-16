krpc
====

Remote Procedure Call server for Kerbal Space Program

A work-in-progress.


### Compiling the Plugin

#### Using the Makefile

1. Install all the required dependencies:
 
 TODO: add details on how to install these
 * protobuf (https://code.google.com/p/protobuf/)
 * protobuf-csharp-port (http://code.google.com/p/protobuf-csharp-port/)

2. Compile the plugin binaries. This will compile the protocol buffer .proto files into C# classes and compile the C# project(s) using the Mono compiler

 `$ make build`

3. Collect together all the binaries and other plugin files, and installs them into `$KSP_DIR/GameData/kRPC`

 `$ make install KSP_DIR=/home/djungelorm/KerbalSpaceProgram`

 Alternatively, you can 'install' the plugin files into directory `dist` in the root of the source tree using `make dist`, then manually copy them over to your copy of KSP.

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
