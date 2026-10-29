.. default-domain:: py
.. highlight:: py

Python Client
=============

This guide describes how to install the Python client, and the features it provides. The
:ref:`python-client-reference` describes each function and class.

Installing the Library
----------------------

The library can be found on `PyPI <https://pypi.python.org/pypi/krpc>`_ or
`downloaded from GitHub <https://github.com/krpc/krpc/releases>`_.

To install using pip:

.. code-block:: bash

   pip install krpc

Connecting to the Server
------------------------

The game must be running with the server started. :ref:`getting-started-server` in the
getting started guide shows how to install and start it.

The client connects over TCP/IP or over a local socket. Each server in the server window uses one
protocol, and accepts connections over that protocol only. Pick the protocol when you add the
server, and connect with the matching function.

.. note::

   A server with "Auto-accept new clients" turned off answers only once the player approves the
   connection in game. The connection fails after 5 seconds by default. Pass
   ``handshake_timeout=None`` to wait indefinitely for the player.

Over TCP/IP
^^^^^^^^^^^

Select "Protobuf over TCP/IP" as the server's protocol. :func:`~krpc.connect` connects to it, and
returns a :class:`krpc.client.Client`. You make every call through this object. The following
connects to a server with the default settings, address ``127.0.0.1`` and port 50000, and prints
its version:

.. literalinclude:: /scripts/python/client/Connecting1.py

:func:`~krpc.connect` also takes the address and port of the server, and a name for the connection.
These match the address and port shown in the server window. The name appears in the server window
in the game:

.. literalinclude:: /scripts/python/client/Connecting2.py

TCP/IP works over a network, so your program can run on a different machine from the game.

Over a Local Socket
^^^^^^^^^^^^^^^^^^^

Select "Protobuf over local socket" as the server's protocol. :func:`~krpc.connect_local` connects
to it, and returns a :class:`krpc.client.Client` as :func:`~krpc.connect` does. The following
connects to a server with the default settings, at the default socket path for your platform, and
prints its version:

.. literalinclude:: /scripts/python/client/ConnectingLocal.py

:func:`~krpc.connect_local` also takes a ``path``, for a server with another socket path. The path
matches the one shown in the server window.

A local socket works only on the machine running the game. It has lower overhead than TCP/IP, so
it is faster.

Closing the Connection
^^^^^^^^^^^^^^^^^^^^^^

Use the connection in a ``with`` statement to close it at the end of the block:

.. literalinclude:: /scripts/python/client/Connecting3.py

:meth:`~krpc.client.Client.close` closes a connection explicitly.

Calling Remote Procedures
-------------------------

The server provides *procedures* for your program to call, grouped into *services*.

The following gets the active vessel from :attr:`SpaceCenter.active_vessel`, an object of type
:class:`SpaceCenter.Vessel`. It sets the vessel's name, then prints its altitude:

.. literalinclude:: /scripts/python/client/RemoteProcedures.py

Each service is an attribute of the connection, such as ``conn.space_center`` for the
``SpaceCenter`` service. From an interactive terminal, ``help(conn.space_center)`` lists the
classes, enumerations, procedures and properties of the service. ``help(conn.space_center.Vessel)``
does the same for a class.

Handling Errors
---------------

A procedure that fails on the server raises an exception in your program. The following catches
the error from loading a save that does not exist:

.. literalinclude:: /scripts/python/client/Errors.py

The exception you catch depends on the error:

* A standard error, such as an invalid argument, raises a subclass of the matching Python
  exception. The example catches ``ValueError``, as a missing save raises
  ``conn.krpc.ArgumentException``.
* An error particular to a service raises a class of that service, such as
  ``conn.krpc.ObjectDestroyedException``.
* An error the client has no class for raises :class:`krpc.error.RPCError`.
* A call made once the connection has closed or failed raises
  :class:`krpc.error.ConnectionError`.

Remote Objects
--------------

Many procedures return an object identifying something in the game, such as a vessel or a part.
The object holds no copy of the game's state. It is just a reference to it. Each call through it
reads the game as it is now:

.. literalinclude:: /scripts/python/client/RemoteObjects.py

Two objects for the same thing in the game compare equal, and can be used as dictionary keys.

Once the thing an object identifies is destroyed, such as a part that explodes, each call through
it raises ``conn.krpc.ObjectDestroyedException``.

An object for a vessel or a part keeps working when you load a save, quickload or revert, and
then reads the loaded game. Other objects depend on what they identify. A maneuver node, for
example, is destroyed by a load.

A vessel far from the active vessel is *unloaded*, and the game keeps no parts for it. The vessel
itself reads normally, but a call on one of its parts raises
``conn.krpc.ObjectNotLoadedException``. The part works again once the vessel comes back into
range.

.. seealso:: :doc:`/user-guide/object-lifetime` describes how each kind of object behaves in more
             detail.

.. _python-client-streams:

Streaming Data
--------------

