Web Browser Control Panel
=========================

This tutorial builds a control panel for a vessel that runs in a web browser, using the
:doc:`JavaScript client </js>`. The panel shows the vessel's altitude, air speed, thrust to weight
ratio and throttle. Its buttons activate the next stage, turn SAS and RCS on and off, and point the
vessel with SAS.

The panel is a page and a script:
:download:`control-panel.html </scripts/tutorials/control-panel/control-panel.html>` and
:download:`ControlPanel.js </scripts/tutorials/control-panel/ControlPanel.js>`.

Building the Panel
------------------

The script imports the client, so we bundle the two into one file for the page. The following
installs the client and `esbuild <https://esbuild.github.io>`_, then bundles them as
``control-panel.js``, which the page loads:

.. code-block:: bash

   npm install @krpc-org/krpc esbuild
   npx esbuild ControlPanel.js --bundle --format=iife --outfile=control-panel.js

The ``iife`` format gives a classic script, which a browser runs from a page opened as a local
file. Open ``control-panel.html`` in the browser.

In the game, add a server in the server window, select "Protobuf over WebSockets" as its protocol,
and start it. Then enter its address and port in the panel, and click "Connect".

The Page
--------

The page holds an element for each value the panel shows, and the buttons. The script fills in the
elements by their ``id``:

.. literalinclude:: /scripts/tutorials/control-panel/control-panel.html
   :language: html
   :start-at: <body>
   :end-at: </body>

Connecting
----------

The script keeps the connection, the controls of the vessel shown, and the streams of that vessel:

.. literalinclude:: /scripts/tutorials/control-panel/ControlPanel.js
   :language: javascript
   :start-at: /** @type {import("@krpc-org/krpc").Connection} */
   :end-at: let streams = [];

Clicking "Connect" calls ``connectToServer``. It connects to the server, then streams the active
vessel:

.. literalinclude:: /scripts/tutorials/control-panel/ControlPanel.js
   :language: javascript
   :start-at: async function connectToServer
   :end-before: element("connect").addEventListener

The active vessel changes when the player switches vessel. The client returns one object for each
vessel, so the callback compares the new vessel with the one shown using ``===``. The callback
returns a ``Promise``, so the client waits for ``showVessel`` to finish before it calls the
callback again.

A stream ends when its connection closes. The end callback of the active vessel stream resets the
panel, whether the player stopped the server or we clicked "Disconnect".

Flight Data
-----------

Each value on the panel comes from a stream. The ``watch`` function adds a callback to a stream
that updates the page, and limits the stream to 10 updates a second:

.. literalinclude:: /scripts/tutorials/control-panel/ControlPanel.js
   :language: javascript
   :start-at: /** Calls update with each new value of a stream
   :end-before: async function connectToServer

``showVessel`` first removes the streams of the vessel shown before. It then adds a stream for each
value on the panel, and for the state of the buttons. A stream of the SAS state lights up the SAS
button, including when the player turns SAS on in the game.

The thrust to weight ratio is a stream of a :ref:`server side function <js-client-functions>`.
The function divides the vessel's thrust by its weight, using the gravity at the vessel's distance
from the center of the body. A ratio over 1 lifts the vessel off the ground.

Controls
--------

Each button runs an action on the vessel's controls through ``act``. A button clicked before the
panel shows a vessel does nothing, and an action that fails logs its error to the page:

.. literalinclude:: /scripts/tutorials/control-panel/ControlPanel.js
   :language: javascript
   :start-at: /** Runs an action on the vessel's controls
   :end-before: /** Calls update with each new value of a stream

The buttons are attached at the end of the script:

.. literalinclude:: /scripts/tutorials/control-panel/ControlPanel.js
   :language: javascript
   :start-at: element("connect").addEventListener

The SAS mode buttons turn SAS on before they set the mode, so the vessel holds the
direction. A mode the vessel cannot use, such as "Target" with no target selected, logs an
error.
