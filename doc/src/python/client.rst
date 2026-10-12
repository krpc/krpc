.. default-domain:: py
.. highlight:: py

Python Client
=============

This client provides a Python API for interacting with a kRPC server.

Installing the Library
----------------------

The library can be found on `PyPI <https://pypi.python.org/pypi/krpc>`_ or
`downloaded from GitHub <https://github.com/krpc/krpc/releases>`_.

To install using pip:

.. code-block:: bash

   pip install krpc

Getting Started
---------------

The :func:`krpc.connect` function is used to open a connection to a server. It returns a connection
object (of type :class:`krpc.client.Client`) through which you can interact with the server. The
following example connects to a server running on the local machine, queries its version and prints
it out:

.. literalinclude:: /scripts/client/python/Connecting1.py

This function also accepts arguments that specify what address and port numbers to connect to, and
an optional descriptive name for the connection which is displayed in the kRPC window in the
game. For example:

.. literalinclude:: /scripts/client/python/Connecting2.py

Calling Remote Procedures
-------------------------

The kRPC server provides *procedures* that a client can run. These procedures are arranged in groups
called *services* to keep things organized. When connecting, the Python client interrogates the
server to discover what procedures it provides, and dynamically creates class types, methods,
properties etc. to call them.

The following example demonstrates how to invoke remote procedures using the Python client. It calls
:attr:`SpaceCenter.active_vessel` to get an object representing the active vessel (of type
:class:`SpaceCenter.Vessel`). It sets the name of the vessel and then prints out its altitude:

.. literalinclude:: /scripts/client/python/RemoteProcedures.py

Many procedures return an object identifying something in the game, such as a vessel or
a part. See :doc:`Object Lifetime </tutorials/object-lifetime>` for how such an object
behaves when a game is loaded or what it identifies is destroyed.

All of the functionality provided by the ``SpaceCenter`` service is accessible via
``conn.space_center``. To explore the functionality provided by a service, you can use the
``help()`` function from an interactive terminal. For example, running ``help(conn.space_center)``
will list all of the classes, enumerations, procedures and properties provides by the
``SpaceCenter`` service. This works similarly for class types, for example:
``help(conn.space_center.Vessel)``.

Deprecated Members
------------------

Some members of the server's API are deprecated, and may be removed in a future release. Calling
one emits a ``DeprecationWarning`` naming the replacement to use instead. The deprecation is also
noted in the member's docstring. Python hides ``DeprecationWarning`` by default; run Python with
``-W default`` or use the ``warnings`` module to see them.

Server Side Functions
---------------------

A server side function is code that runs inside the game, on the server. Pass a Python function or
lambda to :meth:`krpc.client.Client.run` to compile it into a server side function, run it once and
return its value. Every value it reads comes from the same physics tick. For example, the following
reads the vessel's altitude and speed together:

.. literalinclude:: /scripts/client/python/RunFunction.py

Use ``run`` for a function with effects, such as setting a property. An event or a function stream
evaluates its function on every update, repeating the effects.

:ref:`python-client-compiling-functions` describes what a function can contain. The :ref:`server
side functions tutorial <tutorial-server-side-functions>` shows how to build larger functions.

.. _python-client-streams:

Streaming Data
--------------

A common use case for kRPC is to continuously extract data from the game. The naive approach to do
this would be to repeatedly call a remote procedure, such as in the following which repeatedly
prints the position of the active vessel:

.. literalinclude:: /scripts/client/python/Streaming1.py

This approach requires significant communication overhead as request/response messages are
repeatedly sent between the client and server. kRPC provides a more efficient mechanism to achieve
this, called *streams*.

A stream repeatedly executes a procedure on the server (with a fixed set of argument values) and
sends the result to the client. It only requires a single message to be sent to the server to
establish the stream, which will then continuously send data to the client until the stream is
closed.

The following example does the same thing as above using streams:

.. literalinclude:: /scripts/client/python/Streaming2.py

It calls :meth:`krpc.client.Client.add_stream` once at the start of the program to create the
stream, and then repeatedly prints the position returned by the stream. The stream is automatically
closed when the client disconnects.

Streams can also be created using the ``with`` statement, which ensures that the stream is closed
after leaving the block:

.. literalinclude:: /scripts/client/python/Streaming3.py

A stream can be created for any procedure that returns a value. This includes both method calls and
attribute accesses. The examples above demonstrated how to stream method calls. Attributes can be
streamed as follows:

