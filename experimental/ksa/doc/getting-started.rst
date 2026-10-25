Getting Started
===============

Installing the Mod
------------------

The mod runs in the game through StarMap, the KSA code mod loader:

1. Install `StarMap <https://github.com/StarMapLoader/StarMap/releases>`_
   @STARMAP_VERSION@.
2. Copy the ``kRPC.KSA`` folder from the release archive into the KSA mods folder,
   ``<Documents>/My Games/Kitten Space Agency/mods``.
3. Enable the mod in ``<Documents>/My Games/Kitten Space Agency/manifest.toml``:

   .. code-block:: toml

      [[mods]]
      id = "kRPC.KSA"
      enabled = true

4. Launch the game through StarMap.

The server starts once the game has loaded its mods. It listens on ``127.0.0.1``, with RPC
port 50000 and stream port 50001, and accepts every connection.

Server Settings
---------------

The settings are in ``kRPC.KSA/settings.toml``:

.. code-block:: toml

   auto_start = true
   auto_accept_connections = true

   [[servers]]
   name = "Default Server"
   protocol = "ProtocolBuffersOverTCP"
   address = "127.0.0.1"
   rpc_port = 50000
   stream_port = 50001

Set ``address`` to ``0.0.0.0`` to accept connections from other machines.

Connecting
----------

Install the Python client from PyPI:

.. code-block:: bash

   pip install krpc==@KRPC_VERSION@

Then connect to the game and print the vehicles:

.. literalinclude:: /scripts/telemetry.py
   :language: python
   :start-at: conn = krpc.connect
   :end-at: print(f"Active vehicle

The :doc:`clients` page covers the other languages.
