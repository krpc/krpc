## Compiling kRPC from Source

### Compiling the Server Plugin

#### Using the Makefile

1. Install the required dependencies.

 You will need to install the MonoDevelop command line tool `mdtool`, Mono runtime and development libraries, the Protocol Buffers compiler, Inkscape and the `unzip` command line tool.

 On Linux, you should be able to use your package manager. For example using apt:

 `apt-get install mono-complete monodevelop protobuf-compiler inkscape unzip`

 Alternatively, you can install the Protocol Buffers compiler from source: https://code.google.com/p/protobuf/

 On Ubuntu 13.10, you may want to use a more recent version of MonoDevelop than that provided in the apt repositories.
 The latest version can be installed from a ppa as follows:

 ```
 add-apt-repository ppa:keks9n/monodevelop-latest
 apt-get update
 apt-get install monodevelop-latest
 ```

2. Compile the plugin binaries.

 Running `make build` compiles the protocol buffer .proto files into C# classes, and compiles kRPC.dll and kRPCSpaceCenter.dll using the MonoDevelop command line tool.

3. Collect together all the plugin files.

 Running `make dist` will collect together all of the plugin files that need to go into your GameData directory, and places them in a directory called `dist`.

4. Copy the plugin files to KSP's GameData directory.

 You can either manually copy the files from the `dist` directory to your GameData directory, or simply run:

 `make install KSP_DIR=/home/djungelorm/KerbalSpaceProgram`

#### Using an IDE

1. Install the Protocol Buffers compiler (as above).

 For example, on Linux, using your package manager:

 `apt-get install protobuf-compiler`

2. Compile the protocol buffer .proto files into C# classes:

 `make protobuf-csharp`

3. Open the solution file `src/kRPC.sln` in your IDE.

### Compiling the Python Client Library

See [python/README.md](python/README.md) for instructions.

### Running the Tests

The project contains unit tests for the server code (using NUnit) and for the python client code (using unittest). The tests can be run using `make test` (after compiling everything using `make build`).

Running the NUnit tests requires `nunit-console` to be installed. For example, on Linux, using your package manager:

`apt-get install nunit-console`
