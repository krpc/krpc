JavaScript Client Design
========================

The internal design of the JavaScript client. This page builds on the :doc:`shared client design
</client-design>` and the :doc:`client specification </client-spec>`. Usage is in
the :doc:`JavaScript guide </js>`. Paths are relative to ``client/js/``.

Services and Declarations
-------------------------

Every service is built at connect from ``GetServices``. The client then has the members of the
server it connects to.

* A connect registers the types of every service, then the members of every class, then the
  services. A member of a class can belong to another service.
* Each member is a property set with ``defineProperty``. A member naming a type the client does not
  know is left out. An internal procedure is left out too.
* A kRPC property is a ``getX`` and a ``setX`` method. A JavaScript setter cannot return a
  ``Promise``, so ``vessel.name = "x"`` would drop the server's error.
* A procedure returning an event resolves with an ``Event``. One returning a stream, such as
  ``KRPC.AddStream``, rejects with a ``TypeError`` when called, as ``addStream`` makes that call
  itself.
* ``clientgen`` and ``js.tmpl`` in ``krpctools`` generate a TypeScript module per service, in
  ``src/services/``. Its classes and service are declarations. Its enumerations, structures and
  exceptions are values, made by ``declareEnumeration``, ``declareStruct`` and
  ``declareException`` in ``declarations.ts``.
* Each of those calls registers its type in ``registry.ts``, by service and name. ``connect`` uses
  the registered type where the server's definition matches, by the rule of the
  :doc:`Python client <python>`. An enumeration matches on its flags,
  names and numbers, a structure on its field names in order, and an exception on the JavaScript
  error it extends. A matched type is also the service's member, so
  ``conn.spaceCenter.SASMode === SpaceCenter.SASMode``.
* The package's own modules import ``declarations.js`` by its path, through clientgen's
  ``--client-module``. The package's name resolves to ``index.ts``, which imports the modules,
  and ``tsc`` leaves the import path as written.
* ``Connection`` merges with the interface ``Connection extends Services``, which holds the
  services a release ships. A mod's module adds its service to ``Services`` with module
  augmentation. ``DynamicConnection`` types any other service as ``any``.
* ``RemoteObject``, ``RemoteEnum`` and ``RemoteStruct`` take the type's name as a type parameter. A
  property keyed by a symbol that exists in types only holds it, so two classes with the same
  members do not mix.
* A procedure with default arguments has two overloads: the positional one, and the required
  arguments followed by an options object.

The library's ``tsconfig.json`` leaves out the Node.js types. A Node.js built-in used by mistake
then fails the type check, as it would in a browser.

Values
------

JavaScript has no operator overloading, so ``===`` is identity. The client interns the values a
program compares:

* ``ObjectCache`` holds one object per id per connection, as a ``WeakRef``. A
  ``FinalizationRegistry`` removes the entry of a collected object. An object then works as a
  ``Map`` key or a ``Set`` element.
* An ``Enumeration`` holds one ``RemoteEnum`` per number. A flags enumeration also holds each
  combination it has made, so combinations compare with ``===`` too.
* A structure value is a new object for each result. ``equals`` compares structures field by field.
* A structure's ``StructDeclaration`` holds its field names and the class of its values, and is
  shared by the connections that match a generated module. The ``Struct`` of a connection adds the
  field types from the server's definition.
* An enumeration value of another ``Enumeration`` with the same service and name passes as an
  argument by its number. ``equals`` compares two such values by number, as their ``===`` is
  false.

Structures are classes, so a plain object is never a value. A trailing plain object, with the
prototype ``Object.prototype`` or ``null``, then holds arguments by name. A protobuf message, such
as a ``ProcedureCall``, is a plain object too, and ``isMessage`` tells it apart.

``codec.ts`` encodes values with the ``BinaryReader`` and ``BinaryWriter`` of
``@bufbuild/protobuf``. Its 64-bit integers are ``bigint``, so a ``uint64`` above 2^53 round
trips.

Errors
------

Each connection builds a class for each exception a service lists, or uses the generated module's
class where it matches. ``KRPC.StreamRemovedException`` is ``StreamError``, in the connection and
the generated module, as the server ends a stream with it. One the server maps to a .NET
exception extends the matching JavaScript error, such as ``TypeError``, and the others extend
``ServerError``.

A class that extends a JavaScript error also has the ``service``, ``exception``, ``description``
and ``stackTrace`` of a ``ServerError``. Its declaration narrows ``instanceof`` to the JavaScript
error and ``ServerErrorDetails``.

Deprecated Members
------------------

``inheritDeprecation`` applies the shared rule to the service definitions at connect. The client
warns at the use of a member:

* A procedure, method or static method warns when it is called.
* A deprecated service, class, enumeration, structure, exception or enumeration value is a getter,
  which warns when it is read.
* A deprecated structure field is a getter on the value. The codec and ``equals`` read fields
  through ``RemoteStruct.fieldOf``, so a value from the server, or one passed to a call, gives no
  warning.

