Testing
=======

kRPC has two test suites. The first runs without the game, and the second drives a running copy of
KSP.

Tests without the Game
----------------------

Run the full suite with:

.. code-block:: bash

   bazel test //:test

The tests exercise each component without the game. Client tests talk to a ``TestServer``, which is
the server running outside of KSP. The serial IO tests need ``socat`` to pair the two
pseudo-terminals they communicate over.

.. note::

   ``bazel test //...`` does not work. Use ``bazel test //:test``.

Run one package's tests by naming its target, for example the Python client:

.. code-block:: bash

   bazel test //client/python:test

Each client's ``:test`` suite groups its tests by what they depend on:

* ``:unit`` - encoding and decoding values, without a server or a transport.
* ``:conformance`` - the conformance tests, described below.
* ``:client-tcpip`` - the client's own API against a ``TestServer``.
* ``:communication-*`` - for the C-nano client, each transport against a server the test listens
  on.
* ``:lint`` - the language's linters, where it has them.

``//:lint`` runs the linters for every language. Useful options for ``bazel test`` are:

* ``--cache_test_results=false`` runs tests whose results are cached.
* ``--test_output=all`` prints the log of every test, not just the failed ones.
* ``--test_output=streamed`` runs the tests one at a time and prints their output as it comes.

Conformance Tests
-----------------

The conformance tests in ``client/conformance/`` check every client library against the
:doc:`client specification </client-spec>`, over each transport. Each client has a testee in
``client/<language>/conformance/``, a small program built on the client that the test driver
controls. Run the tests for one client with:

.. code-block:: bash

   bazel test //client/python:conformance

``client/conformance/specgen.py`` generates the specification pages from the tests. The docstring
of a test is the text of its requirement.

When you change a client library, or add a feature that clients use, update the conformance tests:

* Add a test for any new or changed behavior. A test in ``client/conformance/tests/`` covers every
  client at once.
* When a test needs a command that no testee has, add it to ``client/conformance/testee.proto`` and
  to every testee.
* A client that fails a test is fixed, or lists the test and the reason in
  ``client/<language>/conformance/expected-failures.txt``. A test the language cannot carry out
  goes in ``client/<language>/conformance/unsupported.txt``. A line ending in ``@<protocol>``,
  such as ``@serialio``, applies only over that protocol.

A client's own tests cover its API, idioms and internals. Behavior that clients share belongs in
the conformance tests.

Tests in the Game
-----------------

These tests live in ``service/*/test/``, and need a copy of KSP. Set ``KSP_DIR`` to the install
directory, or pass ``--ksp-dir``:

.. code-block:: bash

   export KSP_DIR="$HOME/.local/share/Steam/steamapps/common/Kerbal Space Program"
   bazel run //:test-ingame

The command builds and installs the mod and the mods the tests need, launches KSP and runs the
tests. The test framework and the Python client come from the build, so a run picks up your
changes. Arguments after ``--`` go to pytest, to select a subset:

.. code-block:: bash

   bazel run //:test-ingame -- service/SpaceCenter/test/test_camera.py -v
   bazel run //:test-ingame -- -k test_camera_mode

To keep the game running between test runs, start it in another terminal:

.. code-block:: bash

   bazel run //:run-ksp -- --load-game=krpctest

Then pass ``--no-launch`` to run the tests against that game:

.. code-block:: bash

   bazel run //:test-ingame -- --no-launch service/SpaceCenter/test/test_camera.py -v

The tests leave a game they did not start unchanged, so it must already have the mods they need.
``//:run-ksp`` installs the mods named by ``--mods``, and pipes the game log to the terminal. Pass
``-- --help`` to list its other options, which load a save, switch vessel or launch a craft.

.. note::

   A test run that is killed, not interrupted, leaves the game running. Stop it with
   ``pkill -f KSP.x86_64``, or ``taskkill /f /im KSP_x64.exe`` on Windows.

Each release also publishes the ``krpctest`` package. Installed with ``pip``, it provides the
``krpc-install`` and ``krpc-run-ksp`` commands and a pytest plugin, for running the tests outside a
checkout of the repository.
