C++ Client Design
=================

The internal design of the C++ client. This page builds on the :doc:`shared client design
</client-design>` and the :doc:`client specification </client-spec>`. Usage is in
the :doc:`C++ guide </cpp>`. Paths are relative to ``client/cpp/``.

Ownership
---------

``krpc::Client`` is a handle to one shared state. Copies, services, objects, streams and events each
hold the client, so each keeps the connection open:

.. code-block:: cpp

   SpaceCenter::Vessel active_vessel() {
     auto conn = krpc::connect();
     SpaceCenter sc(conn);
     return sc.active_vessel();
     // Holds the connection after conn and sc are destroyed
   }

References
^^^^^^^^^^

The client state holds the ``StreamManager``, which holds the ``Session``.

The program keeps the connection open through what it holds. A ``Client``, ``Service`` or ``Object``
holds the ``Client::State``. A ``Stream`` or ``Event`` holds its ``StreamImpl``, which holds the
``Client::State``.

These references are strong. With a non-owning pointer, a returned object would dangle when its
client is destroyed, moved or reallocated. A weak handle would need a non-owning ``Client`` for each
call, which can dangle or be copied out by a caller.

Reader and dispatcher work holds a ``StreamImpl``, so the reader never runs the last release. Work
left in the session is released by the close.

The handles of a stream share one pointer to its ``StreamImpl``, apart from the pointers this work
holds. Its release stops the callbacks and removes the stream from the server, whatever is queued.
Otherwise a callback slower than its updates would keep the stream, and the connection, alive
indefinitely.

The stream manager holds each ``StreamImpl`` weakly, and holds no client, so these references form
no cycle. It holds the exception throwers directly, as the reader throws stream errors. A stream the
reader registers holds no client until a handle claims it, so the manager holds it strongly until
then.

Lifetime
^^^^^^^^

* The connection closes on ``close()``, or when the last holder is released and the dispatcher
  thread has run the queue down.
* A callback in progress carries on after the last handle is released. ``close()`` waits for it.
* After ``close()``, the state stays valid. Calls through any holder throw ``ConnectionError``, and
  streams and events end. A second ``close()`` does nothing.

Client Handles
^^^^^^^^^^^^^^

* Moves are the defaults. A moved-from client is empty, and its holders are unaffected.
* An empty client is not connected, and its calls throw ``ConnectionError``. A default-constructed
  object holds an empty client.
* Service and object constructors take ``const Client&``, as they copy the client. Stream and event
  constructors and static methods take ``Client&``, as they call through it. Decoders take
  ``Client*``, where null means no client.

Callback Cycles
^^^^^^^^^^^^^^^

User callbacks can add a cycle, as C++ has no garbage collector:

.. list-table::
   :header-rows: 1

   * - A callback holding
     - Effect
   * - An object, in a stream or event callback
     - None. It is released once the stream's last handle is gone
   * - Its own stream or event
     - Keeps the connection open until the stream ends
   * - An object, in an update callback
     - Keeps the connection open until the connection ends

A stream that ends destroys its value callbacks, and the end of the connection destroys the update
callbacks, which breaks these cycles. A program that keeps such a callback calls ``close()``. A
callback added after the end is destroyed by the add.

Threads
-------

The threads and the close follow the :doc:`shared design </client-design>`. The
last release can run on any thread, including the dispatcher thread from a callback:

* The close skips joining the thread it runs on. ``~Session`` detaches that thread.
* A thread holding a condition skips joining the dispatcher thread, which can be waiting for that
  condition. The dispatcher thread holds the manager, and releases it as it ends.
* ``StreamManager::update`` moves its records to the dispatcher, so the reader holds no stream once
  it lets go of the update lock.
* Callbacks are destroyed with no lock held, as one can hold the last copy of the client.

During a close, another thread can hold a stream's condition. The condition then wakes the threads
waiting on it, once its mutex is free.

An exception from a callback goes to standard error.

Mapping the Spec to C++
-----------------------

.. list-table::
   :header-rows: 1

   * - Spec
     - C++
   * - Closing a connection
     - ``Client::close()``, or the last release
   * - Connection error
     - ``krpc::ConnectionError``
   * - Stream error
     - ``krpc::StreamError``
   * - Server error with no mapped type
     - ``krpc::RPCError``
   * - Nullable value
     - ``std::optional``, checked by the compiler where a parameter is not nullable
   * - Null object
     - An empty ``std::optional``. A default-constructed object has id 0, which the server rejects
       as an argument
   * - Interrupting a blocked thread
     - Unsupported, as a C++ thread cannot be interrupted

A stream value is decoded when it is read, and for each callback.
