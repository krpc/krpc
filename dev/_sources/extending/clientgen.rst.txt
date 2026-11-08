.. _service-api-clientgen:

Generating Service Code for Static Clients
==========================================

Some of the client libraries dynamically construct the code necessary to interact with the server
when they connect. This means that these libraries will automatically pick up changes to service
code. Such client libraries include those for Python and Lua.
A generated Python module gives a service type hints and importable types, as described in
:ref:`python-other-mods`.

Other client libraries required code to be generated and compiled into them statically. They do not
automatically pick up changes to service code. Such client libraries include those for C++ and C#.

Code for these 'static' libraries is generated using the krpc-clientgen tool. This is provided as
part of the :doc:`krpctools </krpctools>` Python package. It can be
installed using pip:

``pip install krpctools``

You can then run the script from the command line:

.. code-block:: console

   $ krpc-clientgen --help

   usage: krpc-clientgen [-h] [-v] [-o OUTPUT] [--reference-dir DIR] [--ksp KSP]
                         [--include SERVICE=PATH] [--output-defs OUTPUT_DEFS]
                         language service input [input ...]

   Generate client source code for kRPC services.

   positional arguments:
     language              Language to generate (cnano, cpp, csharp, java,
                           python) or path to generator
     service               Name of service to generate
     input                 Path to service definition JSON file(s) or assembly
                           DLL(s). JSON files can give the definitions of the
                           services whose types a DLL's service uses.

   options:
     -h, --help            show this help message and exit
     -v, --version         show program's version number and exit
     -o, --output OUTPUT   Path to write source code to. If not specified, writes
                           source code to standard output.
     --reference-dir DIR   When reading from an assembly DLL, a directory to load
                           the assemblies the service references from, such as
                           the game's managed assemblies. Can be repeated.
     --ksp KSP             Deprecated: pass the KSP_Data/Managed directory of a
                           Kerbal Space Program install with --reference-dir.
     --include SERVICE=PATH
                           Include the generated code of another service, whose
                           types the service uses, from the given path. Required
                           by the C++ and C nano generators for each such
                           service.
     --output-defs OUTPUT_DEFS
                           When generating client code from a DLL, output the
                           service definitions to the given JSON file

Client code can be generated either directly from an assembly DLL containing the service, or from a
JSON file that has previously been generated from an assembly DLL (using the ``--output-defs``
flag).

.. note::

   Pass every assembly that declares part of the service. A mod adding an extension member to
   another service's class is generated alongside that service's assembly; see
   :ref:`service-api-extension-members`.

A service that uses a structure from another service also needs the definitions of that service.
Pass its JSON file from ``GameData/kRPC`` as another input, e.g. ``KRPC.StdLib.json``.

Generating client code from an assembly DLL needs the
`.NET 10 runtime <https://dotnet.microsoft.com/download/dotnet/10.0>`_, on Linux or Windows. It also
needs the assemblies the service references, such as the game's. Pass each directory that holds
them with ``--reference-dir``. For Kerbal Space Program this is ``KSP_Data/Managed``, or
``KSP_x64_Data/Managed`` on Windows. Generating from a JSON file needs neither, so it is more
portable.

``krpc-servicedefs`` generates the JSON file from the assemblies, and takes ``--reference-dir`` in
the same way:

.. code-block:: console

   $ krpc-servicedefs --reference-dir=/path/to/ksp/KSP_Data/Managed -o LaunchControl.json \
       LaunchControl LaunchControl.dll

Example
-------

The following demonstrates how to generate code for the C++ and C# clients to interact with the
LaunchControl service, given in an example previously.

krpc-clientgen expects to be passed the directory of the game's managed assemblies, the name of
the language to generate, the name of the service (from the :cs:attr:`KRPCService` attribute), a
path to the assembly containing the service and the path to write the generated code to.

For C++, run the following:

``krpc-clientgen --reference-dir=/path/to/ksp/KSP_Data/Managed -o launch_control.hpp cpp LaunchControl LaunchControl.dll``

To then use the LaunchControl service from C++, you need to link your code against the C++ client
library, and include `launch_control.hpp`. If the service uses types from another service, pass
``--include`` with the path of that service's header, so that `launch_control.hpp` includes it,
e.g. ``--include=SpaceCenter=krpc/services/space_center.hpp``.

For C#, run the following:

``krpc-clientgen --reference-dir=/path/to/ksp/KSP_Data/Managed -o LaunchControl.cs csharp LaunchControl LaunchControl.dll``

To then use the LaunchControl service from a C# client, you need to reference the `KRPC.Client.dll`
and include `LaunchControl.cs` in your project.
