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

The package is a set of ES modules, and depends on the ``@bufbuild/protobuf`` and ``acorn``
packages. A web page loads it through a bundler, such as `esbuild <https://esbuild.github.io>`_ or
`Vite <https://vite.dev>`_, which resolves those dependencies.

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

.. _js-client-imports:

Types Without a Connection
^^^^^^^^^^^^^^^^^^^^^^^^^^

The package exports each service of a kRPC release, with its enumerations, structures and
exception classes. You can then use a value before you connect, such as in a default argument:

.. literalinclude:: /scripts/js/Imports.js

When the server defines a type as the package does, the connection uses the package's type. Its
values then compare with ``===``, and ``instanceof`` works with the package's classes. A connection
to a server with a different definition builds its own type. A value of the package's enumeration
still passes as an argument, by its number, and :func:`equals` compares values of the two by number.

.. _js-client-web-page:

Using the Client in a Web Page
------------------------------

The following script shows the altitude of the active vessel in a page. A stream with a callback
updates it each time it changes, at most 10 times a second:

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
* :class:`StreamError` for a stream that has been removed or cannot be added, and for
  :attr:`Stream.value` before the first value. See :ref:`js-client-stream-endings`.

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

Streaming Data
--------------

A common use of kRPC is to read data from the game continuously. Calling a procedure in a loop
sends a request and waits for its response each time. A *stream* is more efficient. The server
runs the call repeatedly, and sends your program the result each time it changes:

.. literalinclude:: /scripts/js/Streaming.js

:meth:`Connection.addStream` takes a function that returns one call, such as
``() => flightInfo.getMeanAltitude()``, and returns a ``Promise`` of a :class:`Stream`. The client
runs the function to build the call, without sending it. The stream's type comes from the call, so
TypeScript types ``altitude`` as a ``Stream<number>``.

:meth:`Stream.get` waits for the stream's first value, and :attr:`Stream.value` is its latest
value. :meth:`Stream.wait` waits for the next value. It takes a timeout in milliseconds, and
resolves with ``false`` when it elapses before an update.

Each call to :meth:`Connection.addStream` creates another stream. A stream of a procedure with no
return value, such as a property setter, runs the call on each update for its effects. Its value
is ``undefined``.

A stream runs until you remove it with :meth:`Stream.remove`, or the connection closes. It keeps
running after your program drops its last reference to it, so remove a stream you no longer need.
An ``await using`` declaration removes a stream at the end of its block. It needs Node.js 24 or a
recent browser, or TypeScript to compile it:

.. literalinclude:: /scripts/js/StreamScope.js

By default, the server sends a stream's value each time it changes. :meth:`Stream.setRate` limits
how many times a second the stream updates, as in the :ref:`web page example <js-client-web-page>`.

Callbacks
^^^^^^^^^

A callback is a function the client calls with each new value of a stream. The following adds two
callbacks to a stream of the abort button:

.. literalinclude:: /scripts/js/Callbacks.js

A callback added to a stream that already has a value is called with that value before
:meth:`Stream.addCallback` returns. It is then called with each new value, in order. The client
calls it once it has handled the message holding the value, so a callback can make calls and
await them.

A callback that returns a ``Promise`` is not called again until the ``Promise`` settles. It is then
called with the latest value, and skips the values in between. An error a callback throws
goes to ``reportError`` in a web page, or ``console.error`` in Node.js. The stream keeps running.

.. _js-client-stream-endings:

Stream Endings
^^^^^^^^^^^^^^

A stream ends when its procedure throws an exception, when the server removes it, when you remove
it, or when the connection closes. :meth:`Stream.get`, :attr:`Stream.value` and :meth:`Stream.wait`
then throw the error that ended it. The following streams the temperature of a part until the part
is destroyed:

.. literalinclude:: /scripts/js/StreamEnd.js

The error is the one the procedure threw, or one of the following:

* :class:`StreamError` when you or the server remove the stream.
* :class:`ConnectionError` when the connection closes or fails.

An exception for an object that is not loaded, or for a procedure not available in the current
game scene, does not end the stream. It pauses the stream's updates, and the stream keeps its last
value. The updates resume once the object or the scene is available.

.. seealso:: :doc:`/user-guide/object-lifetime` describes when each kind of object is not loaded,
             and how streams behave around a load.

:meth:`Stream.addEndCallback` adds a callback that is called once when the stream ends, with the
error:

.. literalinclude:: /scripts/js/EndCallback.js

Events
------