A common use of kRPC is to read data from the game continuously. The following does so by calling
a remote procedure in a loop, printing the position of the active vessel:

.. literalinclude:: /scripts/python/client/Streaming1.py

Each call sends a request to the server and waits for its response. A *stream* is more efficient.
The server runs the call repeatedly, and sends your program the result each time it changes.

The following does the same as above using a stream:

.. literalinclude:: /scripts/python/client/Streaming2.py

:meth:`~krpc.client.Client.add_stream` creates the stream from the method and its arguments, and
returns a :class:`krpc.stream.Stream`. Calling it returns the stream's latest value.

A property is streamed with ``getattr`` and the property's name:

.. literalinclude:: /scripts/python/client/Streaming4.py

A stream can be made of any procedure or property that returns a value. Each call to
:meth:`~krpc.client.Client.add_stream` creates another stream.

A stream runs until you remove it with :meth:`~krpc.stream.Stream.remove`, your program drops its
last reference to it, or the connection closes. Keep a reference to a stream for as long as you
need its values or its callbacks. :meth:`~krpc.client.Client.stream` creates a stream in a
``with`` statement, which removes it at the end of the block:

.. literalinclude:: /scripts/python/client/Streaming3.py

By default, the server sends a stream's value each time it changes. Set
:attr:`~krpc.stream.Stream.rate` to limit how many times a second the stream updates:

.. literalinclude:: /scripts/python/client/StreamRate.py

Waiting for Changes
^^^^^^^^^^^^^^^^^^^

Each stream has a condition variable, a ``threading.Condition`` from the Python standard library.
:meth:`~krpc.stream.Stream.wait` blocks until the stream's value changes. The following waits for
the abort button to be pressed:

.. literalinclude:: /scripts/python/client/ConditionVariables.py

Hold the condition when you call :meth:`~krpc.stream.Stream.wait`, as the ``with`` statement does
here. While you hold it, the value changes only when you wait, so the loop misses no updates. An
ending reaches you the same way: the wait returns, and your next read or wait raises the exception
that ended the stream.

:meth:`~krpc.stream.Stream.wait` takes a timeout in seconds, and returns ``False`` when it elapses
before an update.

Callbacks
^^^^^^^^^

A callback is a function the client calls with each new value of a stream. The following adds
two callbacks to a stream of the abort button:

.. literalinclude:: /scripts/python/client/Callbacks.py

A callback added to a stream that already has a value is called with that value straight away.
It is then called with each new value, in order.

.. note::

   Each connection has a callback thread, which runs the callbacks of its streams one at a time. A
   callback can make remote procedure calls. Other callbacks and waits are held up while it runs, so
   return promptly, and do not wait on a stream in a callback. For a long running task, start a
   worker thread from the callback.

.. _python-client-stream-endings:

Stream Endings
^^^^^^^^^^^^^^

A stream ends when its procedure raises an exception, when the server removes it, when you remove
it, or when the connection closes. Calling the stream or waiting on it then raises the exception
that ended it. The following streams the temperature of a part until the part is destroyed:

.. literalinclude:: /scripts/python/client/StreamEnd.py

The exception is the one the procedure raised, or one of the following:

* ``conn.krpc.StreamRemovedException`` when the server removes the stream, such as for an
  :ref:`event <python-client-events>` that has finished.
* :class:`krpc.error.StreamError` when you remove the stream from the client.
* :class:`krpc.error.ConnectionError` when the connection closes or fails.

An exception for an object that is not loaded, or for a procedure not available in the current
game scene, does not end the stream. It pauses the stream's updates, and the stream keeps its last
value. The updates resume once the object or the scene is available.

.. seealso:: :doc:`/user-guide/object-lifetime` describes when each kind of object is not loaded,
             and how streams behave around a load.

:meth:`~krpc.stream.Stream.add_end_callback` adds a callback that is called once when the stream
ends, with the exception. The following prints the temperature of a part with a callback, and
prints the exception when the stream ends:

.. literalinclude:: /scripts/python/client/EndCallback.py

Server Side Functions
---------------------

A server side function is a Python function or lambda in your script that runs inside the game.
The client compiles it and sends it to the server, which runs it there. The following reads the
vessel's altitude and speed together:

.. literalinclude:: /scripts/python/client/RunFunction.py

:ref:`python-client-compiling-functions` describes what a function can contain. The :ref:`server
side functions guide <server-side-functions>` shows how to build larger functions.

Running a Function
^^^^^^^^^^^^^^^^^^

:meth:`~krpc.client.Client.run` runs a function once, and returns its value. The server runs the
whole function within one physics tick, so every value it reads comes from the same moment in the
game. In the example, the altitude and the speed are consistent with each other, which two
separate calls cannot guarantee.

Streaming a Function
^^^^^^^^^^^^^^^^^^^^

Pass a function to :meth:`~krpc.client.Client.add_stream` to stream its value. The server
evaluates the function on each update, within one physics tick. The following streams the
vessel's altitude in kilometers:

