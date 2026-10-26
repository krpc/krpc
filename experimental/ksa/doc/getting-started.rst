Getting Started
===============

This guide sets up the kRPC for KSA server, and runs a Python script that reads telemetry from
the game.

The Server Mod
--------------

Installation
^^^^^^^^^^^^

The server is a code mod, which runs in the game through StarMap, the KSA code mod loader:

1. Install `StarMap <https://github.com/StarMapLoader/StarMap/releases>`_
   @STARMAP_VERSION@.
2. Copy the ``kRPC`` folder from the release archive into the KSA mods folder,
   ``<Documents>/My Games/Kitten Space Agency/mods``.
3. Enable the mod in ``<Documents>/My Games/Kitten Space Agency/manifest.toml``:

   .. code-block:: toml

      [[mods]]
      id = "kRPC"
      enabled = true

4. Launch the game through StarMap.

The `Borea <https://github.com/KSAModding/Borea>`_ mod manager can do all four steps: it
installs StarMap and the mod, and launches the game through StarMap.

.. note::

   Version 0.1.0 named the mod ``kRPC.KSA``. To upgrade from it, delete the ``mods/kRPC.KSA``
   folder, and change its ``manifest.toml`` entry to ``id = "kRPC"``.

The server starts once the game has loaded its mods. It listens on ``127.0.0.1``, with RPC
port 50000 and stream port 50001, and accepts every connection.

Configuration
^^^^^^^^^^^^^

The **kRPC** menu in the game's menu bar opens the server window and the server info window,
and starts and stops the server. The server window shows each server's address and connected
clients, and edits its protocol, address and ports. **Advanced settings** at the bottom of the
window holds the connection and rate control settings.

Set the address to **Any** to accept connections from other machines.

The window saves its settings to ``kRPC/settings.toml`` in the mods folder. Installing the
mod again restores the default settings.

The Python Client
-----------------

Installation
^^^^^^^^^^^^

Install the kRPC Python client from PyPI:

.. code-block:: bash

   pip install krpc==@KRPC_VERSION@

Your First Script
^^^^^^^^^^^^^^^^^

This script connects to the game, and prints the bodies of the system and the active vehicle:

.. literalinclude:: /scripts/telemetry.py
   :language: python
   :start-at: conn = krpc.connect
   :end-at: print(f"Active vehicle

The client builds the ``KSA`` service from the definitions the server sends when it connects,
so the service is ``conn.ksa``. The complete script, :download:`telemetry.py
</scripts/telemetry.py>`, goes on to print the vehicle's altitude, apoapsis and attitude once a
second.

Next Steps
----------

* The :doc:`tutorials` walk through complete scripts, from :doc:`tutorials/launch-into-orbit`
  to planning and executing burns.
* The :doc:`user-guide` describe how the server and the game behave. :doc:`Server side
  functions </user-guide/server-side-functions>` run your code inside the game, for events and
  streams computed there.
* The :doc:`clients` pages cover each client language, and the API reference.