.. literalinclude:: /scripts/client/python/Streaming4.py

A stream can be created for any function call (except property setters). The most recent value of a
stream can be obtained by calling :func:`krpc.stream.Stream.__call__`. A stream can be stopped and
removed from the server by calling :func:`krpc.stream.Stream.remove` on the stream object. All of a
clients streams are automatically stopped when it disconnects.

.. note::

   A stream runs on the server until you remove it or the connection closes, even once the stream
   object is garbage collected. Each call to ``add_stream`` adds another stream.

Synchronizing with Stream Updates
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

A common use case for kRPC is to wait until the value returned by a method or attribute changes, and
then take some action. kRPC provides two mechanisms to do this efficiently: *condition variables*
and *callbacks*.

Condition Variables
~~~~~~~~~~~~~~~~~~~

Each stream has a condition variable associated with it, that is notified whenever the value of the
stream changes. The condition variables are instances of ``threading.Condition`` from the Python
standard library. These can be used to block the current thread of execution until the value of the
stream changes.

The following example waits until the abort button is pressed in game, by waiting for the value of
``vessel.control.abort`` to change to true:

.. literalinclude:: /scripts/client/python/ConditionVariables1.py

This code creates a stream, acquires a lock on the streams condition variable (using a ``with``
statement) and then repeatedly checks the value of ``abort``. It leaves the loop when it changes to
true.

The body of the loop calls ``wait`` on the stream, which causes the program to block until the value
changes. This prevents the loop from 'spinning' and so it does not consume processing resources
whilst waiting.

The example code above uses a ``with`` statement to acquire the lock on the condition variable. This
can also be done explicitly using ``acquire`` and ``release``:

.. literalinclude:: /scripts/client/python/ConditionVariables2.py

Callbacks
~~~~~~~~~

Streams allow you to register callback functions that are called whenever the value of the stream
changes. Callback functions should take a single argument, which is the new value of the stream, and
should return nothing.

For example the following program registers two callbacks that are invoked when the value of
``vessel.conrol.abort`` changes:

.. literalinclude:: /scripts/client/python/Callbacks.py

A callback added to a stream that already has a value is called with that value straight away.

.. note::

   The callback function may be called from a different thread than the one that created the
   stream. Any changes to shared state must therefore be protected with appropriate
   synchronization.

Stream Endings
~~~~~~~~~~~~~~

The following example streams the temperature of a part until the part is destroyed:

.. literalinclude:: /scripts/client/python/StreamEnd.py

Once a stream ends, calling the stream or waiting on it raises the exception that ended it. A stream
ends when its procedure raises an exception, when the server removes it, when you remove it, or when
the connection closes.

A stream that the server removes, such as an event that has finished, ends with
``StreamRemovedException`` from the ``KRPC`` service. A stream you remove ends with ``StreamError``,
and a closed connection with ``ConnectionError``.

An exception for an object that is not loaded, or a procedure not available in the game scene,
holds the stream instead. It resumes once the object or scene is available.

.. seealso:: :doc:`/tutorials/object-lifetime`

To handle an ending in a callback, add an end callback with
:meth:`krpc.stream.Stream.add_end_callback`. It is called once, with the exception.

Streaming Server Side Functions
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

Pass a Python function or lambda to :meth:`krpc.client.Client.add_stream` to stream its value,
computed on the server. The server evaluates the function on each stream update, within one physics
tick. The value can be any type the server can send, including collections and objects. For example,
the following streams the vessel's altitude in kilometers:

.. literalinclude:: /scripts/client/python/CompiledFunctionStream.py

Each call to ``add_stream`` creates a separate stream, which evaluates the function again on each
update.

A function stream or event ends on any exception its function raises. This includes
``ObjectNotLoadedException`` from the ``KRPC`` service, raised while the game loads or when a vessel
unloads. To keep a function running through a load, catch the exception in the function:

.. code-block:: python

   def altitude_km():
       try:
           return flight.mean_altitude / 1000
       except conn.krpc.ObjectNotLoadedException:
           return 0.0

   stream = conn.add_stream(altitude_km)

For an event, the handler returns ``False`` so the event does not fire.

.. _python-client-events:

Events
------

Some procedures return event objects of type :class:`krpc.event.Event`. These allow you to wait
until an event occurs, by calling :class:`krpc.event.Event.wait`. Under the hood, these are
implemented using streams and condition variables.

