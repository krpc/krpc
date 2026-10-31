Benchmarks
==========

The benchmark suite in ``tools/benchmarks/`` measures the performance of the server and the
clients:

.. code-block:: bash

   bazel run //tools/benchmarks:testserver  # the server, without the game
   bazel run //tools/benchmarks:python      # one client, against a TestServer
   bazel run //tools/benchmarks:clients     # every client, in one table
   bazel run //tools/benchmarks:server      # the server, in the game

There is a client target for each language: ``:python``, ``:csharp``, ``:cpp``, ``:cnano``,
``:java`` and ``:lua``.

Comparing Results
-----------------

Each target takes ``--json PATH``, which writes the results to a file. ``:compare`` prints two
result files side by side, and marks the changes large enough to matter:

.. code-block:: bash

   bazel run //tools/benchmarks:compare -- before.json after.json

The threshold for a change depends on the noise of the machine. To measure it, run the suite twice
on the same code, and pass both results as the noise floor:

.. code-block:: bash

   bazel run //tools/benchmarks:compare -- before.json after.json --noise a.json b.json

The Targets
-----------

``:testserver`` measures the server's cost per remote procedure call: decoding the arguments,
dispatch, the procedure and encoding the result. It runs against a ``TestServer``, so it measures a
change to ``core/`` or ``server/`` without the game.

The client targets measure a client's cost: the round trip for a call, and calls carrying
collections. Each client is timed from inside itself, so each target is a program in its own
language. The report has one block per transport, TCP/IP and local socket.

The C-nano target uses a socket, not a serial port. The server polls a serial port at an interval
longer than every measured case, so a serial port would measure the poll. The transport is a
compile-time choice, so there is one program per transport, ``//client/cnano:benchmark`` and
``//client/cnano:benchmark-localsocket``.

The client targets run the server with ``--no-frame-pacing``, so its update loop runs as fast as
it can. A paced server adds the rest of each update to a round trip. It also serves a client that
is slower than its receive timeout once per update, which measures 16.7 ms and not the client.

``:clients`` runs every client in turn, each with its own server, and reports one column per
language. Its JSON file holds every client's results, and ``:compare`` shows it as one block per
client.

``:server`` measures calls to ``SpaceCenter`` procedures in a running game, object access, and a
stream over a few hundred parts. It is a pytest suite, so arguments go to pytest:

.. code-block:: bash

   bazel run //tools/benchmarks:server -- -k stream
