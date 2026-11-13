.. default-domain:: js
.. highlight:: javascript
.. module:: @krpc-org/krpc

JavaScript
==========

This guide describes how to install the JavaScript client, and the features it provides. The
:ref:`js-client-reference` describes each function and class.

The client runs in a web browser and in Node.js 22 or later. It is written in TypeScript, and the
package includes its type declarations.

Installing the Library
----------------------

The library is the ``@krpc-org/krpc`` package, which can be `downloaded from GitHub
<https://github.com/krpc/krpc/releases>`_. To install the downloaded package with npm:

.. code-block:: bash

   npm install krpc-js-0.8.0.tgz

The package is a set of ES modules, and depends on the ``@bufbuild/protobuf`` package. A web page
loads it through a bundler, such as `esbuild <https://esbuild.github.io>`_ or `Vite
<https://vite.dev>`_, which resolves that dependency.

Connecting to the Server
------------------------

The game must be running with the server started. :ref:`getting-started-server` in the
getting started guide shows how to install and start it.

The client connects over WebSockets, the protocol a web browser can use. The default server uses
TCP/IP, so add a server in the server window, and select "Protobuf over WebSockets" as its
protocol.

.. note::

   A server with "Auto-accept new clients" turned off answers only once the player approves the
   connection in game. The connection fails after 5 seconds by default. Pass ``Infinity`` as the
   ``handshakeTimeout`` to wait for the player.

:func:`connect` connects to the server, and returns a :class:`Connection`. You make every call
through this object. The following connects to a server with the default settings, address
``127.0.0.1`` and port 50000, and prints its version:

.. literalinclude:: /scripts/js/Connecting1.js

:func:`connect` also takes a name for the connection, and the address and port of the server.
These match the address and port shown in the server window. The name appears in the server window
in the game:

.. literalinclude:: /scripts/js/Connecting2.js

WebSockets works over a network, so your program can run on a different machine from the game.

.. note::

   A browser blocks a page served over ``https://`` from connecting to a ``ws://`` address. Open
   the page over ``http://``, or as a local file.

Closing the Connection
^^^^^^^^^^^^^^^^^^^^^^

:meth:`Connection.close` closes a connection:

.. literalinclude:: /scripts/js/Connecting3.js

Calling Remote Procedures
-------------------------

The server provides *procedures* for your program to call, grouped into *services*.

The following gets the active vessel, an object of the ``SpaceCenter.Vessel`` class. It sets the
vessel's name, then prints its altitude:

.. literalinclude:: /scripts/js/RemoteProcedures.js

Each service is a property of the connection, in lower camel case, such as ``conn.spaceCenter``
for the ``SpaceCenter`` service. Procedures and methods are in lower camel case too, as in
``vessel.flight()``. A property is a pair of methods, such as ``vessel.getName()`` and
``vessel.setName(name)``.

Every call returns a ``Promise``, which resolves once the server responds. Several calls can be in
flight at once, and each resolves with its own result. The following reads three values, without
waiting for each response before sending the next call:

.. literalinclude:: /scripts/js/Concurrent.js

Default and Named Arguments
^^^^^^^^^^^^^^^^^^^^^^^^^^^

A procedure can give some of its arguments default values. Pass ``undefined``, or leave out the
trailing arguments, to take the default. A plain object after the other arguments passes
arguments by name. The following launches a vessel without recovering the vessel on the launch
pad, and leaves the crew argument before it at its default:

.. literalinclude:: /scripts/js/Arguments.js

Names are in lower camel case. Passing an argument both by position and by name, or naming an
argument the procedure does not have, rejects with a ``TypeError``.

Values
^^^^^^

Each kRPC type has a JavaScript type:

.. list-table::
   :header-rows: 1

   * - kRPC type
     - JavaScript type
   * - ``float``, ``double``, ``sint32``, ``uint32``
     - ``number``
   * - ``sint64``, ``uint64``
     - ``bigint`` results. Arguments take a ``number`` or a ``bigint``.
   * - ``bool``
     - ``boolean``
   * - ``string``
     - ``string``
   * - ``bytes``
     - ``Uint8Array``
   * - list, tuple
     - ``Array``
   * - set
     - ``Set``
   * - dictionary
     - ``Map``
   * - structure
     - An object of the service's class for it, such as ``conn.spaceCenter.BoundingBox``. It is
       frozen, and has a property for each field, in lower camel case. Make one with ``new``,
       passing an object of the fields.
   * - enumeration
     - A :class:`RemoteEnum`, such as ``conn.spaceCenter.VesselSituation.orbiting``, or a
       :class:`RemoteFlags` for a :ref:`flags enumeration <js-client-flags>`
   * - class
     - An object of the service's class, such as ``SpaceCenter.Vessel``
   * - null
     - ``null``

