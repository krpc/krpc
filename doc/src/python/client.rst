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

Many procedures return an object standing for something in the game, such as a vessel or
a part. See :doc:`Object Lifetime </tutorials/object-lifetime>` for what those objects do
when a game is loaded or the thing they stand for is destroyed.

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

Synchronizing with Stream Updates
---------------------------------

A common use case for kRPC is to wait until the value returned by a method or attribute changes, and
then take some action. kRPC provides two mechanisms to do this efficiently: *condition variables*
and *callbacks*.

Condition Variables
^^^^^^^^^^^^^^^^^^^

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

.. note::

   The stream does not start receiving updates until the first call to ``wait``. This means that the
   example code will not miss any updates to the streams value, as it will have already locked the
   condition variable before the first stream update is received.

The example code above uses a ``with`` statement to acquire the lock on the condition variable. This
can also be done explicitly using ``acquire`` and ``release``:

.. literalinclude:: /scripts/client/python/ConditionVariables2.py

Callbacks
^^^^^^^^^

Streams allow you to register callback functions that are called whenever the value of the stream
changes. Callback functions should take a single argument, which is the new value of the stream, and
should return nothing.

For example the following program registers two callbacks that are invoked when the value of
``vessel.conrol.abort`` changes:

.. literalinclude:: /scripts/client/python/Callbacks.py

.. note::

   When a stream is created it does not start receiving updates until ``start`` is called. This is
   implicitly called when accessing the value of a stream, but as this example does not do this an
   explicit call to ``start`` is required.

.. note::

   The callbacks are registered before the call to ``start`` so that stream updates are not missed.

.. note::

   The callback function may be called from a different thread to that which created the stream. Any
   changes to shared state must therefore be protected with appropriate synchronization.

.. _python-client-events:

Custom Events
-------------

Some procedures return event objects of type :class:`krpc.event.Event`. These allow you to wait
until an event occurs, by calling :class:`krpc.event.Event.wait`. Under the hood, these are
implemented using streams and condition variables.

Custom events can also be created. An expression API allows you to create code that runs on the
server and these can be used to build a custom event. For example, the following creates the
expression ``mean_altitude > 1000`` and then creates an event that will be triggered when the
expression returns true:

.. literalinclude:: /scripts/client/python/Event.py

Expression Streams
------------------

Expressions can also be used to stream the result of a computation from the server, by
calling :meth:`krpc.client.Client.add_expression_stream`. Values are computed on the server on
each stream update, so complex telemetry can be received without the round trip latency of
multiple RPCs, and without the values changing between calls. The expression can evaluate to
any type that can be sent to a client, including collections and objects. For example, the
following streams the vessel's altitude, converted to kilometers on the server:

.. literalinclude:: /scripts/client/python/ExpressionStream.py

.. _python-client-compiling-expressions:

Compiling Python Functions to Expressions
-----------------------------------------

Instead of building an expression from the factory methods directly, a python function or
lambda that takes no arguments can be compiled into a server side expression using
:meth:`krpc.client.Client.compile_expression`. :meth:`krpc.client.Client.add_event` and
:meth:`krpc.client.Client.add_expression_stream` also accept functions directly:

.. literalinclude:: /scripts/client/python/CompiledExpression.py

The compiler translates the function's source code into an expression that computes the same
result on the server:

* Remote procedure calls made by the function — attribute accesses and method calls on remote
  objects and services — become calls embedded in the expression, re-invoked on each
  evaluation. This includes calls on the elements of collections inside comprehensions.
* All other values — captured variables, literals, and any sub-expression that does not involve
  the server — are evaluated once, when the expression is compiled, and embedded as constants.
* Arithmetic and comparison operators (with python's true division semantics, and ``//`` as
  floor division), bitwise operators, boolean operators, conditional expressions
  (``a if condition else b``), assignment expressions (``:=``), tuple/list/set/dictionary
  constructors, indexing, slices (without a step), f-strings (without format specifiers) and
  ``in`` are translated to the corresponding expression operators. Note that ``and`` and
  ``or`` do not short-circuit when evaluated on the server.
* Comprehensions — including dictionary comprehensions and multiple ``for`` clauses — with
  optional conditions, and the builtin functions ``len``, ``sum``, ``min``, ``max``, ``any``,
  ``all``, ``sorted`` (with an optional ``key`` lambda), ``abs``, ``round``, ``int``,
  ``float`` and ``str``, are translated to the server's operations. Calls to ``math`` module
  functions with server side arguments are compiled to the ``StdLib`` service's procedures.
* Reading an attribute of a structure a service defines reads that field of it on the server,
  and calling the structure type — with its field values given by position, by field name, or
  both — builds one there.
* Local function definitions with annotated parameter types, and lambdas without parameters
  assigned to names, compile to server side functions, and calls to them to invocations.
* A plain function may contain statements: ``if``/``elif``/``else``, ``while`` and ``for``
  loops with ``break`` and ``continue``, early returns, local variables — including mutation,
  augmented assignment such as ``total += x``, appending to lists and sets, and assignment to
  list and dictionary elements — assignment to the properties of remote objects and services,
  and remote calls as statements for their effects. Local variables take their type from
  their first assignment; annotate assignments of empty collections, for example
  ``result: list[int] = []``. A function that returns a value must end with a return
  statement.

