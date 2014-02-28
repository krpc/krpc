Compiling kRPC from Source
==========================

## Compiling the Server Plugin

### Using the Makefile

1. Install the required dependencies. Libraries required by the plugin are already included in `lib` and the C# Protocol Buffers compiler is already included in `tools`. You will however need to install the MonoDevelop command line tool `mdtool`, Mono runtime and development libraries, the Protocol Buffers compiler and Inkscape to convert icons to png.

 On Linux, you should be able to use your package manager. For example using apt:

 `apt-get install mono-complete monodevelop protobuf-compiler inkscape`

 You can also install the Protocol Buffers compiler from source: https://code.google.com/p/protobuf/

 On Ubuntu 13.10, You may also want to use a more recent version of MonoDevelop than those provided in the apt repositories.
 The latest can be installed from a ppa as follows:

 ```
 add-apt-repository ppa:keks9n/monodevelop-latest
 apt-get update
 apt-get install monodevelop-latest
 ```

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

## Running the Tests

This project contains unit tests for the server code (written using NUnit) and for the python client code (using the unittest module). The tests can be run using `make test` (after compiling everything using `make build`).

Running the C# unit tests using the Makefile requires `nunit-console` to be installed, for example using apt:

`apt-get install nunit-console`

The plugin can also be built, installed into KSP (along with the TestingTools.dll to auto-load the default save) and run using:

`make ksp`

## Dependencies

 * protobuf (https://code.google.com/p/protobuf/)
 * protobuf-csharp-port (http://code.google.com/p/protobuf-csharp-port/)
