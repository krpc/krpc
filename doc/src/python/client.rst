.. default-domain:: py
.. highlight:: py

Python Client
=============

This client provides a Python API for interacting with a kRPC server. It supports Python 2.7+ and
3.x

Installing the Library
----------------------

The library can be found on `PyPI <https://pypi.python.org/pypi/krpc>`_ or
:github-download-zip:`downloaded from GitHub <krpc-python>`.

To install using pip on Linux:

.. code-block:: bash

   pip install krpc

Or on Windows:

.. code-block:: none

   C:\Python27\Scripts\pip.exe install krpc

Connecting to the Server
------------------------

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

All of the functionality provided by the ``SpaceCenter`` service is accessible via
``conn.space_center``. To explore the functionality provided by a service, you can use the
``help()`` function from an interactive terminal. For example, running ``help(conn.space_center)``
will list all of the classes, enumerations, procedures and properties provides by the
``SpaceCenter`` service. This works similarly for class types, for example:
``help(conn.space_center.Vessel)``.

.. _python-client-streams:

Streaming Data from the Server
------------------------------

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

Client API Reference
--------------------

.. function:: krpc.connect([name=None], [address='127.0.0.1'], [rpc_port=50000], [stream_port=50001])

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
