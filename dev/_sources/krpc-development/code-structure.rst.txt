Code Structure
==============

kRPC consists of a server that runs in the game, service DLLs that add RPCs to it, a client library
for each language, the documentation and tools. The `repository <https://github.com/krpc/krpc>`_
is laid out as follows:

* ``server/`` - the server plugin that runs in the game.
* ``service/`` - the services, one directory each, which add RPCs to the server.
* ``core/`` - the C# library shared by the server and services.
* ``protobuf/`` - the schema for the communication protocol.
* ``client/`` - the client libraries, one directory per language.
* ``client/conformance/`` - the conformance tests, which check every client library against the
  :doc:`client specification </client-spec>`.
* ``doc/`` - the sources for this documentation. The API reference for each client is generated
  by the ``docgen`` tool.
* ``media/`` - the logos and banners, and the script that generates them.
* ``tools/TestServer/`` - a server that runs without the game, for testing clients.
* ``tools/ServiceDefinitions/`` - generates a service definition file, in JSON, from a service
  DLL.
* ``tools/krpctools/`` - a Python package of tools for client libraries: ``clientgen`` generates
  client stubs and ``docgen`` generates the API documentation. ``krpctools.game`` runs tests in
  the game.
* ``tools/game/`` - the runners for the tests in the game, which build kRPC from the sources.
* ``tools/TestingTools/`` - a service for the tests, with RPCs such as teleporting a vessel into
  orbit.
* ``tools/benchmarks/`` - the benchmark suite, described in :doc:`benchmarks`.
* ``tools/buildenv/`` - the Docker image the CI builds run in.
* ``tools/release/`` - the release scripts.
* ``tools/build/`` - the Bazel rules and toolchains.
