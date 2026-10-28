Developer Guide
===============

kRPC for KSA lives in the ``experimental/ksa`` folder of the `kRPC repository
<https://github.com/krpc/krpc/tree/main/experimental/ksa>`_. Its scripts build the mod, the
client stubs and this documentation, and run the tests in the game. They need Python 3, the
.NET 10 SDK, ``zip``, and KSA installed in ``KSA_DIR``, which defaults to ``../KSA`` beside the
repository.

Building
--------

Build the mod and the client stubs:

.. code-block:: bash

   experimental/ksa/build.sh            # build into bin/kRPC
   experimental/ksa/build.sh --install  # also install it into the KSA mods folder

The build uses the kRPC @KRPC_VERSION@ release from PyPI. Its ``krpctools`` package provides
``KRPC.Core.dll``, the ``ServiceDefinitions`` tool, which checks the service's documentation,
and ``krpc-clientgen``, which generates the stubs.

Testing
-------

The tests are Python scripts that drive a live game, run with pytest:

.. code-block:: bash

   experimental/ksa/test.sh                        # launch KSA, run every test, quit
   experimental/ksa/test.sh test/test_orbit.py -v  # one file
   experimental/ksa/test.sh -m "not slow"          # skip the test that flies to orbit
   experimental/ksa/test.sh --no-launch            # against a game started with run-ksa.sh

The harness launches KSA through StarMap when no server is running, and quits it at the end.
Each test class starts from a save of the game's start state. The ``TestingTools`` service,
installed only for the tests, saves, loads and quits the game.

Documentation
-------------

Build this documentation into ``bin/docs``, after ``build.sh``:

.. code-block:: bash

   experimental/ksa/docs.sh           # build the HTML
   experimental/ksa/docs.sh --serve   # also serve it on http://127.0.0.1:8000

``krpc-docgen`` generates the API pages from the service definitions. ``doc/order.txt`` orders
the members, and lists every one of them.

Releasing
---------

``release.sh`` builds the release archive into ``bin/release``, with its checksum. The archive
holds the ``kRPC`` mod folder, the client stubs, the example scripts, a readme and the
licenses. The mod folder sits at the top, with its own copy of the licenses, so that the Borea
mod manager installs it.