.. literalinclude:: /scripts/python/client/CompiledFunctionStream.py

A function stream ends on an exception its function raises, just like an
:ref:`ordinary stream <python-client-stream-endings>`. Unlike an ordinary stream, it also ends on
``ObjectNotLoadedException`` from the ``KRPC`` service, raised while the game loads or when a vessel
unloads. The server cannot pause a function partway through, and running it again would repeat
its effects. To keep a function running through a load, catch the exception in the function:

.. literalinclude:: /scripts/python/client/NotLoadedStream.py
   :language: python
   :start-after: [docs]
   :end-before: [/docs]

.. _python-client-events:

Events
------

A common use of kRPC is to wait for something to happen in the game, such as the vessel reaching
an altitude. An *event* does this efficiently. The server checks a condition, written as a server
side function, and tells your program when it becomes true. The following waits
until the vessel's altitude exceeds 1000 meters:

.. literalinclude:: /scripts/python/client/CompiledEvent.py

:meth:`~krpc.client.Client.add_event` creates the event, and :meth:`~krpc.event.Event.wait` waits
for it to fire. The server checks the condition every physics tick, so the event catches a
condition that holds for only a moment, which a loop of calls can miss.

As with a stream, hold the event's condition when you call :meth:`~krpc.event.Event.wait`. A wait
returns straight away if the event has fired since your last wait, so a firing between two waits is
not missed.

Event Callbacks
^^^^^^^^^^^^^^^

An event can also call a callback each time it fires. The callback takes no arguments:

.. literalinclude:: /scripts/python/client/EventCallback.py

Callbacks for events run on the connection's callback thread, as the callbacks of a stream do.

Event Endings
^^^^^^^^^^^^^

An event ends in the same ways as a :ref:`stream <python-client-stream-endings>`. Waiting on an
event that has ended raises the exception that ended it.

When the event fires just before the server ends it, your next wait returns for that firing first.
Removing the event, dropping your program's last reference to it, or closing the connection, ends
it at once.

:meth:`~krpc.event.Event.add_end_callback` adds a callback that is called once when the event ends,
with the exception, just like an ordinary stream.

Threading
---------

A connection can be shared between threads. Each thread makes calls over it as it would alone,
and each call returns its own result. The following prints the altitude and speed of the vessel
from two threads:

.. literalinclude:: /scripts/python/client/Threads.py

Streams and events can also be used from any thread:

* Several threads can read a stream, or wait on a stream or an event. A change wakes every thread
  waiting for it.
* A thread holding a stream's condition does not stop other threads from making calls.
* Callbacks run on the connection's callback thread, one at a time, and never on your program's
  threads. A callback added to a stream that already has a value is different. Its first call, with
  that value, might run on your thread, before :meth:`~krpc.stream.Stream.add_callback` returns.
  Protect state that a callback shares with your program's threads, such as with a
  ``threading.Lock``.

When one thread closes the connection, or the connection fails, the calls and waits of the other
threads end with :class:`krpc.error.ConnectionError`. A wait on a stream or an event that another
thread removes ends with the exception for a removed stream.

Deprecated Members
------------------

A member of a service can be deprecated, when a newer member replaces it. A deprecated member
still works, but may be removed in a future release, so move to its replacement.

The API reference and the member's docstring mark it as deprecated, and name the replacement.
Calling it emits a ``DeprecationWarning`` naming the replacement too. Python hides
``DeprecationWarning`` by default. Run Python with ``-W default``, or use the ``warnings`` module,
to see them.

.. _python-client-compiling-functions:

Writing Server Side Functions
-----------------------------

The client includes a compiler, which converts a Python function or lambda into a server side
function automatically when you pass it to :meth:`~krpc.client.Client.run`,
:meth:`~krpc.client.Client.add_stream` or :meth:`~krpc.client.Client.add_event`. This section
describes what a function can contain, and how to compile one ahead of time.

Remote Calls and Constants
^^^^^^^^^^^^^^^^^^^^^^^^^^

The compiler translates the function's source code into a server side function that computes
the same result on the server, inside the game. Each part of the function either runs on the server
or becomes a constant:

* Attribute accesses and method calls on remote objects and services become calls in the
  function, made again on each evaluation. This includes calls on the elements of a collection
  in a comprehension. The procedures of the ``KRPC`` service cannot be called. They build and
  run server side functions and add streams, which a function would repeat on every evaluation.
* Reading an attribute of a structure a service defines reads that field of it on the server.
  Calling the structure type builds one there, with its field values given by position, by
  field name, or both.
* Captured variables, literals and any sub-expression that makes no remote call are evaluated
  once, when the function is compiled, and embedded as constants. A client side function called
  there is a compile error if it makes a remote procedure call, as the call would run only once,
  when compiling. To use a value read once, read it before the function and capture the variable.

