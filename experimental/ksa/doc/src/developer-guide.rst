Developer Guide
===============

kRPC for KSA lives in the ``experimental/ksa`` folder of the `kRPC repository
<https://github.com/krpc/krpc/tree/main/experimental/ksa>`_. It builds with Bazel, as the rest
of the repository does, against KSA installed in ``KSA_DIR``. Set ``KSA_DIR`` to the absolute
path of the install, and ``STARMAP_DIR`` to the absolute path of a StarMap install. The game
runs only from the StarMap release pinned as ``starmap_version`` in ``config.bzl``. Without
``KSA_DIR``, the build skips the KSA targets.

Layout
------

The folders mirror those at the root of the repository:

* ``server/`` - the StarMap entry point, ``Addon.cs``, which holds all the StarMap and game
  lifecycle code, with the server windows and the kRPC menu. ``server/grpc/`` is the gRPC
  server.
* ``service/KSA/`` - the ``KSA`` service, its in-game tests, its service definitions and the
  client stubs.
* ``doc/`` - this documentation. The pages are in ``doc/src/``, and the templates of the
  generated API pages in ``doc/api/``.
* ``tools/build/`` - the ``@ksa`` repository of the KSA install, and the rules for the mod.
* ``tools/game/`` - the in-game test harness, and the install, run and test targets.
* ``tools/grpc/`` - the generators of the ``.proto`` files and the gRPC API pages.
* ``tools/release/`` - the readme of the release archive, and the script that publishes this
  documentation.
* ``tools/TestingTools/`` - the ``TestingTools`` service, its server settings and test
  vehicles, installed only for the tests.
* ``config.bzl`` - the versions of the mod, KSA and StarMap.

Building
--------

Build the mod and the client stubs, and install the mod into the KSA mods folder:

.. code-block:: bash

   bazel build //experimental/ksa/...
   bazel run //experimental/ksa/tools/game:install

The mod builds against the ``KRPC.Core`` of the repository. The build checks the service's
documentation, generates the stubs with ``krpc-clientgen``, and compiles the example scripts
against them. Without a KSA install, ``bazel build //...`` skips these targets.

The install target puts the mod into ``<Documents>/My Games/Kitten Space Agency/mods``, or
``KSA_MODS_DIR``, and enables it. ``-- --testing`` adds the ``TestingTools`` service.

Running
-------

Install the mod with ``TestingTools``, and launch KSA through StarMap:

.. code-block:: bash

   bazel run //experimental/ksa/tools/game:run-ksa

StarMap runs from ``STARMAP_DIR``. Its release targets Windows, so on Linux it runs with
``dotnet``, with the KSA folder on ``LD_LIBRARY_PATH``. The game's output goes to
``experimental/ksa/bin/ksa.log``.

KSA opens a configuration window before it loads. Untick "Always Show" in it so that the
tests run unattended.

Server
------

The server runs once per frame, in a Harmony prefix on ``Universe.ExecuteNextVehicleSolvers``.
That point is on the main thread, after the frame's solver results and input are applied. The
vehicle solver runs on a worker thread from there until the next frame.

Testing
-------

The tests that need no game are the linters, type checks and docs checks:

.. code-block:: bash

   bazel test //experimental/ksa:test

``//:test`` and CI run them too. Without ``KSA_DIR``, the suite skips the tests that compile
against KSA.

The in-game tests are Python scripts that drive a live game, run with pytest. Arguments after
``--`` go to pytest:

.. code-block:: bash

   bazel run //experimental/ksa/tools/game:test-ksa                       # launch KSA, run every test
   bazel run //experimental/ksa/tools/game:test-ksa -- -k test_rename     # by name
   bazel run //experimental/ksa/tools/game:test-ksa -- -m "not slow"      # skip the flight to orbit
   bazel run //experimental/ksa/tools/game:run-ksa                        # run KSA on its own
   bazel run //experimental/ksa/tools/game:test-ksa -- --no-launch        # against that game

The harness is the one the kRPC in-game tests use. It installs the mod with the
``TestingTools`` service, launches KSA through StarMap when no server is running, and quits
it at the end. Each test class starts from a save of the game's start state.

* A launched game is saved as ``krpctest`` once it has loaded. ``--no-launch`` needs that
  save from an earlier run.
* ``test_tutorials.py`` flies the launch into orbit tutorial, which takes about eight minutes.
  It is marked ``slow``.
* ``TestingTools.RegisterVehicle`` adds a vehicle from ``tools/TestingTools/vehicles`` to the
  game's default vehicles.

Documentation
-------------

Build this documentation, or serve it on http://localhost:8080 with rebuild on edit:

.. code-block:: bash

   bazel build //experimental/ksa/doc:html
   bazel run //experimental/ksa/doc:serve

``krpc-docgen`` generates the API pages from the service definitions. ``doc/order.txt`` orders
the members, and lists every one of them.

``tools/release/publish-docs.sh`` commits the documentation to the ``gh-pages`` branch, under
``experimental/ksa/``. ``--push`` pushes the commit.

Releasing
---------

``bazel build //experimental/ksa:release`` builds the release archive. It holds the ``kRPC``
mod folder, the client stubs, a readme and the licenses. The mod folder
sits at the top, with its own copy of the licenses, so that the Borea mod manager installs it.
The mod is under the LGPL v3, as ``KRPC.Core`` is.
