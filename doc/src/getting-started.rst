.. _getting-started:

Getting Started
===============

This short guide explains the basics for getting the kRPC server set up and running, and writing a
basic Python script to interact with the game.

The Server Plugin
-----------------

Installation
^^^^^^^^^^^^

1. Download and install the kRPC server plugin from one of these locations:

 * `Github <https://github.com/krpc/krpc/releases>`_
 * `SpaceDock <https://spacedock.info/mod/69/kRPC>`_
 * `Curse <https://mods.curse.com/ksp-mods/kerbal/220219-krpc-control-the-game-using-c-c-java-lua-python>`_
 * Or the install it using `CKAN <https://forum.kerbalspaceprogram.com/index.php?/topic/90246-the-comprehensive-kerbal-archive-network-ckan-package-manager-v1180-19-june-2016/>`_

2. Start up KSP and load a save game.

3. You should be greeted by the server window:

   .. image:: /images/getting-started/server-window.png

4. Click "Start server" to, erm... start the server! If all goes well, the light should turn a happy
   green color.

5. You can hide the window by clicking the close button in the top right. The window can also be
   shown/hidden by clicking on the icon in the top right:

   .. image:: /images/getting-started/applauncher.png

   This icon will also turn green when the server is online.

Configuration
^^^^^^^^^^^^^

The server is configured by clicking edit on the window displayed in-game:

1. **Protocol**: this is the protocol used by the server. This affects type of client can connect to
   the server. For Python, and most other clients that communicate over TCP/IP you want to select
   "Protobuf over TCP".
2. **Address**: this is the IP address that the server will listen on. To only allow connections
   from the local machine, select 'localhost' (the default). To allow connections over a network,
   either select the local IP address of your machine, or choose 'Manual' and enter the local IP
   address manually.
3. **RPC and Stream port numbers**: These need to be set to port numbers that are available on your
   machine. In most cases, they can just be left as the default.

There are also several advanced settings, which are hidden by default, but can be revealed by
checking "Show advanced settings":

1. **Auto-start server**: When enabled, the server will start automatically when the game loads.
2. **Auto-accept new clients**: When enabled, new client connections are automatically allowed. When
   disabled, a pop-up is displayed asking whether the new client connection should be allowed.

The other advanced settings control the :ref:`performance of the server
<server-performance-settings>`.

The Python Client
-----------------

.. note:: kRPC supports both Python 2.7 and Python 3.x.

On Windows
^^^^^^^^^^

1. If you don't already have python installed, download the python installer and run it:
   https://www.python.org/downloads/windows When running the installer, make sure that pip is
   installed as well.

2. Install the kRPC python module, by opening command prompt and running the following command:
   ``C:\Python27\Scripts\pip.exe install krpc`` You might need to replace ``C:\Python27`` with the
   location of your python installation.

3. Run Python IDLE (or your favorite editor) and start coding!

On Linux
^^^^^^^^

1. Your linux distribution likely already comes with python installed. If not, install python using
   your favorite package manager, or get it from here: https://www.python.org/downloads

2. You also need to install pip, either using your package manager, or from here:
   https://pypi.python.org/pypi/pip

3. Install the kRPC python module by running the following from a terminal:
   ``sudo pip install krpc``

4. Start coding!

'Hello World' Script
--------------------

Run KSP and start the server with the default settings. Then run the following python script.


.. code-block:: python
   :linenos:

   import krpc
   conn = krpc.connect(name='Hello World')
   vessel = conn.space_center.active_vessel
   print(vessel.name)

This does the following: line 1 loads the kRPC python module, line 2 opens a new connection to the
server, line 3 gets the active vessel and line 4 prints out the name of the vessel. You should see
something like the following:

.. image:: /images/getting-started/hello-world.png

Congratulations! You've written your first script that communicates with KSP.

Going further...
----------------

 * For some more interesting examples of what you can do with kRPC, check out the
   :doc:`tutorials <tutorials>`.
 * Client libraries are available for other languages too, including :doc:`C# <csharp>`,
   :doc:`C++ <cpp>`, :doc:`Java <java>` and :doc:`Lua <lua>`.
 * It is also possible to :doc:`communicate with the server manually <communication-protocols>` from
   any language you like.
