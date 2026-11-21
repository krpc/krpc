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
* ``:compiler-test`` - for the JavaScript client, the function compiler against a ``TestServer``.
* ``:communication-*`` - for the C-nano client, each transport against a server the test listens
  on.
* ``:lint`` - the language's linters, where it has them.

``//client/http:test`` tests the :doc:`HTTP server </http>` against a ``TestServer``, through a
minimal Python client in ``client/http/``.

``//:test`` includes each language's linters. ``//doc:linkcheck`` checks the external links in
the documentation. It needs network access and is in no suite. Run it by name, as the release
checks do.

``//doc:lint`` checks the formatting of the documentation examples at 76 columns, the width of a
rendered code block. The C# and Java examples are wrapped to that width by hand.

Useful options for ``bazel test`` are:

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
directory, or pass ``--game-dir``:

.. code-block:: bash

   STEAM_APPS="$HOME/.local/share/Steam/steamapps/common"
   export KSP_DIR="$STEAM_APPS/Kerbal Space Program"
   bazel run //tools/game:test-ksp

The command builds and installs the mod and the mods the tests need, launches KSP and runs the
tests. The test framework, :doc:`krpctools.game </extending/testing>`, and the Python client come
from the build, so a run picks up your changes. Arguments after ``--`` go to pytest, to select a
subset:

.. code-block:: bash

   bazel run //tools/game:test-ksp -- \
     service/SpaceCenter/test/test_camera.py -v
   bazel run //tools/game:test-ksp -- -k test_camera_mode

To keep the game running between test runs, start it in another terminal:

.. code-block:: bash

   bazel run //tools/game:run-ksp -- --load-game=krpctest

Then pass ``--no-launch`` to run the tests against that game:

.. code-block:: bash

   bazel run //tools/game:test-ksp -- --no-launch \
     service/SpaceCenter/test/test_camera.py -v

The tests leave a game they did not start unchanged, so it must already have the mods they need.
``//tools/game:run-ksp`` installs the mods named by ``--mods``, and pipes the game log to the
terminal. Pass ``-- --help`` to list its other options, which load a save, switch vessel or launch
a craft.

The third-party mods the tests install are pinned in ``service/registry.bzl``. The
:doc:`mods guide </user-guide/mods>` lists the pinned versions as the versions kRPC is tested
with.
To test against a local build of one, pass ``--mod NAME PATH`` to either command.

.. note::

   A test run that is killed, not interrupted, leaves the game running. Stop it with
   ``pkill -f KSP.x86_64``, or ``taskkill /f /im KSP_x64.exe`` on Windows.

Documentation Examples in the Game
----------------------------------

The tests in ``doc/test/`` run the example programs in ``doc/src/scripts/`` against the game. Each
test is marked ``example`` and its language, and the long flights ``slow``. Select them with
``-m``:

.. code-block:: bash

   bazel run //tools/game:test-ksp -- doc/test -m "python and not slow"

A run with no path runs them too, slow flights included, which takes several hours. Pass
``-m "not example"`` to run only the tests in ``service/*/test/``. The examples run on Linux
only, and are skipped on Windows.

An example passes when it exits with code 0, or for an example that runs until stopped, when it
is still running and printing after a few seconds. It must write nothing to its error output.
Some examples also check the game afterward, such as a launch reaching orbit.

``doc/test/examples.py`` declares how each example runs. An entry names the example by its path
under ``doc/src/scripts/`` without the extension, and covers every language that has it. The
client guide folders share the ``client/`` prefix:

.. code-block:: python

   Example("client/Streaming1", "orbit", run=Loop())
   Example(
       "tutorials/parts/DeployParachutes",
       "parts-tree-fresh",
       check=checks.parachutes_deployed,
   )

The second argument names one of the game setups in ``SETUPS``, such as a vessel in orbit, or a
craft on the launch pad.

``doc/test/unsupported.txt`` lists the examples the tests cannot run. Each line names the
language, or ``*`` for every language, and the example, with the reason as a comment.
``//doc:test`` checks that every example is declared or listed, so a new example needs an entry.

The parts that do not depend on the game are in ``tools/doc_examples/``: running an example in
each language, the test classes, the coverage check and the per-example Bazel macros. The kRPC
for KSA examples use them too, with their own manifest and setups.