An *event* tells your program when something happens in the game. A procedure that returns an
event, such as one from a mod's service, gives you an :class:`Event`. So does
:meth:`Connection.addEvent`, for a :ref:`server side function <js-client-functions>`:

.. code-block:: js

   const event = await conn.myMod.onDocked();
   await event.wait();

:meth:`Event.wait` waits for the event to fire. It returns at once if the event has fired since
your last wait, so a firing between two waits is not missed. :meth:`Event.addCallback` adds a
function to call each time the event fires.

An event ends in the same ways as a :ref:`stream <js-client-stream-endings>`. When the event fires
just before the server ends it, your next wait returns for that firing first. Removing the event
or closing the connection ends it at once. :attr:`Event.stream` is the event's stream, of a
``boolean`` that is ``true`` once the event has fired.

.. _js-client-functions:

Server Side Functions
---------------------

A server side function runs inside the game. The client compiles a JavaScript function into one,
and the server runs it there. The following reads the vessel's altitude and speed in one physics
tick:

.. literalinclude:: /scripts/js/CompiledFunction.js

The function is written as it runs on the server. A property is read as a property, as in
``flight.meanAltitude``, and a method returns its value. The :ref:`server side functions guide
<server-side-functions>` describes what functions are for, and how the server runs them.

.. _js-client-compiling-functions:

Captured Values
^^^^^^^^^^^^^^^

The function reads values from your program only through its first parameter, an object pattern
such as ``({ flight, gravity })``. The client passes it the object given after the function. A
minifier keeps the keys of the pattern, so a compiled function survives one. A name the function
reads that is not captured is a :class:`CompilationError`:

.. literalinclude:: /scripts/js/CompilationErrors.js

Each captured value is used as follows:

* A remote object or a service makes its members calls in the function, made again on each
  evaluation. A service is captured as ``conn.spaceCenter``, or through the connection, as
  ``conn``.
* A number, ``bigint``, string, boolean, ``null`` or enumeration value is a constant.
* A plain object, a generated module such as ``SpaceCenter``, and the object of an enumeration's
  values are read on the client. ``SpaceCenter.SASMode.prograde`` is a constant.
* Any sub-expression of constants, such as ``gravity * 2``, is computed on the client, with the
  result JavaScript gives.

A function with no captures takes no parameters, or names its first one, such as ``_``, and does
not read it. The builtins ``Math``, ``console``, ``Number``, ``String``, ``Boolean``, ``BigInt``,
``undefined``, ``NaN`` and ``Infinity`` need no capture.

What a Function Can Contain
^^^^^^^^^^^^^^^^^^^^^^^^^^^

A compiled function is an arrow function, a function expression or a method. Its body holds
statements, collections and local functions, as in your program:

.. literalinclude:: /scripts/js/CompiledStatements.js

The following compile to the server's operations:

* Property reads and assignments, and calls of methods and static methods, with arguments by
  name as a last object, as in the client's own API. Destructuring reads properties too.
* Arithmetic, comparisons, ``!``, ``&&``, ``||``, ``??``, ``?.``, ``?:`` and template literals.
* The bitwise operators, on numbers and on the values of a :ref:`flags enumeration
  <js-client-flags>`, which also have ``union``, ``intersection``, ``difference`` and
  ``contains``.
* ``const`` and ``let``, ``if``, ``switch``, ``while``, ``do``, ``for``, ``for...of``, ``break``,
  ``continue`` and ``return``, assignments, compound assignments, ``++`` and ``--``. A declaration
  in a ``case`` goes in a block.
* Arrays, ``new Set`` and ``new Map``. An array is a tuple where the position takes a tuple, such
  as a vector argument, and a list otherwise. A captured array, ``Set`` or ``Map`` is a constant.
* The array members ``length``, indexing, ``at``, ``map``, ``filter``, ``some``, ``every``,
  ``find``, ``findIndex``, ``includes``, ``indexOf``, ``reduce``, ``slice``, ``concat``, ``join``,
  ``toSorted``, ``toReversed``, ``push``, ``pop`` and assignment to an element. ``toSorted`` takes
  a comparator that subtracts a number, such as ``(a, b) => a.mass - b.mass``.
* The ``Set`` and ``Map`` members ``has``, ``add``, ``get``, ``set``, ``delete``, ``clear``,
  ``size``, ``keys``, ``values`` and ``entries``, and ``Array.from``.