The compiler raises :class:`krpc.error.FunctionCompilationError` for code that would run only once,
when compiling, where you would expect it to run on every evaluation:

* Reading a stream inside a function, as the stream would be read once.
* A call to a client side function written as a statement, such as ``time.sleep(1)``. The
  builtins under :ref:`python-client-builtins`, such as ``print``, run on the server instead.
* A Python function passed to :meth:`~krpc.client.Client.add_event` or
  :meth:`~krpc.client.Client.add_stream` that makes no remote procedure call and uses no state
  variable, as its value is the same on every update. A function compiled with
  :meth:`~krpc.client.Client.compile` first is not checked.

Operators
^^^^^^^^^

* Operators, conditional expressions (``a if condition else b``), indexing, slices without a
  step, f-strings without format specifiers, ``in``, ``is None`` and ``is not None`` compile to
  the matching expression operators.
* ``/`` is true division, ``//`` is floor division, and the result of ``%`` takes the sign of
  the divisor, as in Python.
* ``and`` and ``or`` short-circuit on the server, and produce the operand that decides the
  result. ``&`` and ``|`` evaluate both operands.
* ``+`` adds numbers, and concatenates two strings or two lists. The other arithmetic operators
  and the ordering comparisons take numbers only.
* ``==``, ``!=`` and ``in`` take values of the same kind. Numbers of different types widen to a
  common type.

None and Truth
^^^^^^^^^^^^^^

* ``None`` takes its type from where it is used: a parameter, the other branch of a
  conditional, the other elements of a list, the function's other returns, or the variable's
  other assignments. Elsewhere, give it a type with an annotation or ``typing.cast``, for
  example ``cast(Optional[str], None)``.
* A condition tests truth as Python does. ``None`` is false, a number is true when it is not
  zero, and a string, bytes or a collection when it is not empty. Any other value is true.

Collections
^^^^^^^^^^^

* Tuple, list, set and dictionary displays compile to the matching expression operators.
* Comprehensions compile to the server's collection operations, including dictionary
  comprehensions and several ``for`` clauses with conditions.
* A comprehension target can unpack a tuple, as in ``[key for key, value in d.items()]``.
* Iterating over a dictionary iterates over its keys, and ``in`` tests its keys. ``d.items()``
  gives its key and value pairs, ``d.keys()`` its keys and ``d.values()`` its values.
* A string cannot be iterated over.

.. _python-client-builtins:

Builtin Functions and Methods
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

* The builtin functions ``len``, ``sum``, ``any``, ``all``, ``reversed``, ``abs``, ``round``,
  ``int``, ``float``, ``str``, ``range``, ``enumerate`` and ``zip`` of two collections compile
  to server side operations. ``sorted``, ``min`` and ``max`` take an optional ``key`` lambda.
  ``min`` and ``max`` take a collection or several values, which are numbers unless a ``key`` is
  given. ``sorted`` also orders strings and tuples.
* The string methods ``upper``, ``lower``, ``strip``, ``lstrip``, ``rstrip``, ``replace``,
  ``split``, ``find``, ``startswith``, ``endswith`` and ``join`` compile to server side
  operations. ``strip``, ``lstrip`` and ``rstrip`` take no argument, and ``split`` takes a
  separator.
* A ``math`` module function with server side arguments compiles to a :mod:`StdLib` procedure.
* ``print`` writes a line to the game's log with :meth:`StdLib.print`. An argument computed on the
  server is a string, a number or a bool. A constant argument is formatted on the client, as
  ``str()`` formats it. ``sep`` is a constant.

Statements
^^^^^^^^^^

A Python function can also contain statements, which run on the server. This one shuts down
the engines that have run out of fuel, and returns how many it shut down:

.. literalinclude:: /scripts/python/client/CompiledStatements.py

A function body can contain:

* ``if``, ``elif`` and ``else``, ``while`` and ``for`` loops with ``break`` and ``continue``,
  and early returns.
* Local variables, and assignment expressions (``:=``).
* Assignment to the properties of remote objects and services.
* Remote calls written as statements, for their effects.
* Assignment that unpacks a tuple, as in ``a, b = b, a``, and a ``for`` target that does.
* ``+=`` and the other augmented assignments. ``+=`` on a list extends it in place.
* Assignment to a list or dictionary element.
* ``append`` on a list and ``add`` on a set.
* ``del`` of a list index or a dictionary key, ``remove`` on a list or a set, ``discard`` on a
  set, and ``clear`` on any collection.
* Local function definitions with annotated parameter types, and lambdas without parameters
  assigned to names. Calls to them run on the server.
* ``raise``, and ``try`` with ``except`` and ``finally`` clauses.

A function that returns a value must end with a ``return`` or ``raise`` statement, an ``if`` or
``try`` that returns or raises on every path, or a ``while True`` loop with no ``break``.

Variables and Types
^^^^^^^^^^^^^^^^^^^