``Deprecations`` records the warnings a connection has given, so a loop does not repeat one. It
calls ``process.emitWarning`` when the runtime has it, and ``console.warn`` otherwise. It finds
``process`` through ``globalThis``, as the library has no Node.js types.

The generated modules mark each deprecated member with ``@deprecated``, for editors. Their
deprecated enumeration values and structure fields warn when read too, once per process, as
those values belong to no connection.

Ownership
---------

``connect`` returns a ``Connection``. Services and objects hold the connection's state, and none of
them owns it:

.. code-block:: js

   const conn = await connect();
   const vessel = await conn.spaceCenter.getActiveVessel();
   await conn.close();
   await vessel.getName();  // Rejects with a ConnectionError

References
^^^^^^^^^^

The ``Connection`` holds its ``Session``, and a ``WeakMap`` in ``connection.ts`` maps it to its
``Context``. The context holds the session, the ``Types`` and the service definitions. The session
holds the ``WebSocket``.

The objects the program keeps also hold the context. Each connection builds its own classes, whose
methods are closures over the context. A service holds those closures too. Enumeration and
structure values hold no connection.

The open socket holds the session through its event listeners. An unclosed connection is therefore
never collected.

Lifetime
^^^^^^^^

* The connection closes on ``close()``, when the server closes the socket or sends a
  ``disconnect``, or when a message breaks the protocol.
* ``close()`` sends a ``disconnect`` and a close frame, then waits up to a second for the server to
  close the socket. A second ``close()`` sends nothing.
* After a close or a failure, ``Session.ended`` holds the ``ConnectionError``. Each call waiting for
  a response rejects with it, and so does each later call. Each stream ends with it, or for a
  failure with a ``ConnectionError`` whose cause is that error.
* ``Connection`` and objects have no finalizer. An object is an id and its connection's classes, and
  sends no release to the server.

Event Loop
----------

The client runs on one thread, like the :doc:`single-threaded clients </client-design>`, but no call
reads the socket. The socket's ``message`` event reads each message, so several calls can wait at
once, and stream updates arrive between calls:

* A call adds its pending slot, keyed by request id, then sends its request. ``WebSocket.send``
  queues the whole message, so requests never interleave.
* The message handler holds no ``await``. No other code runs while it handles a message, and it
  settles the ``Promise`` of the call the response is for.
* A response with an unknown id is a protocol error, as no call can abandon its slot. JavaScript
  has no way to interrupt a call.
* The handler passes a ``stream_update`` to the ``StreamManager``, and skips any message case it
  does not know. A ``connection_request``, ``connection_response`` or ``request`` is a protocol
  error.
* A WebSocket message arrives whole, so a message never stalls and the client has no stall timeout.
  ``setTimeout`` bounds the upgrade and the handshake together.

Streams and Events
------------------

``streams.ts`` holds the streams. A ``StreamState`` is the state the handles of one stream share,
and the ``StreamManager`` holds a connection's states by id. ``Stream`` and ``Event`` are the
handles. A copy of a handle is a reference to the same object.

Adding a Stream
^^^^^^^^^^^^^^^

``addStream`` takes a function that returns one call, as C#'s ``AddStream`` takes a lambda:

.. code-block:: js

   const altitude = await conn.addStream(() => flight.getMeanAltitude());

* The client runs the function in a capture mode. A member called then builds its
  ``ProcedureCall``, records it and returns a ``Promise`` that never settles, in place of sending
  the call. ``addStream`` checks that the function returned that ``Promise``, from one call.
* The member warns of its deprecation as it would for a call, so a stream of a deprecated member
  warns too.
* The stream's value type is the call's result type, so TypeScript infers ``Stream<number>`` from
  the function's ``Promise<number>``.
* A stream is added on the connection ``addStream`` is called on, whichever connection the object
  came from.

A stream's first update can follow the response to ``AddStream`` in the same read. The session's
``call`` takes a function to run on the result in the message handler, before the next message.
It registers the stream there. A call of a procedure returning an event registers the event's
stream the same way.

Values and Waits
^^^^^^^^^^^^^^^^

The message handler stores each value and wakes the waits, with no ``await``, so a call that
returns sees each value sent before its response. ``get`` waits for the first value, and the
``value`` getter reads the latest one at once. ``wait`` resolves on the next value after it
starts.

An event counts the true values of its stream as its firings. Each ``Event`` keeps the count its
last wait returned for, and a wait returns at once for a later firing. A remove or a close discards
an unseen firing, and an end by the server keeps it.

The client has no ``conditions`` profile. A condition holds a stream's value still while a thread
reads it and waits. A run of reads with no ``await`` between them already sees one state, and no
other code runs during it.

Callbacks
^^^^^^^^^

A microtask after the message handler runs the callbacks of the update's values, then the update
callbacks. A callback added in between gets the value through its first call.

* **A callback runs one call at a time.** A callback that returns a ``Promise`` is busy until it
  settles. A value that arrives meanwhile waits, and a newer one replaces it.