A call to ``wait`` returns straight away if the event has fired since the previous call on the same
event object, so no firing is missed between two calls. Several firings between two calls make
``wait`` return once.

You can also create a custom event from a Python function or lambda. The following creates an event
that triggers when ``mean_altitude > 1000``:

.. literalinclude:: /scripts/client/python/CompiledEvent.py

The same event can be built by hand from ``KRPC.Expression`` nodes, with the expression API:

.. literalinclude:: /scripts/client/python/Event.py

.. _python-client-compiling-functions:

Compiling Server Side Functions
-------------------------------

Pass a Python function or lambda to :meth:`krpc.client.Client.compile` to compile it into a server
side function. :meth:`krpc.client.Client.add_event`, :meth:`krpc.client.Client.add_stream` and
:meth:`krpc.client.Client.run` take the result, and run it without compiling it again. The following
compiles a function once, and runs it every second:

.. literalinclude:: /scripts/client/python/CompiledFunction.py

Given a Python function or lambda, ``add_event``, ``add_stream`` and ``run`` compile it again on
every call, so its captured variables take their current values. A compiled function keeps the
values from when it was compiled. Compiling makes a remote procedure call per node.

To use a function repeatedly, compile it once with ``compile`` and pass the result. ``compile``
also compiles the function on the server, so its first run is as fast as later ones.

``compile`` also works as a decorator, which replaces the function with its compiled form:

.. code-block:: python

   @conn.compile
   def altitude_km():
       return flight.mean_altitude / 1000

   stream = conn.add_stream(altitude_km)

A ``def`` can take parameters, each with a type annotation. ``run`` passes its arguments to them,
with defaults and keyword arguments as in Python:

.. code-block:: python

   @conn.compile
   def altitude_above(height: float, scale: float = 1000) -> float:
       return (flight.mean_altitude - height) / scale

   print(conn.run(altitude_above, 70000))
   print(conn.run(altitude_above, 70000, scale=1))

Only a ``def`` takes parameters. An event or a function stream runs its function with no arguments.
Passing ``add_event`` or ``add_stream`` a ``def`` with parameters raises
:class:`krpc.error.FunctionCompilationError`, and a compiled function with parameters raises
``StreamError``.

The compiler translates the function's source code into a server side function that computes
the same result on the server:

* Attribute accesses and method calls on remote objects and services become calls in the
  function, made again on each evaluation. This includes calls on the elements of a collection
  in a comprehension. The procedures of the ``KRPC`` service cannot be called.
* Captured variables, literals and any sub-expression that makes no remote call are evaluated
  once, when the function is compiled, and embedded as constants. A client side function called
  there must not make a remote procedure call, as it would make it only once.
* Operators, conditional expressions (``a if condition else b``), indexing, slices without a
  step, f-strings without format specifiers, ``in``, ``is None`` and ``is not None`` compile to
  the matching expression operators. So do tuple, list, set and dictionary displays.
* ``/`` is true division, ``//`` is floor division, and the result of ``%`` takes the sign of
  the divisor, as in Python.
* ``and`` and ``or`` short-circuit on the server, and produce the operand that decides the
  result. ``&`` and ``|`` evaluate both operands.
* ``+`` adds numbers, and concatenates two strings or two lists. The other arithmetic operators
  and the ordering comparisons take numbers only.
* ``==``, ``!=`` and ``in`` take values of the same kind. Numbers of different types widen to a
  common type.
* ``None`` takes its type from where it is used: a parameter, the other branch of a
  conditional, the other elements of a list, the function's other returns, or the variable's
  other assignments. Elsewhere, give it a type with an annotation or ``typing.cast``, for
  example ``cast(Optional[str], None)``.
* Comprehensions compile to the server's collection operations, including dictionary
  comprehensions and several ``for`` clauses with conditions.
* The builtin functions ``len``, ``sum``, ``any``, ``all``, ``reversed``, ``abs``, ``round``,
  ``int``, ``float``, ``str``, ``range``, ``enumerate`` and ``zip`` of two collections compile
  to server side operations. ``sorted``, ``min`` and ``max`` take an optional ``key`` lambda.
  ``min`` and ``max`` take a collection or several values, which are numbers unless a ``key`` is
  given. ``sorted`` also orders strings and tuples.
* The string methods ``upper``, ``lower``, ``strip``, ``lstrip``, ``rstrip``, ``replace``,
  ``split``, ``find``, ``startswith``, ``endswith`` and ``join`` compile to server side
  operations. ``strip``, ``lstrip`` and ``rstrip`` take no argument, and ``split`` takes a
  separator.