The :func:`equals` function compares two values by their contents, such as two structures.

.. _js-client-flags:

Flags Enumerations
^^^^^^^^^^^^^^^^^^

A flags enumeration, such as :class:`~SpaceCenter.MapFilterType`, holds any combination of its
values. You combine values with ``union``, and test for one with ``contains``:

.. literalinclude:: /scripts/js/Flags.js

``intersection`` and ``difference`` give the other combinations of two values. Each combination is
one object, so combinations compare with ``===``.

Using the Client in a Web Page
------------------------------

The following script shows the altitude of the active vessel in a page, and updates it twice a
second:

.. literalinclude:: /scripts/js/Dashboard.js

Bundle the script for the page, for example with esbuild:

.. code-block:: bash

   npx esbuild dashboard.js --bundle --format=esm --outfile=dashboard.bundle.js

Then load it from the page with ``<script type="module" src="dashboard.bundle.js"></script>``.

.. note::

   Any page open in the browser can connect to a server it can reach. A server with
   "Auto-accept new clients" turned off asks the player to approve each connection.

Handling Errors
---------------

A procedure that fails on the server rejects its ``Promise`` with an error. The following catches
the error from loading a save that does not exist:

.. literalinclude:: /scripts/js/Errors.js

The error you catch depends on the error on the server:

* A standard error, such as an invalid argument, is a class that extends only the matching
  JavaScript error. A missing save is ``conn.krpc.ArgumentException``, which extends
  ``TypeError``.
* An error particular to a service is a class of that service, such as
  ``conn.krpc.ObjectDestroyedException``, which extends :class:`ServerError`.
* Any other error is a :class:`ServerError`, with the server's description as its message.

The client's own errors each extend :class:`RemoteError`, as :class:`ServerError` does:

* :class:`ConnectionError` for a failed connection attempt. When the server refuses the connection,
  its ``status`` names the status the server gave, such as ``DENIED``. When the server ends the
  connection, its ``reason`` names the reason, such as ``SERVER_STOPPED``. A call made once the
  connection has closed or failed rejects with it too.
* :class:`EncodingError` for a malformed result from the server.

A call with an argument of the wrong type rejects with a ``TypeError``, and one with a number
outside the range of its type with a ``RangeError``. These errors and the standard errors, such
as ``conn.krpc.ArgumentException``, do not extend :class:`RemoteError`.

Remote Objects
--------------

Many procedures return an object identifying something in the game, such as a vessel or a part.
The object holds no copy of the game's state. It is just a reference to it. Each call through it
reads the game as it is now:

.. literalinclude:: /scripts/js/RemoteObjects.js

A connection returns one JavaScript object for each object on the server, so two results for the
same thing in the game compare equal with ``===``. An object works as a ``Map`` key or a ``Set``
element.

Once the thing an object identifies is destroyed, such as a part that explodes, each call through
it rejects with ``conn.krpc.ObjectDestroyedException``.

.. seealso:: :doc:`/user-guide/object-lifetime` describes how each kind of object behaves in more
             detail.

Streams and Events
------------------

The JavaScript client has no streams or events yet. Read a value by calling its procedure in a
loop instead.

Deprecated Members
------------------

A member of a service can be deprecated, when a newer member replaces it. A deprecated member
still works, but may be removed in a future release, so move to its replacement.

The API reference and the TypeScript declarations mark it as ``@deprecated``, and name the
replacement. A use of it emits a warning naming the replacement, once per member on a connection.
The warning is a ``DeprecationWarning`` in Node.js, and a console warning in a web browser.

Service Stubs
-------------

When you connect, the client builds every service from the service definitions the server sends,
so the services match the server you connect to, whatever its version. The package also holds
TypeScript declarations of the services a kRPC release ships, which give your editor the types
and documentation of every procedure, class and value. They type-check JavaScript as well as
TypeScript, for example with ``// @ts-check`` at the top of a script.

