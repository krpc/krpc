Getting Started
===============

The Server Plugin
-----------------

Installation
^^^^^^^^^^^^

1. Download the kRPC server plugin from one of these locations:

 * `KerbalStuff <https://kerbalstuff.com/mod/636>`_
 * `Curse <http://www.curse.com/project/220219>`_
 * `Github <https://github.com/djungelorm/krpc/releases>`_

2. Extract the archive and copy the GameData/kRPC folder to the GameData folder
   in your KSP installation.

3. Start KSP and send a vessel to the launchpad.

4. You should be greeted by the server window:

.. image:: /images/getting-started/server-window-offline.png

5. Click "Start Server" to, erm... start the server! If all goes well, the light
   should turn a happy green color:

.. image:: /images/getting-started/server-window-online.png

6. You can show/hide this window by clicking the kRPC icon in the application
   launcher in the top right:

.. image:: /images/getting-started/applauncher.png

Configuration
^^^^^^^^^^^^^

The server can be configured using the window displayed in-game. The
configuration options are:

1. **Address**: this is the IP address that the server will listen on. To only
   allow connections from the local machine, enter 127.0.0.1 (the default). To
   allow connections over the network, enter the local IP address of your
   machine.
2. **RPC and Stream port numbers**: These need to be set to port numbers that
   are available on your machine. In most cases, they can just be left as the
   default.
3. **Auto-accept new clients**: If disabled, when a client connects a pop-up is
   displayed asking whether the connection should be allowed. If enabled, new
   connections are automatically allowed.
4. **Auto-start server**: When enabled, the server will start automatically when
   the flight view is entered. For example, when switching to the launch pad
   from the VAB. This means you don't have to click "Start Server" every time
   you launch a vessel.

The Python Client
-----------------

On Windows
^^^^^^^^^^

1. If you don't already have python installed, download the python installer and
   run it: https://www.python.org/downloads/windows You want version 2.x
   (version 3 does not work with kRPC). When running the installer, make sure
   that pip is installed as well.

2. Install the kRPC python module. Open command prompt, and run the following
   command: ``pip install krpc``

3. Run Python IDLE (or your favorite editor) and start coding!

On Linux
^^^^^^^^

1. Your linux distribution likely already comes with python installed. If not,
   install python version 2.x using your favourite package manager, or get it
   from here: https://www.python.org/downloads

2. You also need to install pip, either using your package manager, or from
   here: https://pypi.python.org/pypi/pip

3. Install the kRPC python module by running the following from a terminal:
   ``pip install krpc``

4. Start coding!

'Hello World' Script
--------------------

Run KSP and start the server with the default settings. Then run the following
python script:

.. code-block:: python
   :linenos:

   import krpc
   conn = krpc.connect(name='Hello World')
   vessel = conn.space_center.active_vessel
   print vessel.name

This does the following: line 1 loads the kRPC python module, line 2 opens a new
connection to the server, line 3 gets the active vessel and line 4 prints out
the name of the vessel. You should see something like the following:

.. image:: /images/getting-started/hello-world.png

Congratulations! You've written your first script that communicates with KSP.
For some more interesting examples, check out the :doc:`tutorials <tutorials>`.