* A ``math`` module function with server side arguments compiles to a ``StdLib`` procedure.
* ``print`` writes a line to the game's log with ``StdLib.print``. An argument computed on the
  server is a string, a number or a bool. A constant argument is formatted on the client, as
  ``str()`` formats it. ``sep`` is a constant.
* Iterating over a dictionary iterates over its keys, and ``in`` tests its keys. A string cannot
  be iterated over. ``d.items()``
  gives its key and value pairs, ``d.keys()`` its keys and ``d.values()`` its values.
* A comprehension target can unpack a tuple, as in ``[key for key, value in d.items()]``.
* A condition tests truth as Python does. ``None`` is false, a number is true when it is not
  zero, and a string, bytes or a collection when it is not empty. Any other value is true.
* Reading an attribute of a structure a service defines reads that field of it on the server.
  Calling the structure type builds one there, with its field values given by position, by
  field name, or both.

A Python function can also contain statements, which run on the server. This one shuts down
the engines that have run out of fuel, and returns how many it shut down:

.. literalinclude:: /scripts/client/python/CompiledStatements.py

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

Local variables and functions follow these rules:

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
* A name assigned anywhere in the function is local to all of it, as in Python.
* Reading a variable is an error unless every path to the read assigns it first. An assignment
  in a loop or a ``try`` body does not count after the loop or the ``try``.
* A local function reads a variable when it is called, so the variable can be assigned after the
  function's definition, if every path to each call assigns it.
* A captured collection is copied to the server, so assigning to or deleting one of its
  elements is an error.
* A captured variable declared ``nonlocal`` in the function is a state variable. It starts
  with the value it holds when the function is compiled. A compiled function keeps its value
  between runs, as does a stream or an event. The value belongs to the compiled function, so two
  streams of one function advance it twice per update. A function compiled again on each call
  starts it again. A local function cannot declare one.
* A state variable's type comes from its starting value. To give it another type, cast it in
  a statement of the function's body before any other use of it, for example
  ``values = cast(List[int], values)`` for a list that starts empty, or
  ``last = cast(Optional[float], last)`` for a value that starts as ``None``.
* A local function defined or redefined in a nested block can only be called within that
  block.
* A function that returns a value must end with a ``return`` or ``raise`` statement, an ``if``
  or ``try`` that returns or raises on every path, or a ``while True`` loop with no ``break``.

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

A construct with no server side equivalent raises :class:`krpc.error.FunctionCompilationError`
naming it. Examples are a format specifier, ``str.format``, and a call to a client side function
with server side arguments.

Reading a stream inside a function is an error, as the stream would be read once, when the
function is compiled. So is a call to a client side function written as a statement, such as
``time.sleep(1)``.

A Python function passed to ``add_event`` or ``add_stream`` without ``compile`` is an error when it
makes no remote procedure call and uses no state variable, as its value is the same on every update.

Client API Reference
--------------------

.. function:: krpc.connect([name=None], [address='127.0.0.1'], [rpc_port=50000], [stream_port=50001], [use_pregenerated_stubs=True], [timeout=None])

   This function creates a connection to a kRPC server. It returns a :class:`krpc.client.Client`
   object, through which the server can be communicated with.

   :param str name: A descriptive name for the connection. This is passed to the server and appears
                    in the in-game server window.
   :param str address: The address of the server to connect to. Can either be a hostname or an IP
                       address in dotted decimal notation. Defaults to '127.0.0.1'.
   :param int rpc_port: The port number of the RPC Server. Defaults to 50000. This should match the
                           RPC port number of the server you want to connect to.
   :param int stream_port: The port number of the Stream Server. Defaults to 50001. This should
                           match the stream port number of the server you want to connect to.
   :param bool use_pregenerated_stubs: Whether to use the pre-generated service stubs bundled with
                           the client, which include type hints. If set to ``False``, or if the
                           server provides a service with no bundled stub, the service is generated
                           dynamically at runtime. Defaults to ``True``.
   :param float timeout: How many seconds to wait for a connection before giving up. Defaults to
                           ``None``, which waits indefinitely. A network that drops a connection
                           attempt rather than refusing it otherwise leaves the client waiting.