* The fields of a structure, and ``new`` of a structure's class with an object of its fields.
* The string members ``length``, indexing, ``at``, ``slice``, ``substring``, ``toUpperCase``,
  ``toLowerCase``, ``trim``, ``trimStart``, ``trimEnd``, ``includes``, ``startsWith``,
  ``endsWith``, ``indexOf``, ``replaceAll`` and ``split``, and ``String(x)``. The replacement of
  ``replaceAll`` is a constant with no ``$`` pattern.
* The ``Math`` functions with a ``StdLib`` procedure, ``Number.isNaN`` and ``Number.isFinite``.
* ``console.log``, which writes a line to the game's log with :meth:`StdLib.print`.
* ``throw new Error(message)``, ``TypeError``, ``RangeError`` and a service's exception class,
  and ``try``, ``catch`` and ``finally``.

The values keep their JavaScript meaning:

* **A number is a double.** Arithmetic on numbers is in double precision, so ``7 / 2`` is
  ``3.5``. The bitwise operators convert their operands to 32-bit integers, as JavaScript does.
  A number passed as an integer argument is converted toward zero.
* **A** ``bigint`` **is a 64-bit integer**, so ``7n / 2n`` is ``3n``. Mixing a number and a
  ``bigint`` is a :class:`CompilationError`.
* **Truth is JavaScript's.** ``0``, ``NaN``, ``""`` and ``null`` are false.
* **Null takes the type of the value beside it**, such as the other branch of ``?:``. Arithmetic
  on a value that can be null is a :class:`CompilationError`. Give a value for null with
  ``??``.
* **Compare a server value with** ``== null`` **or** ``!= null``. The server has one null, so
  ``=== null`` and ``=== undefined`` are a :class:`CompilationError`.
* **An empty array, Set or Map, and null, take their type from a later use**, such as a ``push``
  or an assignment. With no such use, give one a type with ``cast``, such as
  ``t.list(t.double).cast([])`` with the ``types`` value captured as ``t``.
* **A function that returns a value returns one on every path**, and a variable is read only
  where every path to the read assigns it.
* **A Set or a Map is the server's**, a .NET ``HashSet`` or ``Dictionary``. After a ``delete``, its
  order can differ from the order of insertion.
* A number converts to a string as the server formats it, which can differ from JavaScript in
  the last digits.

**Local functions** are called as in JavaScript, and passed by name to the array methods. Each
parameter takes the one type that fits the arguments of every call. A function cannot call itself,
and reads the variables it uses when called, so every path to each call assigns them.

**Exceptions** reach a ``catch`` with only their message, as ``e.message``. A handler that starts
with ``if (e instanceof RangeError)`` catches each exception the client maps to a ``RangeError``,
and the same holds for ``TypeError`` and a service's exception class. An ``else`` that is
``throw e`` leaves the other exceptions uncaught. A thrown ``Error`` becomes ``KRPC.Error`` on the
server, a ``TypeError`` becomes ``KRPC.ArgumentException``, and a ``RangeError`` becomes
``KRPC.ArgumentOutOfRangeException``.

**State between evaluations.** A captured plain object whose property the function assigns, or
changes with ``push`` and the like, keeps that property on the server between evaluations. The
property starts as it is when the function is compiled, and your program's object keeps its
value:

.. literalinclude:: /scripts/user-guide/server-side-functions/FiringCounter.js
   :start-after: [docs]
   :end-before: [/docs]

Any other construct is a :class:`CompilationError` that names it, with its line and column in
the function's source. The client loads the compiler the first time you compile a function, so a
web page that never compiles one does not load it.

.. note::

   A transpiler that targets an older version of JavaScript rewrites ``?.``, ``??`` and arrow
   functions into code the compiler rejects. Target ES2020 or later. A coverage tool that
   instruments the source, such as Istanbul, also changes the function. V8 coverage, as ``c8``
   and Vitest use by default, does not.

Running a Function
^^^^^^^^^^^^^^^^^^

:meth:`Connection.run` compiles a function with no parameters and runs it once. The server runs
the whole function within one physics tick, so every value it reads comes from the same moment in
the game. In the first example, the altitude and the speed are consistent with each other, which
two separate calls cannot guarantee.

A function takes arguments through the parameters after its captures. :meth:`Connection.compile`
compiles it once, with a type for each of those parameters, and :meth:`Connection.run` passes the
arguments:

.. literalinclude:: /scripts/js/CompiledParameters.js

The ``types`` values name the types: ``types.double``, ``types.float``, ``types.int32``,
``types.uint32``, ``types.int64``, ``types.uint64``, ``types.bool``, ``types.string`` and
``types.bytes``, ``types.nullable(T)``, and a service's class, such as ``conn.spaceCenter.Vessel``.
Compile a function once and run it many times, as each compile builds the function again on the
server.

