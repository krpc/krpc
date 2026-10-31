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

The documentation needs LaTeX, Graphviz and Enchant, and the serial IO tests need ``socat``:

.. code-block:: bash

   sudo apt-get install socat graphviz libenchant-2-2 \
     latexmk texlive-latex-base texlive-latex-recommended texlive-fonts-recommended \
     texlive-latex-extra texlive-fonts-extra tex-gyre

The C and C++ clients build with an LLVM toolchain that Bazel downloads. Pass
``--config=system-llvm`` to build them with the system LLVM install named by
``llvm.toolchain_root`` in ``MODULE.bazel``, as the CI builds do.

Windows
^^^^^^^

Pass ``--config=windows`` to Bazel. The whole tree builds with MSVC, apart from the serial IO
tests, the C and C++ lint and the PDF documentation, which are skipped. A Windows machine needs
four settings first.

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

**A shell.** Bazel runs test and shell rules with bash. Under PowerShell, point ``BAZEL_SH`` at the
bash from Git for Windows:

.. code-block:: powershell

   $env:BAZEL_SH = 'C:\Program Files\Git\usr\bin\bash.exe'

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
* ``//doc:html``, ``//doc:pdf`` - the documentation.

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