* A variable takes its type from its first assignment. Annotate an assignment of an empty
  collection, for example ``result: list[int] = []``. An empty collection passed as an argument,
  or used beside a typed value, takes that type.
* An annotation names ``int``, ``float``, ``bool`` or ``str``, a ``list``, ``set``, ``dict`` or
  ``tuple`` of types, or a class, enumeration or structure a service defines.
* ``typing.cast(T, value)`` converts the value to ``T`` on the server. A float cast to ``int``
  is truncated toward zero, where assigning it to an ``int`` variable is an error.
* A variable assigned ``None`` can hold ``None``. A variable holding a string, an object or a
  collection can also be ``None`` when a value assigned to it can.
* An annotation such as ``Optional[int]`` or ``int | None`` allows ``None`` for any type.
  Assigning ``None`` to a variable or a parameter annotated with a type that does not allow it is
  an error.
* A ``None`` reaching a variable, a parameter or a set that does not take one raises an error
  when the function runs.
* A captured collection is copied to the server, so assigning to or deleting one of its
  elements is an error.

Scope
^^^^^

* A name assigned anywhere in the function is local to all of it, as in Python.
* Reading a variable is an error unless every path to the read assigns it first. An assignment
  in a loop or a ``try`` body does not count after the loop or the ``try``.
* A local function reads a variable when it is called, so the variable can be assigned after the
  function's definition, if every path to each call assigns it.
* A local function defined or redefined in a nested block can only be called within that
  block.

State Variables
^^^^^^^^^^^^^^^

A captured variable declared ``nonlocal`` in the function is a state variable, which keeps its
value from one evaluation to the next. This stream gives the highest altitude the vessel has
reached:

.. literalinclude:: /scripts/python/client/StateVariable.py
   :language: python
   :start-after: [docs]
   :end-before: [/docs]

State variables follow these rules:

* It starts with the value it holds when the function is compiled. A compiled function keeps its
  value between runs, as does a stream or an event.
* The value belongs to the compiled function, so two streams of one function advance it twice per
  update. A function compiled again on each call starts it again.
* A local function cannot declare one.
* Its type comes from its starting value. To give it another type, cast it in a statement of the
  function's body before any other use of it, for example ``values = cast(List[int], values)``
  for a list that starts empty, or ``last = cast(Optional[float], last)`` for a value that starts
  as ``None``.

Exceptions
^^^^^^^^^^

Exceptions map to the exceptions the server raises:

* ``raise`` takes an exception a service declares, or one of the builtins below.
* An exception a service declares is named by its class, such as
  ``conn.krpc.ObjectNotLoadedException``.
* ``RuntimeError`` stands for ``KRPC.InvalidOperationException``.
* ``ValueError`` stands for the three argument exceptions.
* ``TypeError``, ``KeyError``, ``IndexError``, ``ZeroDivisionError``, ``NotImplementedError``,
  ``TimeoutError`` and ``OverflowError`` stand for the matching kRPC exceptions.
* ``raise Exception`` raises a ``KRPC.Error``, with the message ``str()`` gives its argument.
* An ``except`` clause naming a base class, such as ``LookupError``, catches the exceptions
  derived from it. ``except RuntimeError`` catches every other exception the services declare,
  apart from ``KRPC.Error``.
* An ``except`` clause can name a tuple of exceptions. Its ``as`` name holds the exception's
  message.
* ``except Exception``, a bare ``except`` and a tuple naming ``Exception`` catch every exception.
* A ``break``, ``continue`` or ``return`` cannot leave a ``finally`` clause.

Differences from Python
^^^^^^^^^^^^^^^^^^^^^^^

Some results differ from Python's, as the server computes with fixed size numbers and its own
collection operations:

* Integer arithmetic wraps around on overflow.
* A 32-bit unsigned integer a procedure returns is negated, or inverted with ``~``, as a 64-bit
  signed integer, so the result is negative as in Python. A 64-bit unsigned integer cannot be
  negated or inverted.
* A float passed where an integer is expected is an error. Convert it with ``int()``.
* ``int()``, ``round()``, ``math.floor`` and ``math.ceil`` of a float give a 32-bit integer.
* A float divided by zero gives ``inf`` or ``nan``.
* ``sum`` adds floats in order, where Python 3.12 and later compensates for rounding. For
  example, ``sum([0.1] * 10)`` gives ``0.9999999999999999``.
* A math function outside its domain gives ``nan`` or ``inf`` where Python raises an error. For
  example, ``math.sqrt(-1)`` gives ``nan`` and ``math.log(0)`` gives ``-inf``.
* A generator expression is computed in full where it is written. ``any`` and ``all`` stop early
  only over a generator expression written as their argument.
* ``sum``, ``min`` and ``max`` skip a ``None`` among the values, and ``sorted`` puts it first.
  Python raises ``TypeError`` for each.
* A negative index counts from the end only when it is a constant. A negative power of an
  integer is a fraction only when the power is a constant. A slice bound cannot be a negative
  constant.
