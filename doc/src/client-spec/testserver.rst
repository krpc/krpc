Test Server
===========

The TestServer is a kRPC server that runs without the game. The conformance tests run against it,
and you can test your own client against it while you write it.

Running the Server
------------------

The TestServer ships in the :doc:`krpctools </krpctools>` Python package.
Install it, then start the server on port 50000:

.. code-block:: sh

   pip install krpctools
   krpc-testserver --tcpip-port=50000

The server runs on Linux and Windows, and needs the
`.NET 10 runtime <https://dotnet.microsoft.com/download/dotnet/10.0>`_. It prints
``Server started successfully`` once it is listening. Without ``--tcpip-port`` it listens on a
free port, and prints it as ``tcpip_port = <port>``. Press :kbd:`Ctrl+C` to stop it.

Protocols
---------

``--type`` picks the :doc:`communication protocol </communication-protocols>`: ``protobuf`` for
TCP/IP (the default), ``localsocket``, ``websockets`` or ``serialio``. Here the server takes
clients over websockets and TCP/IP at the same time:

.. code-block:: sh

   krpc-testserver --type=websockets,protobuf --websockets-port=50001 --tcpip-port=50000

``--localsocket-path`` names the socket of a local socket server, and ``--serialio-port`` the
port of a serial IO server. ``krpc-testserver --help`` lists every option, including the bind
address, the serial port settings and the log level.

Services
--------

The server has the services below. The ``KRPC.GetServices`` procedure returns their definitions,
with documentation.

.. list-table::
   :header-rows: 1

   * - Service
     - Purpose
   * - ``KRPC``
     - The main kRPC service, which every server has.
   * - ``StdLib``
     - Mathematics and printing for :doc:`server side functions </client-spec/functions>`.
   * - ``TestService``, ``TestService2``
     - Procedures, classes, enumerations and exceptions of every kind, for testing how a client
       encodes and decodes values and calls procedures.
   * - ``ConformanceService``
     - Server state that the conformance tests control.
   * - ``DeprecatedService``
     - A deprecated service, for testing how a client signals the use of its members.
   * - ``Benchmark``
     - Procedures for measuring the cost of a call.
