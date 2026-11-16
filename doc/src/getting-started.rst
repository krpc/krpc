.. _getting-started:

Getting Started
===============

This guide sets up the kRPC server in the game, then writes a Python script that talks to it.

.. _getting-started-server:

The Server Plugin
-----------------

Installation
^^^^^^^^^^^^

1. Download kRPC from one of these places:

   * `GitHub <https://github.com/krpc/krpc/releases>`_
   * `SpaceDock <https://spacedock.info/mod/69/kRPC>`_
   * `CurseForge <https://www.curseforge.com/kerbal/ksp-mods/krpc-control-the-game-using-c-c-java-lua-python>`_

   Copy the ``GameData`` folder from the zip into your KSP folder. You can also install kRPC with
   `CKAN <https://github.com/KSP-CKAN/CKAN>`_.

2. Start KSP and load a save. The server window appears, with two servers: "TCP/IP Server" on
   port 50000, and "WebSockets Server" on port 50001 for the JavaScript client. The servers start
   when the game loads, and the light next to each turns green:

   .. image:: /images/getting-started/server-window.png

3. Click a server's name to show or hide its details: the protocol, the address and the
   connected clients. Its "Stop" and "Start" buttons stop and start it, and "Stop servers" and
   "Start servers" stop and start them all.

4. The close button in the top right hides the window. The kRPC button in the toolbar shows and
   hides it again, and turns green while a server is running:

   .. image:: /images/getting-started/applauncher.png

Configuration
^^^^^^^^^^^^^

Stop a server, then click its "Edit" button to change its settings. Click "Save" to keep them:

1. **Protocol**: How clients connect. The Python client connects over "Protobuf over TCP/IP",
   the default, or "Protobuf over local socket". Each client guide lists the protocols its client
   supports.
2. **Address**: The address the server listens on. "localhost", the default, accepts connections
   from the same machine only. To accept connections over a network, select one of the machine's
   addresses, "Any" for all of them, or "Manual" to type one in.
3. **Port**: The TCP port, 50000 by default for TCP/IP and 50001 for WebSockets. Change it only
   if another program uses that port.

"Add server" adds another server, for example one over a local socket.

"Show advanced settings" reveals these settings:

1. **Auto-start server**: On by default. Starts the servers when the game loads.
2. **Auto-accept new clients**: On by default. When off, a pop-up asks whether to allow each new
   client. A client stops waiting after its handshake timeout, 5 seconds by default, so connect
   with no handshake timeout to leave time to answer.
3. **Confirm disconnecting a client**: Asks before the disconnect button next to a client closes
   its connection.
4. **Pause the server when the game pauses**: While the game is paused, calls wait until it
   resumes.
5. **Debug logging**: Writes more detail to the game's log file, ``KSP.log``.

The other advanced settings control the :ref:`performance of the server
<server-performance-settings>`.

When a service fails to load, for example after another mod updates, a window lists the problems.
"Show problems" in the server window opens it again.

The Python Client
-----------------

The Python client needs Python 3.10 or later.

On Windows
^^^^^^^^^^

1. Download Python from https://www.python.org/downloads/windows and run the installer.

2. Install the client from a command prompt:

   .. code-block:: bat

      py -m pip install krpc

On Linux and macOS
^^^^^^^^^^^^^^^^^^

1. Install Python with your package manager, or from https://www.python.org/downloads.

2. Install the client into a virtual environment, from a terminal:

   .. code-block:: bash

      python3 -m venv krpc-env
      source krpc-env/bin/activate
      pip install krpc

   Activate the environment again in each new terminal before you run a script.

'Hello World' Script
--------------------

With the server running with the default settings, run this script:

.. literalinclude:: /scripts/getting-started/HelloWorld.py

It connects to the server, gets the active vessel and prints its name:

.. image:: /images/getting-started/hello-world.png

Going Further
-------------

* The :doc:`Python guide <python>` covers the client in depth.
* The :doc:`tutorials <tutorials>` show more of what kRPC can do.
* Clients are also available for :doc:`C# <csharp>`, :doc:`C++ <cpp>`, :doc:`C <cnano>`,
  :doc:`Java <java>`, :doc:`Lua <lua>` and :doc:`JavaScript <js>`.
* Any language can :doc:`talk to the server directly <communication-protocols>`.
* Ask for help on the `kRPC Discord server <https://discord.gg/bXuaTrj>`_.