* The keys of a dictionary are integers, strings or ``bool`` values.
* Bytes cannot be indexed, measured with ``len`` or searched with ``in``.
* ``str()`` and an f-string take a string, a number or a bool. A float uses the server's
  formatting, so ``1.0`` becomes ``1``.

Unsupported Constructs
^^^^^^^^^^^^^^^^^^^^^^

A construct with no server side equivalent raises :class:`krpc.error.FunctionCompilationError`
naming it. Examples are a format specifier, ``str.format``, and a call to a client side function
with server side arguments.

Compiling Ahead of Time
^^^^^^^^^^^^^^^^^^^^^^^

Pass a Python function or lambda to :meth:`~krpc.client.Client.compile` to compile it into a server
side function. :meth:`~krpc.client.Client.add_event`, :meth:`~krpc.client.Client.add_stream` and
:meth:`~krpc.client.Client.run` take the result, and run it without compiling it again. The
following compiles a function once, and runs it every second:

.. literalinclude:: /scripts/python/client/CompiledFunction.py

Given a Python function or lambda, :meth:`~krpc.client.Client.add_event`,
:meth:`~krpc.client.Client.add_stream` and :meth:`~krpc.client.Client.run` compile it again on every
call, so its captured variables take their current values. A compiled function keeps the values from
when it was compiled. Compiling makes a remote procedure call per node.

To use a function repeatedly, compile it once with :meth:`~krpc.client.Client.compile` and pass the
result. :meth:`~krpc.client.Client.compile` also compiles the function on the server, so its first
run is as fast as later ones.

:meth:`~krpc.client.Client.compile` also works as a decorator, which replaces the function with
its compiled form:

.. literalinclude:: /scripts/python/client/CompileDecorator.py
   :language: python
   :start-after: [docs]
   :end-before: [/docs]

Parameters
^^^^^^^^^^

A ``def`` can take parameters, each with a type annotation. :meth:`~krpc.client.Client.run` passes
its arguments to them, with defaults and keyword arguments as in Python:

.. literalinclude:: /scripts/python/client/CompiledFunctionArguments.py
   :language: python
   :start-after: [docs]
   :end-before: [/docs]

Only a ``def`` takes parameters. An event or a function stream runs its function with no arguments.
Passing :meth:`~krpc.client.Client.add_event` or :meth:`~krpc.client.Client.add_stream` a ``def``
with parameters raises :class:`krpc.error.FunctionCompilationError`, and a compiled function with
parameters raises ``StreamError``.

Service Stubs
-------------

The client includes code for each service that ships with kRPC, generated from the services of
one kRPC release. These *stubs* give your editor the classes, procedures and type hints of each
service.

The stubs match the server from the same release, so install the client version that matches your
server. With another version, a procedure that was removed or renamed raises an error when you
call it. A parameter or return type that changed is not checked, so a call can send or return a
wrong value with no error.

When you connect, the client fills in what the stubs lack from the service definitions the server
sends:

* A service with no stub, such as one added by another mod, is generated at runtime.
* A class member that only the server declares is added to the stub's class.
* A structure the server declares with fewer fields than the stub is read as a plain named tuple,
  with a warning.

To generate every service at runtime instead, pass ``use_pregenerated_stubs=False``:

.. code-block:: python

   conn = krpc.connect(use_pregenerated_stubs=False)

The services then match the server you connect to, whatever its version, but have no type hints.

.. _python-client-reference:

Client API Reference
--------------------

.. function:: krpc.connect([name=None], [address='127.0.0.1'], [port=50000], [use_pregenerated_stubs=True], [timeout=None], [handshake_timeout=5], [stall_timeout=30])

   Connects to a kRPC server, and returns a :class:`krpc.client.Client`.

   :param str name: A name for the connection, shown in the server window in the game.
   :param str address: The address of the server, as a host name or an IP address. Defaults to
                       ``'127.0.0.1'``.
   :param int port: The port of the server. Defaults to 50000.
   :param bool use_pregenerated_stubs: Whether to use the service stubs bundled with the client,
                           which include type hints. A service with no bundled stub, or every
                           service when this is ``False``, is generated at runtime. Defaults to
                           ``True``.
   :param float timeout: How many seconds to wait for the connection to open. Defaults to
                           ``None``, which waits indefinitely. Without a timeout, a network that
                           drops the connection attempt leaves the client waiting.
   :param float handshake_timeout: How many seconds to wait for the server to accept the
                           connection. Defaults to 5. ``None`` waits indefinitely, such as for a
                           server that asks the player to approve each connection.
   :param float stall_timeout: How many seconds a message from the server can go without a new
                           byte, once it has started to arrive, before the connection fails.
                           Defaults to 30. ``None`` waits indefinitely.

   Raises :class:`krpc.error.ConnectionError` if the connection fails or the server refuses it,
   and ``ValueError`` for a timeout that is not positive.