.. function:: krpc.connect_local([name=None], [rpc_path], [stream_path], [use_pregenerated_stubs=True])

   This function creates a connection to a kRPC server running on the same machine, over unix
   domain sockets rather than TCP/IP. It returns a :class:`krpc.client.Client` object, just as
   :func:`krpc.connect` does, and the connection behaves identically thereafter.

   Use this when the script runs on the same machine as the game and the server is configured to
   use the local socket protocol. A script that makes many calls in quick succession completes
   more of them per physics update. A script that makes a call and then waits runs at the game's
   update rate either way.

   Unix domain sockets are available on Linux, macOS, and Windows 10 1803 and Windows Server
   2019 or later. Python's socket module does not expose the address family on Windows, so the
   client opens the socket through winsock itself.

   :param str name: A descriptive name for the connection. This is passed to the server and appears
                    in the in-game server window.
   :param str rpc_path: The path of the socket the RPC server is listening on. This should match
                        the RPC socket path shown in the in-game server window. Defaults to the
                        path the server uses unless it was configured with another.
   :param str stream_path: The path of the socket the Stream Server is listening on. This should
                           match the stream socket path shown in the in-game server window.
                           Defaults as ``rpc_path`` does. Pass ``None`` to connect without stream
                           support.
   :param bool use_pregenerated_stubs: As for :func:`krpc.connect`.

.. class:: krpc.client.Client

   This class provides the interface for communicating with the server. It is dynamically populated
   with all the functionality provided by the server. Instances of this class should be obtained by
   calling :func:`krpc.connect`.

   .. method:: add_stream(func, *args, **kwargs)

      Create a stream for the remote procedure *func* called with arguments *args* and *kwargs*.
      Returns a :class:`krpc.stream.Stream` object. Each call creates a new stream.

      *func* can also be a server side function, which the stream evaluates on each update. It
      is a ``KRPC.Expression`` object, or a Python function or lambda that is compiled with
      :meth:`compile` on every call. A server side function takes no arguments. A function with
      no result, or a compiled function with parameters, raises ``StreamError``. Any other value
      raises ``TypeError``.

   .. method:: stream(func, *args, **kwargs)

      Allows use of the ``with`` statement to create a stream and automatically remove it from the
      server when it goes out of scope. The remote procedure or server side function to be
      streamed should be passed as *func*, and its arguments as *args* and *kwargs*.

   .. method:: compile(func)

      Compile a Python function or lambda into a server side function, a ``KRPC.Expression``
      object. A ``def`` can take parameters, each with a type annotation. Can be used as a
      decorator. Raises :class:`krpc.error.FunctionCompilationError` for a construct with no
      server side equivalent. See :ref:`python-client-compiling-functions`.

   .. method:: add_event(function)

      Create an event from a server side function that evaluates to a boolean value. The
      function can also be a Python function or lambda that takes no arguments, which is
      compiled with :meth:`compile` on every call. Returns a
      :class:`krpc.event.Event` object.

      Raises ``StreamError`` without a stream connection, and for a compiled function with
      parameters. A function that does not evaluate to a boolean value raises ``ValueError``,
      and a value that is not a function raises ``TypeError``.

   .. method:: run(function, *args, **kwargs)

      Run a function on the server, within a single physics tick, and return the value it
      produces. The value is ``None`` when the function has no result or its result is null.
      The function is a ``KRPC.Expression`` object, or a Python function or lambda, which is
      compiled with :meth:`compile` on every call. The arguments are passed to the function's
      parameters. A function built with the expression API takes positional arguments only.
      This is the intended way to run a function with side effects once, where an event or a
      stream runs it on every update.

      Raises ``TypeError`` for a value that is not a function, and for an argument its parameter
      cannot take, naming the argument's position.

   .. attribute:: stream_update_condition

      A condition variable (of type ``threading.Condition``) that is notified whenever a stream
      update finishes processing.

   .. method:: wait_for_stream_update(timeout=None)

      This method blocks until a stream update finishes processing or the operation times out.

      The stream update condition variable must be locked before calling this method.

      If *timeout* is specified and is not ``None``, it should be a floating point number specifying
      the timeout in seconds for the operation.

   .. method:: add_stream_update_callback(callback)

      Adds a callback function that is invoked whenever a stream update finishes processing.

      .. note::

         The callback function may be called from a different thread than the one that created the
         stream. Any changes to shared state must therefore be protected with appropriate
         synchronization.

   .. method:: remove_stream_update_callback(callback)

      Removes a stream update callback function.

   .. method:: get_call(func, *args, **kwargs)

      Converts a call to function *func* with arguments *args* and *kwargs* into a message
      object. This allows descriptions of procedure calls to be passed to the server, for example
      when constructing custom events. See :ref:`python-client-events`.

   .. method:: close()

      Closes the connection to the server.

   .. attribute:: krpc

      The basic KRPC service, providing interaction with basic functionality of the server.

      :rtype: :class:`krpc.client.KRPC`