Anything else — ``try`` statements, string formatting, calls to client side functions with
server side arguments — cannot run on the server, and raises
:class:`krpc.error.ExpressionCompilationError` describing the unsupported construct.

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
   more of them per physics update this way; one that makes a call and then waits sees no
   difference, as the wait is governed by the game's update rate.

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

      Create a stream for the function *func* called with arguments *args* and *kwargs*. Returns a
      :class:`krpc.stream.Stream` object.

   .. method:: stream(func, *args, **kwargs)

      Allows use of the ``with`` statement to create a stream and automatically remove it from the
      server when it goes out of scope. The function to be streamed should be passed as *func*, and
      its arguments as *args* and *kwargs*.

   .. method:: add_expression_stream(expression)

      Create a stream that evaluates the given server side expression (a ``KRPC.Expression``
      object) on each update and streams the value it evaluates to. The type of the stream's
      values is reported by the server, from the expression's return type. Returns a
      :class:`krpc.stream.Stream` object.

   .. method:: expression_stream(expression)

      Allows use of the ``with`` statement to create an expression stream and automatically
      remove it from the server when it goes out of scope.

   .. method:: compile_expression(func)

      Compile a python function or lambda, taking no arguments, into a server side expression
      (a ``KRPC.Expression`` object) that computes the same result on the server. Remote
      procedure calls made by the function are re-invoked on each evaluation of the
      expression; other values are captured when the expression is compiled. Raises
      :class:`krpc.error.ExpressionCompilationError` for constructs that cannot run on the
      server.

   .. method:: add_event(expression)

      Create an event from a server side expression, that must evaluate to a boolean value.
      The expression may also be given as a python function or lambda taking no arguments,
      which is compiled using :meth:`compile_expression`. Returns a
      :class:`krpc.event.Event` object.

   .. method:: run_function(function)

      Run a function on the server, within a single physics tick, and return the value it
      produces, or ``None`` for a function with no result. The function may be given as a
      ``KRPC.Expression`` object, or as a python function or lambda taking no arguments,
      which is compiled using :meth:`compile_expression`. This is the intended way to use
      functions with side effects, which would otherwise re-run on every update of an event
      or stream.

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

         The callback function may be called from a different thread to that which created the
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

.. class:: krpc.error.ExpressionCompilationError

   Raised when a python function cannot be compiled into a server side expression. The error
   message describes the unsupported construct. See
   :ref:`python-client-compiling-expressions`.

.. class:: krpc.stream.Stream

   This class represents a stream. See :ref:`python-client-streams`.

   .. method:: start(wait=True)

      Starts the stream. When a stream is created by calling :meth:`krpc.client.Client.add_stream`
      it does not start sending updates to the client until this method is called.

      If wait is true, this method will block until at least one update has been received from the
      server.

      If wait is false, the method starts the stream and returns immediately. Subsequent calls to
      :meth:`__call__` may raise a ``StreamError`` exception if the stream does not yet contain a
      value.

   .. attribute:: rate

      The update rate of the stream in Hertz. When set to zero, the rate is unlimited.

   .. method:: __call__()

      Returns the most recent value for the stream. If executing the remote procedure for the stream
      throws an exception, calling this method will rethrow the exception. Raises a ``StreamError``
      exception if no update has been received from the server.

      If the stream has not been started this method calls ``start(True)`` to start the stream and
      wait until at least one update has been received.

   .. attribute:: condition

      A condition variable (of type ``threading.Condition``) that is notified whenever the value of
      the stream changes.

   .. method:: wait(timeout=None)

      This method blocks until the value of the stream changes or the operation times out.

      The streams condition variable must be locked before calling this method.

      If *timeout* is specified and is not ``None``, it should be a floating point number specifying
      the timeout in seconds for the operation.

      If the stream has not been started this method calls ``start(False)`` to start the stream
      (without waiting for at least one update to be received).

   .. method:: add_callback(callback)

      Adds a callback function that is invoked whenever the value of the stream changes. The
      callback function should take one argument, which is passed the new value of the stream.

      .. note::

         The callback function may be called from a different thread to that which created the
         stream. Any changes to shared state must therefore be protected with appropriate
         synchronization.

   .. method:: remove_callback(callback)

      Removes a callback function from the stream.

   .. method:: remove()

      Removes the stream from the server.

.. class:: krpc.event.Event

   This class represents an event. See :ref:`python-client-events`. It is wrapper around a stream of
   type ``bool`` that indicates when the event occurs.

   .. method:: start()

      Starts the event. When an event is created, it will not receive updates from the server until
      this method is called.

   .. attribute:: condition

      The condition variable (of type ``threading.Condition``) that is notified whenever the event
      occurs.

   .. method:: wait(timeout=None)

      This method blocks until the event occurs or the operation times out.

      The events condition variable must be locked before calling this method.

      If *timeout* is specified and is not ``None``, it should be a floating point number specifying
      the timeout in seconds for the operation.

      If the event has not been started this method calls ``start()`` to start the underlying
      stream.

   .. method:: add_callback(callback)

      Adds a callback function that is invoked whenever the event occurs. The callback function
      should be a function that takes zero arguments.

   .. method:: remove_callback(callback)

      Removes a callback function from the event.

   .. method:: remove()

      Removes the event from the server.

   .. attribute:: stream

      Returns the underlying stream for the event.

Numeric Limits
--------------

The ``krpc.limits`` module names the extremes of the numeric types that kRPC carries over the
wire. Python names none of them itself: its integers are unbounded, so there is no largest
``int``, and its floats are C doubles, so the standard library describes ``DOUBLE`` but says
nothing about the 32-bit ``FLOAT``. A service can declare one of these as a parameter's default
value, in which case the generated stub names the constant here.

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