A service from another mod has no declarations in the package. You can generate a module for it
with :ref:`krpc-clientgen <service-api-clientgen>`. Without one, its members have no static
types, and TypeScript reaches it through :class:`DynamicConnection`:

.. code-block:: ts

   import { connect, type DynamicConnection } from "@krpc-org/krpc";

   const conn = (await connect()) as DynamicConnection;
   await conn.myMod.doSomething();

.. _js-client-reference:

Client API Reference
--------------------

.. function:: connect(options)

   Connects to a kRPC server over WebSockets, and builds its services.

   :param options.name: A name for the connection, shown in the server window in the game.
                        Defaults to ``""``.
   :param options.address: The address of the server, as a host name or an IP address. Defaults
                           to ``"127.0.0.1"``.
   :param options.port: The port of the server. Defaults to 50000.
   :param options.handshakeTimeout: How many milliseconds to wait for the server to accept the
                                    connection. Defaults to 5000. ``Infinity`` waits indefinitely,
                                    such as for a server that asks the player to approve each
                                    connection.
   :returns: A ``Promise`` of the :class:`Connection`.

   Rejects with a :class:`ConnectionError` if the connection fails or the server refuses it, and
   with a ``RangeError`` for a timeout that is not positive.

.. function:: getObjectId(obj)

   The identifier of a remote object on the server, e.g. to pass to
   ``KRPC.Expression.constantObject``. Throws a ``TypeError`` if the object is ``null``.

.. function:: equals(a, b)

   Whether two values are equal. Objects compare by the object on the server, structures, arrays
   and bytes item by item, sets and maps by their members, and other values with ``===``.

.. class:: Connection()

   A connection to the server, with a property for each of the server's services. Obtain one by
   calling :func:`connect`.

   .. method:: close()

      Closes the connection. Each call waiting for a response rejects with a
      :class:`ConnectionError`. Closing a connection that is already closed does nothing.

   .. attribute:: krpc

      The ``KRPC`` service, providing the server's basic functionality. Each of the server's other
      services is a property too, such as ``spaceCenter``.

.. class:: DynamicConnection()

   The type of a :class:`Connection` with a property for every service the server has, such as a
   service from another mod. A TypeScript type only, with no static types for the services'
   members.

.. class:: RemoteService()

   The base class of a connection's services.

.. class:: RemoteObject()

   The base class of the objects of a service's classes.

.. class:: RemoteStruct()

   The base class of the values of a service's structures. A value is frozen, with a property for
   each field.

.. class:: RemoteEnum()

   A value of an enumeration. Each value is one object, so values compare with ``===``.

   .. attribute:: name

      The name of the value, in lower camel case.

   .. attribute:: value

      The value's number.

.. class:: RemoteFlags()

   A value of a flags enumeration, which holds any combination of the enumeration's values. It
   extends :class:`RemoteEnum`. Each combination is one object, so combinations compare with
   ``===``.

   .. method:: union(other)

      The combination of the flags of this value and another.

   .. method:: intersection(other)

      The flags this value and another both hold.

   .. method:: difference(other)

      This value without the flags of another.

   .. method:: contains(other)

      Whether this value holds every flag of another.

.. class:: RemoteError()

   The base class of the errors the client throws and rejects with.

.. class:: ServerError()

   A procedure that threw an exception on the server, with no JavaScript error for it. The class of
   each such exception a service lists extends it.

   .. attribute:: service

      The service that lists the exception, or ``""``.

   .. attribute:: exception

      The name of the exception, or ``""``.

   .. attribute:: description

      The description of the exception, from the server.

   .. attribute:: stackTrace

      The stack trace of the exception on the server, or ``""``.

.. class:: ConnectionError()

   A connection that failed, was refused or is closed.

   .. attribute:: status

      The status of a refused connection, such as ``DENIED``, or ``""``.

   .. attribute:: reason

      The reason in the server's disconnect message, such as ``SERVER_STOPPED``, or ``""``.

.. class:: EncodingError()

   A value that failed to encode or decode, such as a malformed result from the server.

API Reference
-------------

Each procedure and method, including the getter and setter of a property, returns a ``Promise``.
It resolves to a value of the return type listed for it.

.. include:: js/api.inc
