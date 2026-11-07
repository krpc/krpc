Building from Source
====================

kRPC builds with `Bazel <https://bazel.build>`_. The build is hermetic. It downloads the
toolchains for C#, Python, Java, C, C++ and Lua, and a set of stub KSP assemblies from the
`ksp-lib <https://github.com/krpc/ksp-lib>`_ repository. You do not need Mono, the .NET SDK, a JDK
or a copy of the game to build it.

Requirements
------------

Install Bazel through `bazelisk <https://github.com/bazelbuild/bazelisk>`_, which fetches the
Bazel version the repository pins. On Ubuntu:

.. code-block:: bash

   wget https://github.com/bazelbuild/bazelisk/releases/latest/download/bazelisk-linux-amd64
   chmod +x bazelisk-linux-amd64
   sudo mv bazelisk-linux-amd64 /usr/local/bin/bazel

The serial IO tests need ``socat``:

.. code-block:: bash

   sudo apt-get install socat

The C and C++ clients build with an LLVM toolchain that Bazel downloads. Pass
``--config=system-llvm`` to build them with the system LLVM install named by
``llvm.toolchain_root`` in ``MODULE.bazel``, as the CI builds do.

Windows
^^^^^^^

Bazel applies the ``windows`` configuration on a Windows host. The whole tree builds with MSVC.
Bazel skips the targets that need Linux: the serial IO tests, clang-tidy and the Python lock file
check. A Windows machine needs these first.

**Symlinks.** Bazel runs with ``--windows_enable_symlinks`` and ``--enable_runfiles``, and creating a
symlink needs Developer Mode. Turn it on under Settings > System > For developers, or from an
elevated prompt:

.. code-block:: bat

   reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock" /t REG_DWORD /f ^
     /v AllowDevelopmentWithoutDevLicense /d 1

**Long paths.** The virtual environment rules_python builds for its tools has paths over the
default limit of 260 characters. A path over the limit fails with a symlink privilege error. Raise
the limit from an elevated prompt:

.. code-block:: bat

   reg add "HKLM\SYSTEM\CurrentControlSet\Control\FileSystem" /t REG_DWORD /f ^
     /v LongPathsEnabled /d 1

Both settings apply to processes started afterwards, so run ``bazel shutdown`` before building
again.

**MSVC.** The C and C++ clients build with Visual Studio Build Tools, with the C++ workload and the
Windows SDK:

.. code-block:: bat

   winget install --id Microsoft.VisualStudio.2022.BuildTools ^
     --override "--quiet --wait --add Microsoft.VisualStudio.Workload.VCTools --includeRecommended"

Bazel caches the compiler it finds. If Bazel ran before the install, set ``BAZEL_VC`` to
``...\BuildTools\VC`` to make it search again.

**A shell.** Bazel runs shell rules with the bash from Git for Windows, installed at
``C:\Program Files\Git``. For Git installed elsewhere, set the bash path in
``%USERPROFILE%\.bazelrc``:

.. code-block:: text

   build:windows "--shell_executable=D:/Git/usr/bin/bash.exe"
   common:windows "--repo_env=BAZEL_SH=D:/Git/usr/bin/bash.exe"

Under Git Bash, set ``MSYS2_ARG_CONV_EXCL="*"``. Without it, Git Bash rewrites ``//core:test`` into
the path ``/core:test``.

Building
--------

Build the mod release archive with:

.. code-block:: bash

   bazel build //:krpc

The archive, holding the ``GameData`` directory, is written to ``bazel-bin/krpc-<version>.zip``.
Build outputs are in ``bazel-bin``, and ``bazel build //...`` builds everything. Each component has
its own target:

* ``//server`` - the server plugin.
* ``//service/SpaceCenter``, ``//service/Debug``, ``//service/Drawing``, ``//service/UI``,
  ``//service/DockingCamera``, ``//service/InfernalRobotics``, ``//service/KerbalAlarmClock``,
  ``//service/LiDAR``, ``//service/RemoteTech`` - the services.
* ``//client/python``, ``//client/csharp``, ``//client/cpp``, ``//client/cnano``,
  ``//client/java``, ``//client/lua`` - the client libraries.
* ``//client/python:python_base`` - the Python client without its generated stubs.
* ``//protobuf:csharp``, ``//protobuf:java``, ``//protobuf:lua``, ``//protobuf:python`` - the
  protobuf code for each language.
* ``//doc:html`` - the documentation.

Serve the documentation locally with:

.. code-block:: bash

   bazel run //doc:serve

It rebuilds as you edit the sources, and opens a browser at ``http://localhost:8080``. Pass
``-- --no-browser`` to skip the browser, or ``-- --port <PORT>`` to change the port.

Useful options for ``bazel build`` and ``bazel test`` are:

* ``--subcommands`` prints the commands being run.
* ``--jobs 1`` runs one action at a time.

``bazel fetch //...`` downloads every dependency up front, for working offline. ``bazel clean``
deletes the build outputs, and ``bazel clean --expunge`` also deletes the downloaded dependencies.

NuGet Packages
--------------

The NuGet packages used by the C# targets are listed in ``tools/build/dotnet/paket.dependencies``.
After editing that file, regenerate the lock file and the package lists with:

.. code-block:: bash

   bazel run //tools/build/dotnet:update

A target depends on a package through its lowercase name, such as ``@paket.main//google.protobuf``.
The target framework of the target selects the package's assets and dependencies.

A few versions are written by hand, and an update must change them too. Google.Protobuf's
version appears in a repository name in ``MODULE.bazel``, in the ``Google.Protobuf.netstandard2.0``
import in ``tools/build/dotnet/BUILD.bazel``, and in the dependencies of the ``KRPC.Client``
NuGet package. ``//tools/build/dotnet:versions-test`` checks each of them against the lock file.

C# IDEs
-------

The repository root has a solution file, ``kRPC.sln``, for Visual Studio, Rider or a similar IDE.
It needs some generated C# sources, which this command builds:

.. code-block:: bash

   bazel build //:csproj

Each `release <https://github.com/krpc/krpc/releases>`_ also has a
``krpc-genfiles-<version>.zip`` holding these files. Extract it over the source tree to use the
solution without Bazel.

Docker
------

The ``buildenv`` Docker image has every requirement installed, and is what the CI builds run in.
Start a container from it, as the ``runner`` user the image expects:

.. code-block:: bash

   docker pull ghcr.io/krpc/buildenv:latest
   docker run --user runner:runner -it ghcr.io/krpc/buildenv:latest

Then clone the repository, build it and run the tests:

.. code-block:: bash

   git clone https://github.com/krpc/krpc.git
   cd krpc
   bazel build //...
   bazel test //:test
