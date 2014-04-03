kRPC Python Client
==================

### Installation

The easiest way to install the krpc python package is using pip:

   ```pip install krpc```

This will install the latest version of the package and all it's dependencies, from https://pypi.python.org/pypi/krpc

### Usage

`import krpc` from your python scripts and away you go! See [bin/example.py](bin/example.py) for an example.

### Installation from Source

Alternatively, you can install it from source as follows:

1. Install the Protocol Buffers python package, for example using pip:

   ```pip install protobuf```

   Or, alternatively, you can install it from source: https://code.google.com/p/protobuf

2. Compile the Protocol Buffers .proto files to .py files using:

   ```make protobuf```

2. Install the krpc python package:

   `python setup.py install`
