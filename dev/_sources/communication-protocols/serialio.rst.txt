Serial IO
=========

This protocol carries the :doc:`messages <messages>` over a serial port, for a microcontroller such
as an Arduino.

.. note::

   If a client library is available for your language, you do not need to implement this protocol.

.. note::

   Streams and events are not supported over serial IO. The server accepts the stream and event
   RPCs, but their updates have no flow control and can fill its write buffer, as described under
   :ref:`communication-protocol-serialio-buffering`.

Configuring the Server
----------------------

To use this protocol, add a server in the server window and select *Protobuf over SerialIO* as its
protocol. The window sets the serial port name and the baud rate, which defaults to 9600.

Framing
-------

The framing is the same as for :doc:`tcpip`. Each message is a ``Message``, prefixed with its size
in bytes, encoded as a varint.

Sessions
--------

Open the serial port, then send the
:ref:`connection handshake <communication-protocol-handshake>`. The server serves one session on
the port at a time.

A serial line has no connect or disconnect. A session ends with a
:ref:`disconnect <communication-protocol-disconnect>`, after which the server reads the next
message as a ``connection_request``. A ``connection_request`` sent during a session also starts a
new one.

.. _communication-protocol-serialio-resetting:

Resetting Clients
-----------------

A client that resets, such as an Arduino after a power cycle, can find the line in the state its
earlier session left it in. The server ends a session whose message stalls for 3 seconds partway,
and drops the partial message. The client specification states how a client resumes a line. See
:doc:`/client-spec/connection`.

A server with "Auto-accept new clients" turned off answers a connection request only once the player
approves it. A client of such a server connects with no handshake timeout, so it recovers from a
stale partial message only by a manual reset.

.. _communication-protocol-serialio-buffering:

Buffering and Data Rates
------------------------

A serial port carries data far more slowly than the game produces it. At the default 9600 baud it
carries roughly a thousand bytes per second. The server buffers up to a megabyte of data in each
direction:

* Data received from the port waits in its buffer until the game reads it. A full buffer pauses
  reading from the port, and no data is lost.

* Data written by the game waits in its buffer until the port has sent it. The buffer fills when a
  client sends requests faster than it reads their responses. The server then drops the session
  and stops, and is started again from the server window.

To keep the write buffer from filling, read each response before sending the next request.

Example
-------

.. note::

   This example shows the protocol, and omits much of the behavior a client library needs, such as
   handling errors and failed connections. A client library should
   implement the :doc:`client specification </client-spec>` in its entirety.

The following script connects to a server on the serial port ``/dev/ttyUSB0`` with the name
"Example", calls ``KRPC.GetStatus`` and prints the result. It then ends the session with a
``disconnect``:

.. literalinclude:: /scripts/communication-protocols/serialio/RPC.py
   :language: python
