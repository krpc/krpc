kRPC
====

A Remote Procedure Call server for Kerbal Space Program.

A work-in-progress.

### Usage

[Download the plugin files](http://github.com/djungelorm/krpc/releases) and copy them to your GameData directory. kRPC is a 'parts-less' plugin. The server configuration window will appear when piloting any vessel.

See [python/example.py](python/example.py) for an example script that sends commands to control the active vessel using kRPC.

### The Basics

kRPC consists of two components: a server and a client. The server provides 'Procedures' that the client can run (hence the name 'Remote Procedure Call', or RPC for short). These procedures are arranged in groups called 'Services', to keep things organized.

The server is a plugin that runs inside KSP. The core functionality is provided by kRPC.dll. This includes the in-game user interface, the client-server communication layer and the 'Services API' (which other plugins can use to add Services and Procedures to the server). kRPCServices.dll is included with kRPC, and provides a collection of standard services, including basic autopiloting and querying flight data.

The client runs outside of KSP. This gives you the freedom to run scripts in whatever environment you want. The client communicates with the server to run procedures. kRPC includes a Python client library ([python/krpc.py](python/krpc.py)), that provides all the functionality needed to write a Python script to execute Remote Procedure Calls. See [python/example.py](python/example.py) for an example script.

### Compiling from Source

See [COMPILE.md](COMPILE.md) for instructions.
