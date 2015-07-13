.. _getting-started:

Getting Started
===============

This short guides explains the basics for getting kRPC set up and running, and
writing a basic Python script to communicate with the game.

The Server Plugin
-----------------

Installation
^^^^^^^^^^^^

1. Download the kRPC server plugin from one of these locations:

 * `KerbalStuff <https://kerbalstuff.com/mod/636>`_
 * `Curse <http://www.curse.com/project/220219>`_
 * `Github <https://github.com/djungelorm/krpc/releases>`_
 * Or the plugin can be obtained via `CKAN
   <http://forum.kerbalspaceprogram.com/threads/100067-The-Comprehensive-Kerbal-Archive-Network-%28CKAN%29-Package-Manager-v1-6-5-6-Mar-2015>`_

2. Extract the GameData folder from the archive into your KSP directory.

3. Start up KSP and load a save game.

4. You should be greeted by the server window:

   .. image:: /images/getting-started/server-window-offline.png

5. Click "Start server" to, erm... start the server! If all goes well, the light
   should turn a happy green color:

   .. image:: /images/getting-started/server-window-online.png

6. You can hide the window by clicking the close button in the top right, or
   show/hide the window by clicking on the kRPC icon in the application
   launcher:

   .. image:: /images/getting-started/applauncher.png

   This icon will also turn green when the server is online.

Configuration
^^^^^^^^^^^^^

The server can be configured using the window displayed in-game. The
configuration options are:

1. **Address**: this is the IP address that the server will listen on. To only
   allow connections from the local machine, select 'localhost' (the
   default). To allow connections over the network, either select the local IP
   address of your machine, or choose 'Manual' and enter the local IP address
   manually.
2. **RPC and Stream port numbers**: These need to be set to port numbers that
   are available on your machine. In most cases, they can just be left as the
   default.

There are also several advanced, which are hidden by default, but can be
revealed by checking the 'Advanced settings' box:

1. **Auto-start server**: When enabled, the server will start automatically when
   the game loads.
2. **Auto-accept new clients**: When enabled, new client connections are
   automatically allowed. When disabled, a pop-up is displayed asking whether
   the new client connection should be allowed.

The other advanced settings control the performance of the server. For details,
:ref:`see here <server-performance-settings>`.

The Python Client
-----------------

.. note:: kRPC supports both Python 2.7 and Python 3.x.

On Windows
^^^^^^^^^^

1. If you don't already have python installed, download the python installer and
   run it: https://www.python.org/downloads/windows When running the installer,
   make sure that pip is installed as well.

2. Install the kRPC python module, by opening command prompt and running the
   following command: ``pip install krpc``

3. Run Python IDLE (or your favorite editor) and start coding!

On Linux
^^^^^^^^

1. Your linux distribution likely already comes with python installed. If not,
   install python using your favorite package manager, or get it from here:
   https://www.python.org/downloads

2. You also need to install pip, either using your package manager, or from
   here: https://pypi.python.org/pypi/pip

3. Install the kRPC python module by running the following from a terminal:
   ``pip install krpc``

4. Start coding!

'Hello World' Script
--------------------

Run KSP and start the server with the default settings. Then execute the
following python script:

.. code-block:: python
   :linenos:

   import krpc
   conn = krpc.connect(name='Hello World')
   vessel = conn.space_center.active_vessel
   print(vessel.name)

This does the following: line 1 loads the kRPC python module, line 2 opens a new
connection to the server, line 3 gets the active vessel and line 4 prints out
the name of the vessel. You should see something like the following:

.. image:: /images/getting-started/hello-world.png

Congratulations! You've written your first script that communicates with KSP.
For some more interesting examples, check out the :doc:`tutorials <tutorials>`.