.. function:: krpc.connect_local([name=None], [path], [use_pregenerated_stubs=True], [handshake_timeout=5], [stall_timeout=30])

   Connects to a kRPC server on the same machine over a local socket, and returns a
   :class:`krpc.client.Client`. The server must use the local socket protocol.

   A script that makes many calls in quick succession completes more of them per physics update
   than over TCP/IP. A script that makes a call and then waits runs at the game's update rate
   either way.

   Local sockets are available on Linux, macOS, and Windows 10 1803 and Windows Server 2019 or
   later.

   :param str name: A name for the connection, shown in the server window in the game.
   :param str path: The path of the server's socket, shown in the server window in the game.
                    Defaults to the path the server uses unless it was configured with another.
   :param bool use_pregenerated_stubs: As for :func:`krpc.connect`.
   :param float handshake_timeout: As for :func:`krpc.connect`.
   :param float stall_timeout: As for :func:`krpc.connect`.

   Raises :class:`krpc.error.ConnectionError` and ``ValueError`` as :func:`krpc.connect` does.

.. function:: krpc.get_object_id(obj)

   The identifier of a remote object on the server, e.g. to pass to
   :meth:`KRPC.Expression.constant_object`. Raises ``ValueError`` if the object is ``None``.

   :param obj: The remote object.
   :rtype: int

.. class:: krpc.client.Client

   A connection to the server, with an attribute for each of the server's services. Obtain one
   by calling :func:`krpc.connect`. It can be used in a ``with`` statement, which closes it at the
   end of the block.

   .. method:: close()

      Closes the connection. Closing a connection that is already closed does nothing.

   .. attribute:: krpc

      The :mod:`KRPC` service, providing the server's basic functionality. The client uses some
      of its procedures itself, such as to add and remove streams, and your program does not need
      to call those. Each of the server's other services is an attribute too, such as
      ``space_center``.

   .. method:: add_stream(func, *args, **kwargs)

      Creates a stream for the remote procedure *func* called with arguments *args* and *kwargs*,
      and returns a :class:`krpc.stream.Stream`. Each call creates a new stream. Stream a property
      by passing ``getattr`` as *func*, then the object and the property's name.

      *func* can also be a server side function, which the stream evaluates on each update. It
      is a ``KRPC.Expression`` object, or a Python function or lambda that is compiled with
      :meth:`compile` on every call. A server side function takes no arguments.

      Raises :class:`krpc.error.StreamError` for a property setter, for arguments passed with a
      server side function, for a function with no result, and for a compiled function with
      parameters. Raises ``TypeError`` for any other value.

   .. method:: stream(func, *args, **kwargs)

      Creates a stream as :meth:`add_stream` does, for use in a ``with`` statement. The stream is
      removed at the end of the block.

   .. attribute:: stream_update_condition

      A condition variable, of type ``threading.Condition``, notified after each stream update
      message is processed.

   .. method:: wait_for_stream_update(timeout=None)

      Blocks until the next stream update message is processed, or the timeout elapses. The
      timeout is in seconds, and ``None`` waits indefinitely. Hold
      :attr:`stream_update_condition` when calling this method.

   .. method:: add_stream_update_callback(callback)

      Adds a callback, with no arguments, called after each stream update message is processed.
      It runs on the connection's callback thread, as the callbacks of a stream do.

   .. method:: remove_stream_update_callback(callback)

      Removes a stream update callback.

   .. method:: run(function, *args, **kwargs)

      Runs a function on the server, within a single physics tick, and returns the value it
      produces. The value is ``None`` when the function has no result or its result is null.
      The function is a ``KRPC.Expression`` object, or a Python function or lambda, which is
      compiled with :meth:`compile` on every call. The arguments are passed to the function's
      parameters. A function built with the expression API takes positional arguments only.

      Raises ``TypeError`` for a value that is not a function, and for an argument its parameter
      cannot take, naming the argument's position.

   .. method:: add_event(function)

      Creates an event from a server side function that evaluates to a boolean value, and
      returns a :class:`krpc.event.Event`. The function can also be a Python function or lambda
      that takes no arguments, which is compiled with :meth:`compile` on every call.

      Raises ``StreamError`` for a compiled function with parameters. A function that does not
      evaluate to a boolean value raises ``ValueError``, and a value that is not a function
      raises ``TypeError``.

   .. method:: compile(func)

      Compiles a Python function or lambda into a server side function, a ``KRPC.Expression``
      object. A ``def`` can take parameters, each with a type annotation. Can be used as a
      decorator. Raises :class:`krpc.error.FunctionCompilationError` for a construct with no
      server side equivalent. See :ref:`python-client-compiling-functions`.

   .. method:: get_call(func, *args, **kwargs)

      Converts a call to the remote procedure *func* with arguments *args* and *kwargs* into a
      ``KRPC.ProcedureCall`` message, for use in a server side function built with the
      expression API. See :ref:`server-side-functions-api`.