.. class:: krpc.client.KRPC

      This class provides access to the basic server functionality provided by the :class:`KRPC`
      service. An instance can be obtained by calling :attr:`krpc.client.Client.krpc`.

      See :class:`KRPC` for full documentation of this class.

      Some of this functionality is used internally by the python client (for example to create and
      remove streams) and therefore does not need to be used directly from application code.

.. class:: krpc.error.FunctionCompilationError

   Raised when a Python function cannot be compiled into a server side function. The error
   message names the unsupported construct. See :ref:`python-client-compiling-functions`.

.. class:: krpc.stream.Stream

   This class represents a stream. See :ref:`python-client-streams`.

   .. attribute:: rate

      The update rate of the stream in Hertz. When set to zero, the rate is unlimited.

   .. method:: __call__()

      Returns the most recent value for the stream, waiting for the first value if none has arrived
      yet. Raises the exception that ended the stream, if it has ended, such as an exception thrown
      by the remote procedure.

   .. attribute:: condition

      A condition variable (of type ``threading.Condition``) that is notified whenever the value of
      the stream changes.

   .. method:: wait(timeout=None)

      This method blocks until the value of the stream changes or the operation times out. Returns
      ``True`` if the value changed, and ``False`` on a timeout.

      The streams condition variable must be locked before calling this method.

      If *timeout* is specified and is not ``None``, it should be a floating point number specifying
      the timeout in seconds for the operation.

      Raises the exception that ended the stream, if it ends before or during the wait.

   .. method:: add_callback(callback)

      Adds a callback function that is invoked whenever the value of the stream changes. The
      callback function should take one argument, which is passed the new value of the stream. If
      the stream already has a value, the callback is invoked with it straight away.
      An error from the stream does not invoke the callback. Calling the stream raises it.

      .. note::

         The callback function may be called from a different thread than the one that created the
         stream. Any changes to shared state must therefore be protected with appropriate
         synchronization.

   .. method:: remove_callback(callback)

      Removes a callback function from the stream.

   .. method:: add_end_callback(callback)

      Adds a callback function that is invoked once when the stream ends. The callback function
      should take one argument, which is passed the exception that ended the stream. It is
      invoked straight away if the stream has already ended.

   .. method:: remove_end_callback(callback)

      Removes an end callback function from the stream.

   .. method:: remove()

      Removes the stream from the server. Other streams of the same call are not affected.

.. class:: krpc.event.Event

   This class represents an event. See :ref:`python-client-events`. It is wrapper around a stream of
   type ``bool`` that indicates when the event occurs.

   .. attribute:: condition

      The condition variable (of type ``threading.Condition``) that is notified whenever the event
      occurs.

   .. method:: wait(timeout=None)

      This method blocks until the event occurs or the operation times out. It returns straight
      away if the event has occurred since the previous call on this event object. Returns
      ``True`` if the event occurred, and ``False`` on a timeout.

      The events condition variable must be locked before calling this method.

      If *timeout* is specified and is not ``None``, it should be a floating point number specifying
      the timeout in seconds for the operation.

      Raises the exception that ended the event, if it ends before or during the wait.

   .. method:: add_callback(callback)

      Adds a callback function that is invoked whenever the event occurs. The callback function
      should be a function that takes zero arguments. It is invoked straight away if the event
      has occurred since the previous call to :meth:`krpc.event.Event.wait` on this event
      object, and the event's callbacks have not been invoked for it. A callback added after the
      event has ended is not invoked for an earlier firing. An error from the event does not invoke
      the callback. Waiting on the event raises it.

   .. method:: remove_callback(callback)

      Removes a callback function from the event.

   .. method:: add_end_callback(callback)

      Adds a callback function that is invoked once when the event ends, with the exception that
      ended it. It is invoked straight away if the event has already ended.

   .. method:: remove_end_callback(callback)

      Removes an end callback function from the event.

   .. method:: remove()

      Removes the event from the server.

   .. attribute:: stream

      Returns the underlying stream for the event.

Numeric Limits
--------------

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
