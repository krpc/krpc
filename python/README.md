kRPC Python Client
==================

krpc.py provides a client interface for the kRPC server.

### Installation

1. Install using:

   `python setup.py install`

   Or, alternatively, using pip:

   `pip install krpc`

2. The python client library requires the Protocol Buffers python package.
   The easiest way to install it is using pip:

   `pip install protobuf`

   Or you can install it from source: https://code.google.com/p/protobuf/

### Usage

`import krpc` from your python scripts and away you go! See [example.py](example.py) for an example.

### Compiling from source

1. Compile the .proto files to .py files using:

 `$ make protobuf`
