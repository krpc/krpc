Compiling kRPC
==============

The Server Plugin
-----------------

Using the Makefile
^^^^^^^^^^^^^^^^^^

1. Install the required dependencies.

   You will need to install the MonoDevelop command line tool, Mono runtime and
   development libraries, the Protocol Buffers compiler (at least version
   3.0.0-alpha-1) and Inkscape.

   On Linux, you should be able to use your package manager. For example on
   Ubuntu:

   ``apt-get install mono-complete monodevelop protobuf-compiler inkscape``

   Alternatively, you can install the Protocol Buffers compiler from source:
   https://code.google.com/p/protobuf

   On Ubuntu, you may want to use a more recent version of MonoDevelop than that
   provided in the apt repositories. The latest version can be installed from
   http://www.monodevelop.com/download/linux/#debian-ubuntu-and-derivatives

   Python dependencies may be installed using pip:
   
   ``pip install pyenchant sphinx sphinxcontrib-spelling sphinx_rtd_theme yaml``

2. Set the path to your KSP directory.

   The build scripts need to know where your KSP directory is. This is specified
   by the ``KSP_DIR`` environment variable. On Linux this can be set using:

   ``export KSP_DIR="/path/to/Kerbal\\ Space\\ Program"``

   Note: The path needs to be an absolute path and any spaces need to be escaped
   using ``\\``

3. Compile the plugin binaries.

   Running ``make build`` compiles the protocol buffer .proto files into C#
   classes, and compiles kRPC.dll and kRPCSpaceCenter.dll using the MonoDevelop
   command line tool.

4. Collect together all the plugin files.

   Running ``make dist`` will collect together all of the plugin files that need
   to go into your GameData directory, and places them in a directory called
   ``dist``.

5. Copy the plugin files to KSP's GameData directory.

   You can either manually copy the files from the `dist` directory to your
   GameData directory, or run the following command (passing the path to your
   KSP install directory):

   ``make install KSP_DIR="/home/djungelorm/Kerbal Space Program"``

Using an IDE
^^^^^^^^^^^^

1. Install the Protocol Buffers compiler (as above).

   For example, on Ubuntu Linux, using your package manager:

   ``apt-get install protobuf-compiler``

2. Compile the protocol buffer .proto files into C# classes:

   ``make protobuf-csharp``

3. Open the solution file ``kRPC.sln`` in your IDE.

The Python Client Library
-------------------------

The source code for the python client library can be found in the directory
``python``. You can install it from source as follows:

1. Install the Protocol Buffers python package (at least version 3.0.0-alpha-1)
   for example using pip:

   ``pip install protobuf``

   Or, alternatively, you can install it from source:
   https://code.google.com/p/protobuf

2. Compile the Protocol Buffers .proto files to .py files using:

   ``make protobuf``

3. Install the krpc python package:

   ``python setup.py install``

Running the Tests
^^^^^^^^^^^^^^^^^

The project contains unit tests for the server code (using NUnit) and for the
python client code (using unittest), and integration tests to test the
SpaceCenter API when a copy of KSP is running. The tests can be run using ``make
test`` (after compiling everything using ``make build``).

Running the NUnit tests requires ``nunit-console`` to be installed.
To install this on Ubuntu run:

``apt-get install nunit-console``