Function Streams and Events
^^^^^^^^^^^^^^^^^^^^^^^^^^^

:meth:`Connection.addEvent` adds an event that fires when a function's value becomes ``true``.
:meth:`Connection.addStream` streams a function's value:

.. literalinclude:: /scripts/js/FunctionEvent.js

The server computes the function on each stream update. A function of a stream or an event takes
no parameters, and makes a call, as a function of constants has the same value on every update.

The Expression API
^^^^^^^^^^^^^^^^^^

The methods of ``conn.krpc.Expression`` build a function node by node, as the clients with no
compiler do. The following reads the vessel's altitude and speed into a tuple:

.. literalinclude:: /scripts/js/RunFunction.js

:meth:`Connection.getCall` takes a function that returns one call, as :meth:`Connection.addStream`
does, and returns the call as a message for ``Expression.call`` to embed in the function. Each
method of ``conn.krpc.Expression`` builds one node of the function on the server, and returns a
``Promise`` of it. :ref:`server-side-functions-api` in the server side functions guide describes
how to build functions. :meth:`Connection.run`, :meth:`Connection.compile`,
:meth:`Connection.addStream` and :meth:`Connection.addEvent` take the function the expression
API builds. The client reads the types of its parameters and value from the server, once per
function. TypeScript types the value as ``unknown``, or as the type you name with ``run<T>``,
which the client does not check.

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
TypeScript, for example with ``// @ts-check`` at the top of a script. Their enumerations,
structures and exceptions are values too, which you can :ref:`use without a connection
<js-client-imports>`.

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
      :class:`ConnectionError`, and each stream ends with one. Closing a connection that is
      already closed does nothing.

   .. method:: addStream(call, captures)

      Adds a stream of a call, or of the value of a server side function.

      :param call: A function that returns one call of a procedure, method or property getter,
                   such as ``() => flight.getMeanAltitude()``. The client runs it to build the
                   call, which it does not send. Or a ``KRPC.Expression`` with no parameters, or a
                   function to compile, with ``captures``.
      :param captures: The captured values of a function to compile.
      :returns: A ``Promise`` of the :class:`Stream`.

      Rejects with a :class:`StreamError` for a procedure that returns an event, with a
      ``TypeError`` for a function that does not return one call, or a server side function with
      parameters, and with a :class:`CompilationError` for a function that does not compile.

   .. method:: addEvent(fn, captures)

      Adds an event that fires when the value of a server side function becomes ``true``, and
      each time it becomes ``true`` again.

      :param fn: A ``KRPC.Expression`` of a ``boolean``, with no parameters, or a function to
                 compile that returns a ``boolean``.
      :param captures: The captured values of a function to compile, if it has any.
      :returns: A ``Promise`` of the :class:`Event`.

   .. method:: getCall(call)

      The call a function returns, as a ``ProcedureCall`` message for ``KRPC.Expression.call``.

      :param call: A function that returns one call, as for :meth:`addStream`. The client runs it
                   to build the call, which it does not send.

      Throws a ``TypeError`` for a function that does not return one call.

   .. method:: compile(fn, captures, parameters)

      Compiles a JavaScript function into a server side function, and compiles a server side
      function on the server, so that its first run is as fast as later ones.

      :param fn: A ``KRPC.Expression``, or a function to compile.
      :param captures: The captured values of a function to compile, if it has any.
      :param parameters: The type of each parameter of a function to compile, after its captures,
                         such as ``[types.double]``.
      :returns: A ``Promise`` of the ``KRPC.Expression``.

      Rejects with a :class:`CompilationError` for a function that does not compile.

   .. method:: run(fn, ...args)

      Runs a server side function once, within a single physics tick.

      :param fn: A ``KRPC.Expression``, or a function to compile with no parameters.
      :param args: The arguments, one for each of the function's parameters, in order. For a
                   function to compile, its captured values, if it has any.
      :returns: A ``Promise`` of the function's value, or of ``undefined`` for a function with no
                value.

      Rejects with a ``TypeError`` for the wrong number of arguments, or an argument of the wrong
      type, with a :class:`CompilationError` for a function that does not compile, and with the
      error the function throws.

   .. method:: addUpdateCallback(callback)

      Adds a function to call once for each stream update message from the server, after the
      callbacks of its streams.

   .. method:: removeUpdateCallback(callback)

      Removes a function added with :meth:`addUpdateCallback`.

   .. attribute:: krpc

      The ``KRPC`` service, providing the server's basic functionality. Each of the server's other
      services is a property too, such as ``spaceCenter``.

