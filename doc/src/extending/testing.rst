In-Game Tests
=============

:doc:`krpctools </krpctools>` runs tests against a live copy of KSP. ``krpc-game`` installs kRPC
and its test add-on into a KSP install and runs the game. In a pytest suite, the game runs with the
mods each test class requires. Both run on Linux and Windows.

The Game
--------

Set ``KSP_DIR`` to a KSP install, or pass ``--game-dir``, then run:

.. code-block:: sh

   krpc-game run --load-game=krpctest --mods=FAR

This installs the kRPC release your krpctools was built with, the test add-on and the FAR mod. It
then launches KSP, loads the ``krpctest`` save, created blank on the first run, and prints the
game's kRPC log lines. ``krpc-game stop``, from another terminal, stops the game.
``krpc-game install`` installs without launching.

To test against another kRPC release, install the krpctools of the same version. ``--krpc``
installs a kRPC archive such as a local build. The bundled test add-on is built against the
release, so it may fail to load with another build.

``--mods`` names the supported mods to install, and each one brings the mods it needs. The install
removes the other supported mods. Run ``krpc-game run --help`` for the options that load a save,
switch vessel or launch a craft.

.. note::

   The tests need GameData to hold only the stock game, kRPC, ModuleManager and the requested
   mods. An install with anything else fails, unless you pass ``--skip-install-check``.

Test Suites
-----------

A test suite chooses its game in ``conftest.py``. This one tests the fictional Heatshields mod,
built into ``build/Heatshields.zip`` beside it:

.. literalinclude:: /scripts/extending/testing/conftest.py
   :language: python

A mod names the folders it installs into GameData, and in ``files`` any files it puts at the top
of GameData. ``present`` returns whether the running game loaded the mod, or ``None`` when the
mod gives no way to tell. A mod that needs another mod names it in ``requires``.

Each test class names the mods it requires. Before its ``setUpClass`` runs, KSP is running with
exactly those mods and the mods they require:

.. literalinclude:: /scripts/extending/testing/test_heatshields.py
   :language: python

The class fails when the :doc:`integration <other-mods>` of a required mod, matched by name,
failed to load or is missing members of the installed version.

``launch`` loads a new save, launches the craft, removes the other vessels and returns the active
vessel. It takes the craft from a ``craft`` directory beside the test file, or from the game's
stock craft. ``directory`` names another directory, and ``editor="SPH"`` launches onto the runway.
The craft takes its default crew, or the Kerbals named in ``crew``, and its crew become pilots.
``save=None`` launches in the current save, and ``keep_other_vessels=True`` keeps the vessels
already there.
The ``TestCase`` also places a vessel in orbit, on the ground or in flight, and has assertions
with a tolerance. ``krpctools.game.images`` checks the PNG and JPEG images an RPC returns.

Run the suite with the plugin loaded:

.. code-block:: sh

   pytest -p krpctools.game.pytest_plugin

The plugin orders the tests by the mods they require, so the game restarts at most once per mod
set. The run restarts, and installs into, only a game it launched.

Options
-------

``krpc-game`` and the pytest plugin take these options:

.. list-table::
   :header-rows: 1

   * - Option
     - Effect
   * - ``--game``
     - The game: ``ksp``, or ``module:object`` for a game object in Python code. ``krpc-game``
       defaults to ``ksp``, and the plugin to the game ``conftest.py`` registers.
   * - ``--game-dir``
     - The game's install directory, in place of ``KSP_DIR``.
   * - ``--krpc``
     - The kRPC archive to install, in place of the release krpctools was built with.
   * - ``--mod NAME SOURCE``
     - Installs a mod from a path or a URL, in place of its latest pinned archive.
   * - ``--skip-install-check``
     - Allows mods outside the requested set in GameData.
   * - ``--skip-mod-install``
     - Leaves the installed mods as they are.
   * - ``--no-launch``
     - Runs the tests against a game already running. Plugin only.
   * - ``--address``
     - The address of the game's server. Plugin only.

Downloads are cached in the user cache directory, such as ``~/.cache/krpctools``, so a repeat
install needs no network.