* **The first call is made before** ``addCallback`` **returns**, with the value the stream has. No
  ``await`` lies between reading the value and the call, so no update passes a newer one first.
* **End callbacks of a server ending wait for the value callbacks.** A value that arrived before
  the ending reaches the callbacks first. A remove or a close drops the waiting values, and calls
  the end callbacks at once.
* **A remove or a close calls the end callbacks itself.** A failure of the connection calls them
  in a microtask, so the message handler runs no program code.
* **An error a callback throws is reported**, through ``reportError`` where the runtime has it and
  ``console.error`` otherwise. JavaScript has no kind of error apart from its ordinary ones, so
  none propagates.

Lifetime of a Stream
^^^^^^^^^^^^^^^^^^^^

A stream runs until it is removed, or its connection closes. A collected handle leaves its stream on
the server, as in the other clients. A collection happens at no predictable time, so a stream kept
only for its callbacks would stop at random.

``Stream`` and ``Event`` have ``Symbol.asyncDispose``, so an ``await using`` declaration removes the
stream at the end of its block. Removing a stream ends it before the ``RemoveStream`` call, so a
value that arrives during the call is dropped.

Server Side Functions
---------------------

``functions.ts`` adds the server side functions built with the expression API. The client has no
function compiler yet.

* ``getCall`` takes a function returning one call, as ``addStream`` does, and returns the
  ``ProcedureCall`` it captures. It is synchronous, as the capture sends nothing.
* ``addStream`` takes a ``KRPC.Expression`` in place of a function, and ``addEvent`` takes one
  too. They call ``KRPC.AddFunctionStream`` and ``KRPC.AddEvent``, which the services leave out as
  internal. The stream is registered in the message handler, as for ``AddStream``.
* **The function supplies its types.** ``run`` and ``addStream`` read the types of the parameters
  and the value from the ``KRPC.Expression``, as in the :doc:`Lua client <lua>`. They read them
  once per function, cached in a ``WeakMap`` keyed by the object. TypeScript erases a type
  argument, so ``run<T>`` and ``addStream<T>`` name a type the client cannot check.
* A function with parameters has no stream or event, as nothing passes its arguments. The client
  rejects one with a ``TypeError`` before calling the server.

Transports
----------

WebSockets is the only transport, through the global ``WebSocket``, which Node.js 22 and browsers
both have. Its ``binaryType`` is ``arraybuffer``, and a text message ends the connection as
malformed.

Mapping the Spec to JavaScript
------------------------------

.. list-table::
   :header-rows: 1

   * - Spec
     - JavaScript
   * - Calls
     - A ``Promise`` per call, resolved with the result
   * - Streams, events
     - ``Connection.addStream``, ``Connection.addEvent``, ``Stream`` and ``Event``, whose reads and
       waits return a ``Promise``. The ``value`` getter reads a stream's latest value at once
   * - Stream error
     - ``StreamError``, extending ``RemoteError``. It is also ``KRPC.StreamRemovedException``
   * - Callbacks
     - Functions called in a microtask after the message handler, one call at a time each
   * - Scoping construct
     - ``await using``
   * - Closing a connection
     - ``Connection.close()``, which returns a ``Promise``
   * - Remote error, and the connection and encoding errors
     - ``RemoteError``, and ``ConnectionError`` and ``EncodingError`` extending it
   * - Server error with no mapped type
     - ``ServerError``, the base of each exception class a service lists. For an exception with no
       class, ``Service.Name:`` comes before the description
   * - Server exception with a mapped type
     - A class on the service, extending only ``TypeError`` or ``RangeError``
   * - Argument error
     - ``TypeError``, or ``RangeError`` for a number outside the range of its type
   * - Null object, nullable value
     - ``null``. ``undefined`` at an argument takes its default
   * - Member names
     - lowerCamel, as in ``httpServer`` for ``HTTPServer``. A property is ``get`` or ``set``
       and the name, as in ``getRCSThrust``
   * - Named arguments
     - A plain object after the positional arguments
   * - 64-bit integers
     - ``bigint`` results. Arguments take a ``number`` or a ``bigint``
   * - List, tuple, set, dictionary
     - ``Array``, ``Array``, ``Set``, ``Map``
   * - Timeouts
     - Milliseconds, with ``Infinity`` for no timeout
   * - Static method
     - A function on the class, reached through the service of a connection
   * - Interrupting a call or a wait, hashes
     - Unsupported, as JavaScript has neither
   * - Fatal errors from callbacks
     - Unsupported, as JavaScript has no kind of error apart from its ordinary ones
   * - Conditions
     - None. See `Values and Waits`_
   * - Getting a call as a value
     - ``Connection.getCall``, given a function returning one call
   * - Value type of a server side function
     - The function's own type, read from the server. A null value is ``null`` at any type
   * - Flags enumeration
     - A ``RemoteFlags``, combined with ``union``, ``intersection``, ``difference`` and
       ``contains``, as JavaScript has no operator overloading
