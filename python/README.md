kRPC Python Client
==================

krpc.py provides a client interface for the kRPC server.

### Usage

1. Install dependencies.

 The python client library requires the Protocol Buffers python package.
 The easiest way to install it is using pip:

 `pip install protobuf`

 Or you can install it from source: https://code.google.com/p/protobuf/

2. Then you simply import krpc from your python scripts and away you go! See [example.py](example.py) for an example.

### Compiling from source

1. Compile the .proto files to .py files using:

 `$ make protobuf`
