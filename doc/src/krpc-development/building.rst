Building from Source
====================

kRPC builds with `Bazel <https://bazel.build>`_. The build is hermetic. It downloads the
toolchains for C#, Python, Java, C, C++, Lua and JavaScript, and a set of stub KSP assemblies
from the `ksp-lib <https://github.com/krpc/ksp-lib>`_ repository. You do not need Mono, the .NET
SDK, a JDK, Node.js or a copy of the game to build it.

Requirements
------------

Install Bazel through `bazelisk <https://github.com/bazelbuild/bazelisk>`_, which fetches the
Bazel version the repository pins. On Ubuntu:

.. code-block:: bash

   BAZELISK=https://github.com/bazelbuild/bazelisk/releases/latest/download
   wget "$BAZELISK/bazelisk-linux-amd64"
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

   reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock" ^
     /t REG_DWORD /f /v AllowDevelopmentWithoutDevLicense /d 1

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

   set VCTOOLS=Microsoft.VisualStudio.Workload.VCTools
   winget install --id Microsoft.VisualStudio.2022.BuildTools ^
     --override "--quiet --wait --add %VCTOOLS% --includeRecommended"

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
* ``//service/<name>``, such as ``//service/SpaceCenter`` - a service. ``service/registry.bzl``
  lists them.
* ``//client/python``, ``//client/csharp``, ``//client/cpp``, ``//client/cnano``,
  ``//client/java``, ``//client/js``, ``//client/lua`` - the client libraries.
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

Dependencies
------------

``tools/deps/deps.toml`` lists each third-party dependency once, with every file that pins its
version. ``tools/deps/deps.py`` reads it. To list the dependencies with newer releases:

.. code-block:: bash

   python3 tools/deps/deps.py outdated

To update them all, or a few by name:

.. code-block:: bash

   python3 tools/deps/deps.py update --all
   python3 tools/deps/deps.py update protobuf ruff

An update rewrites each pin and the hash of each downloaded archive. It then regenerates the
lock files the updated dependencies use, with the commands below. ``update --all`` regenerates
every lock file and moves its transitive pins to their newest releases. It skips the entries that
give a reason to hold, such as the Google.Protobuf version KSP's Unity needs.

``//tools/deps:test`` checks that every pin agrees with ``deps.toml``. It also scans the build
files, project files, package metadata and workflows for pins that ``deps.toml`` does not list,
so a new dependency, or a new pin of one, needs an entry. ``python3 tools/deps/deps.py mods``
lists the KSP mods in ``service/registry.bzl`` with newer GitHub releases. A mod update needs the
in-game tests, so it stays a hand edit.

The lock files regenerate with these commands, after a hand edit of the file named:

* ``tools/build/python/requirements.in``: ``bazel run //tools/build/python:requirements.update``
* ``tools/build/dotnet/paket.dependencies``: ``bazel run //tools/build/dotnet:update``
* ``maven.install`` in ``MODULE.bazel``: ``RULES_JVM_EXTERNAL_REPIN=1 bazel run @unpinned_maven//:pin``
* ``client/js/package.json``: ``bazel run -- @pnpm --dir $PWD/client/js install --lockfile-only``

A C# target depends on a NuGet package through its lowercase name, such as
``@paket.main//google.protobuf``. The target framework of the target selects the package's assets
and dependencies.

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