.. class:: krpc.stream.Stream

   A stream. See :ref:`python-client-streams`.

   .. method:: __call__()

      Returns the most recent value of the stream, waiting for the first value if none has
      arrived yet. Raises the exception that ended the stream, if it has ended.

   .. method:: wait(timeout=None)

      Blocks until the value of the stream changes, or the timeout elapses. Returns ``True`` if
      the value changed, and ``False`` on a timeout. Hold :attr:`condition` when calling this
      method.

      The timeout is in seconds, and ``None`` waits indefinitely. Raises ``ValueError`` for a
      negative timeout. Raises the exception that ended the stream, if it ends before or during
      the wait. A thread holding the condition sees an ending as an update: the wait returns
      ``True``, and later reads and waits raise the exception.

   .. attribute:: condition

      A condition variable, of type ``threading.Condition``, notified when the value of the
      stream changes.

   .. attribute:: rate

      The update rate of the stream, in Hertz. Zero, the default, means no limit.

   .. method:: add_callback(callback)

      Adds a callback, called with each new value of the stream. If the stream already has a
      value, the callback is called with it straight away. The exception that ends a stream goes
      to its end callbacks.

   .. method:: remove_callback(callback)

      Removes a callback.

   .. method:: add_end_callback(callback)

      Adds a callback, called once when the stream ends, with the exception that ended it. It is
      called straight away if the stream has already ended.

   .. method:: remove_end_callback(callback)

      Removes an end callback.

   .. method:: remove()

      Removes the stream from the server. Other streams of the same call are not affected.

.. class:: krpc.event.Event

   An event, made from a stream of ``bool`` that is true when the event fires. See
   :ref:`python-client-events`.

   .. method:: wait(timeout=None)

      Blocks until the event fires, or the timeout elapses. Returns straight away if the event
      has fired since the previous wait. Returns ``True`` if the event fired, and ``False`` on a
      timeout. Hold :attr:`condition` when calling this method.

      The timeout is in seconds, and ``None`` waits indefinitely. Raises ``ValueError`` for a
      negative timeout. Raises the exception that ended the event, if it ends before or during
      the wait.

   .. attribute:: condition

      A condition variable, of type ``threading.Condition``, notified when the event fires.

   .. method:: add_callback(callback)

      Adds a callback, with no arguments, called each time the event fires. It is called straight
      away for a firing that no wait has returned for and no callback has been called for.

   .. method:: remove_callback(callback)

      Removes a callback.

   .. method:: add_end_callback(callback)

      Adds a callback, called once when the event ends, with the exception that ended it. It is
      called straight away if the event has already ended.

   .. method:: remove_end_callback(callback)

      Removes an end callback.

   .. method:: remove()

      Removes the event from the server.

   .. attribute:: stream

      The stream the event is made from.

.. class:: krpc.error.ConnectionError

   Raised when connecting fails, and by each call once the connection has closed or failed. A
   subclass of ``RuntimeError``.

   .. attribute:: status

      The status the server refused the connection with, such as ``"DENIED"``, or ``None``.

   .. attribute:: reason

      The reason the server gave for ending the connection, such as ``"SERVER_STOPPED"``, or
      ``None``.

.. class:: krpc.error.RPCError

   Raised for an error from the server that the client has no exception class for. A subclass
   of ``RuntimeError``.

.. class:: krpc.error.StreamError

   Raised by a stream or event that you removed, and when a stream cannot be created. A
   subclass of ``RuntimeError``.

.. class:: krpc.error.EncodingError

   Raised when a value cannot be encoded or decoded. A subclass of ``RuntimeError``.

.. class:: krpc.error.FunctionCompilationError

   Raised when a Python function cannot be compiled into a server side function. The error
   message names the unsupported construct. See :ref:`python-client-compiling-functions`.

Numeric Limits
^^^^^^^^^^^^^^

The ``krpc.limits`` module names the extremes of the numeric types kRPC carries over the wire.
Python integers are unbounded, and Python floats are C doubles, so the standard library covers
``DOUBLE`` alone. A service can declare one of these limits as a parameter's default value, and
the generated stub then names the constant from this module.

The minimum of an unsigned type is ``0``, so it has no constant.

.. data:: krpc.limits.DOUBLE_MAX
          krpc.limits.DOUBLE_LOWEST

   The largest and most negative finite 64-bit float.

.. data:: krpc.limits.FLOAT_MAX
          krpc.limits.FLOAT_LOWEST

   The largest and most negative finite 32-bit float.

.. data:: krpc.limits.SINT32_MAX
          krpc.limits.SINT32_MIN

   The largest and most negative 32-bit signed integer.

.. data:: krpc.limits.SINT64_MAX
          krpc.limits.SINT64_MIN

   The largest and most negative 64-bit signed integer.

.. data:: krpc.limits.UINT32_MAX

   The largest 32-bit unsigned integer.

.. data:: krpc.limits.UINT64_MAX

   The largest 64-bit unsigned integer.
