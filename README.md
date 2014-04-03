## kRPC

A Remote Procedure Call server for Kerbal Space Program.

A work-in-progress.

### Usage

 1. [Download the plugin files](http://github.com/djungelorm/krpc/releases) and extract them to your GameData directory. kRPC is a 'parts-less' plugin. The server configuration window will appear when piloting any vessel.

 2. Install the python client using pip:

    ```pip install krpc```

    Or, alternatively, by [downloading the source distribution](https://pypi.python.org/pypi/krpc), extracting and install it using:

    ```python setup.py install```

See [python/bin/example.py](python/bin/example.py) for an example script that sends commands to control the active vessel using kRPC.

See the [wiki](https://github.com/djungelorm/krpc/wiki) for more information.

### Compiling from Source

See [COMPILE.md](COMPILE.md) for details.
