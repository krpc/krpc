Benchmarks
==========

The benchmark suite in ``tools/benchmarks/`` measures the performance of the server and the
clients:

.. code-block:: bash

   bazel run //tools/benchmarks:testserver  # the server, without the game
   bazel run //tools/benchmarks:update-loop # the update loop, without the game
   bazel run //tools/benchmarks:python      # one client, against a TestServer
   bazel run //tools/benchmarks:http        # the HTTP server, against a TestServer
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

   bazel run //tools/benchmarks:compare -- before.json after.json \
     --noise a.json b.json

The Targets
-----------

``:testserver`` measures the server's cost per remote procedure call: decoding the arguments,
dispatch, the procedure and encoding the result. It runs against a ``TestServer``, so it measures a
change to ``core/`` or ``server/`` without the game. ``--steps`` times each step of a call on its
own, per type of argument and result: parsing the request, decoding the arguments, the call,
encoding the result and serializing the response:

.. code-block:: bash

   bazel run //tools/benchmarks:testserver -- --steps

``:update-loop`` measures the server's update loop: the calls a second that clients of different
shapes get, and what the loop costs the frame. It runs a ``TestServer`` that models a game's frame
loop, with fixed steps at 50 a second, simulated work in each step and each frame, and a frame
limit of 60 frames a second. ``--game`` picks a light or a heavy game, ``--shape`` the client
shapes, and ``--preset`` the server's rate settings, Balanced by default:

.. code-block:: bash

   bazel run //tools/benchmarks:update-loop -- --game light --shape synchronous

Each client runs in its own process, and counts its own calls. The report has one block per game
and shape, with a row per point and a column per figure. It also reports how long the ways of
waiting take on the server's runtime.

The client targets measure a client's cost: the round trip for a call, and calls carrying
collections. Each client is timed from inside itself, so each target is a program in its own
language. The report has one block per transport, TCP/IP and local socket.

The C-nano target uses a socket, as the other clients do, so that its results compare with
theirs.

The client targets run the server with ``--no-frame-pacing``, so its update loop runs as fast as
it can. A paced server adds the rest of each update to a round trip. It also serves a client that
is slower than its max call wait once per update, which measures 16.7 ms and not the client.

``:http`` measures the same round trips over the HTTP server, with Python's ``http.client``, and
a round trip on a new connection for each call, as ``curl`` makes. Read against the Python client,
it compares the two protocols from one language.

``:clients`` runs every client in turn, each with its own server, and reports one column per
language, with HTTP in the TCP/IP block. Its JSON file holds every client's results, and
``:compare`` shows it as one block per client.

``:server`` measures calls to ``SpaceCenter`` procedures in a running game, object access, and a
stream over a few hundred parts. It is a pytest suite, so arguments go to pytest:

.. code-block:: bash

   bazel run //tools/benchmarks:server -- -k stream

``test_update_loop.py`` runs the update loop shapes of ``:update-loop`` in the game, to check the
model against it.

``test_http.py`` measures round trips in the game, from the Python client and over HTTP.

``test_steps.py`` times the steps of ``--steps`` in the game, with calls on the reference craft
added.