.. class:: Stream()

   A stream of a call, which the server repeats, sending each new value. Obtain one by calling
   :meth:`Connection.addStream`.

   .. method:: get()

      The stream's latest value. Waits for the first value.

      :returns: A ``Promise`` of the value. Rejects with the error that ended the stream.

   .. attribute:: value

      The stream's latest value. Throws a :class:`StreamError` before the first value, and the
      error that ended the stream once it has ended.

   .. method:: wait(timeout)

      Waits for the stream's next value.

      :param timeout: The time to wait, in milliseconds. Leave it out, or pass ``Infinity``, to
                      wait indefinitely.
      :returns: A ``Promise`` of ``true`` on an update, or ``false`` once the timeout elapses.
                Rejects with the error that ends the stream, and with a ``RangeError`` for a
                negative or ``NaN`` timeout.

   .. attribute:: rate

      The most times a second the server sends the stream's value. Zero means no limit.

   .. method:: setRate(rate)

      Sets the most times a second the server sends the stream's value. Zero means no limit.

      :returns: A ``Promise`` that resolves once the server has the rate.

   .. method:: addCallback(callback)

      Adds a function to call with each new value. A stream that has a value calls it with that
      value before this returns. A callback that returns a ``Promise`` is not called again until
      it settles.

   .. method:: removeCallback(callback)

      Removes a function added with :meth:`addCallback`.

   .. method:: addEndCallback(callback)

      Adds a function to call once when the stream ends, with the error that ended it. A stream
      that has ended calls it before this returns.

   .. method:: removeEndCallback(callback)

      Removes a function added with :meth:`addEndCallback`.

   .. method:: remove()

      Removes the stream, and calls its end callbacks. Reads and waits then throw a
      :class:`StreamError`. Removing a stream that has ended does nothing.

      :returns: A ``Promise`` that resolves once the server has removed the stream.

.. class:: Event()

   An event, which fires each time its stream's value becomes ``true``. A procedure that returns an
   event gives one, as does :meth:`Connection.addEvent`.

   .. method:: wait(timeout)

      Waits for the event to fire. Resolves at once for a firing since the last wait.

      :param timeout: The time to wait, in milliseconds. Leave it out, or pass ``Infinity``, to
                      wait indefinitely.
      :returns: A ``Promise`` of ``true`` when the event fires, or ``false`` once the timeout
                elapses. Rejects with the error that ends the event.

   .. method:: addCallback(callback)

      Adds a function to call each time the event fires. It is called before this returns for a
      firing since the last wait that no callback has been called for.

   .. method:: removeCallback(callback)

      Removes a function added with :meth:`addCallback`.

   .. method:: addEndCallback(callback)

      Adds a function to call once when the event ends, with the error that ended it.

   .. method:: removeEndCallback(callback)

      Removes a function added with :meth:`addEndCallback`.

   .. method:: remove()

      Removes the event, and calls its end callbacks.

   .. attribute:: stream

      The event's :class:`Stream`, of a ``boolean`` that is ``true`` once the event has fired.

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

.. class:: StreamError()

   A stream or an event that you or the server removed, or that cannot be added. Reading
   :attr:`Stream.value` before the first value throws one too. It is the class of
   ``KRPC.StreamRemovedException``.

.. class:: CompilationError()

   A function the client cannot compile into a server side function. The message names the
   construct, and its position in the function's source.

   .. attribute:: line

      The line of the construct in the function's source, counting from 1, or ``0``.

   .. attribute:: column

      The column of the construct in its line, counting from 1, or ``0``.

.. data:: types

   The types of the parameters of a compiled function: ``double``, ``float``, ``int32``,
   ``uint32``, ``int64``, ``uint64``, ``bool``, ``string`` and ``bytes``, and ``nullable(T)``.
   ``list(T)``, ``set(T)``, ``map(K, V)`` and ``tuple(T, ...)`` name collections. A service's
   class, enumeration or structure names its own type. Each type value is a :class:`TypeValue`.

.. class:: TypeValue()

   A type of the values of a server side function.

   .. method:: cast(value)

      Returns the value. In a compiled function, gives the value this type.

API Reference
-------------

Each procedure and method, including the getter and setter of a property, returns a ``Promise``.
It resolves to a value of the return type listed for it.

.. include:: js/api.inc
