Java Client Design
==================

The internal design of the Java client. This page builds on the :doc:`shared client design
</client-design>` and the :doc:`client specification </client-spec>`. Usage is in
the :doc:`Java guide </java>`. Paths are relative to ``client/java/``.

Ownership
---------

``krpc.client.Connection`` owns the socket and is ``AutoCloseable``. Services, objects, streams and
events each hold the connection strongly, and none of them owns it:

.. code-block:: java

   SpaceCenter.Vessel vessel;
   try (Connection connection = Connection.newInstance()) {
     vessel = SpaceCenter.newInstance(connection).getActiveVessel();
   }
   vessel.getMET();  // Throws RPCException

References
^^^^^^^^^^

The connection holds its ``Session`` and ``StreamManager``, which also hold each other.

The objects the program keeps also hold the connection. A ``Service``, ``RemoteObject`` or
``Stream`` holds the ``Connection``. An ``Event`` holds its ``Stream``, and a ``Stream`` holds its
``StreamImpl``. A ``StreamImpl`` holds the ``Connection`` and the ``StreamManager``.

The threads keep the connection alive. The reader and dispatcher threads hold the ``Session``, which
holds the ``StreamManager``, which holds the ``Connection``. An unclosed connection is therefore
never collected, and the cycles matter only once the threads end.

The stream manager holds each open ``StreamImpl`` strongly. Its weak set ``registered`` holds every
registered ``StreamImpl``. The set finds a stream whose condition the closing thread holds,
including one the client removed, and pins no removed stream.

A callback pins what it captures until its stream ends.

Lifetime
^^^^^^^^

* The connection closes on ``close()``, on a connection failure, or when the JVM exits.
* The client has no ``Cleaner`` or finalizer. An unclosed connection holds two threads, three
  selectors and the socket until the JVM exits, and does not delay that exit.
* After ``close()``, a call throws ``RPCException``, and streams and events end. A second
  ``close()`` does nothing.
* A connection failure closes the channel and selectors and ends both threads, with no ``close()``.
* A stream runs until ``remove()``, a server ending or the end of the connection. Dropping every
  handle leaves the stream and its callbacks running.

Threads
-------

The threads follow the :doc:`shared design </client-design>`, and are named
``krpc-reader`` and ``krpc-dispatcher``.

* ``kRPC stream waker`` threads wake conditions during a close, as a monitor has no timed lock. A
  waker blocked on a held condition stays until it is released, and another takes over after 10 ms.
* Lock order is condition, update lock, state lock. The reading thread takes only the last two, so a
  held condition never stops a read.
* Plain reads see a value once it is stored under the update lock. A thread holding the condition
  sees a frozen value until the dispatcher announces the next one.
* A callback's exception goes to the thread's uncaught exception handler.
* An ``Error`` off the dispatcher propagates from the add, the remove or the close.
* A callback waiting for a condition the closing thread holds finishes after that thread releases
  it.

Interrupts and Transports
-------------------------

TCP/IP and local sockets both use ``SocketChannel``. ``java.net.Socket`` has no unix domain support,
so a local socket needs a channel.

The client compiles for and requires Java 17. ``UnixDomainSocketAddress`` arrived in Java 16, and 17
is the oldest release with a registered Bazel toolchain.

The channel turns non-blocking at ``Session.start``, as a blocking channel closes when its thread is
interrupted. Selector waits are capped at 1 s, as closing the channel does not wake a selector.

* An interrupted call abandons its id and throws ``RPCException``, and the connection stays open. An
  interrupted wait throws ``StreamException``. Both keep the interrupt flag.
* A write holds an interrupt back until it ends, as a partial message breaks the framing.

Generics and Generated Stubs
----------------------------

Erasure removes the element type of a ``List<Integer>`` at run time. Generated stubs therefore pass
a ``krpc.schema.KRPC.Type`` descriptor to ``Encoder.encode`` and ``Encoder.decode``, and cast the
result.

``clientgen`` writes one class per service in the fixed package ``krpc.client.services``, and the
jar includes the stubs of the shipped services.

* ``Encoder`` finds a class by the name ``krpc.client.services.<Service>$<Name>``, and calls its
  constructor, ``fromValue``, ``fieldTypes`` or ``fromFieldValues`` by reflection. A wire type names
  a structure and omits its fields, so the generated class supplies them.
* ``addStream`` and ``getCall`` find a method by name and the run-time classes of its arguments.
  ``@RPCInfo`` names the ``_Types`` class holding its descriptors. ``T`` is unchecked, so a wrong
  ``T`` throws ``ClassCastException`` at the assignment.
* ``_Types`` fills maps in initializers of 50 procedures each, as a single ``switch`` for a large
  service passes the JVM's 64 KB method limit.
* A service's exception types register on the connection in its ``newInstance``. The ``Connection``
  constructor does this for ``KRPC`` only.
* A stream registers before its first update. The return type in ``_Types`` shows that a call adds
  one, or one ``GetServices`` call if the stubs are not loaded.

Mapping the Spec to Java
------------------------

.. list-table::
   :header-rows: 1

   * - Spec
     - Java
   * - Connection error, while connecting
     - ``ConnectionException``, a checked ``IOException``
   * - Connection error, from a call
     - ``RPCException``, with the ``ConnectionException`` as its cause
   * - Connection error, from a stream or event
     - ``StreamException``, with the ``ConnectionException`` as its cause
   * - Stream error, encoding error
     - ``StreamException``, checked. ``EncodingException``, unchecked
   * - Server error with a native type
     - The unchecked JDK type, from ``lang/java.py`` in ``krpctools``
   * - Server error of a service, or with no mapped type
     - ``RPCException`` or a generated subclass, checked
   * - Nullable value
     - The boxed or reference type, with ``null``
   * - Unsigned integer
     - ``int`` or ``long``, bit for bit
   * - Tuple
     - ``org.javatuples``, ``Unit`` to ``Decade``
   * - Condition
     - A plain monitor from ``getCondition()``, held with ``synchronized``
   * - Infinite timeout
     - ``Duration.ZERO`` to connect, ``Double.POSITIVE_INFINITY`` to wait
   * - Static method
     - ``Class.method(connection, ...)``. ``addStream`` and ``getCall`` take the connection first, and drop it unchecked
   * - Scoped stream or event
     - Try-with-resources. ``close()`` calls ``remove()``, and declares ``RPCException``. The language keeps the block's exception, with the exception from ``close()`` suppressed

``invoke`` declares only ``RPCException``, which is why a connection error is its cause. A server
timeout throws its generated class, as ``TimeoutException`` is checked. ``Consumer`` and
``Runnable`` declare no checked exception, so a callback catches ``RPCException`` itself.
