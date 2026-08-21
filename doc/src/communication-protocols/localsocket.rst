Protocol Buffers over a Local Socket
====================================

This communication protocol allows a client running on the same machine as the game to interact
with a kRPC server over a unix domain socket, rather than a TCP/IP connection.

Compared to :doc:`tcpip`, a local socket carries the same messages over a cheaper path: it does not
go through the network stack, so a client that makes many calls in quick succession gets more of
them done per physics update. A client that makes one call and then waits sees no difference, as
the wait is governed by the game's update rate rather than by the connection.

.. note:: If a client library is available for your language, you do not need to implement this
          protocol.

.. note:: Unix domain sockets are available on Linux, macOS, and Windows 10 1803 and Windows
          Server 2019 or later.

Sending and Receiving Messages
------------------------------

Identical to :doc:`tcpip`: messages are encoded according to the protobuf binary format, and are
prefixed with their size in bytes encoded as a Protocol Buffers varint.

Connecting to the RPC Server
----------------------------

The handshake is identical to :doc:`tcpip`, differing only in how the connection is opened:

1. Open a unix domain socket to the path the RPC server is listening on. The server window shows
   the path, which by default is ``krpc/rpc`` inside the directory named by the
   ``XDG_RUNTIME_DIR`` environment variable, or by ``LOCALAPPDATA`` on Windows, falling back to
   ``krpc-<user>/rpc`` inside the system temporary directory.

2. Send a ``ConnectionRequest`` message with its ``type`` field set to ``ConnectionRequest.RPC``,
   exactly as for TCP/IP.

3. Receive a ``ConnectionResponse`` message, check that ``status`` is ``ConnectionResponse.OK``,
   and keep the 16-byte ``client_identifier`` for the stream connection.

Connecting to the Stream Server
-------------------------------

As for :doc:`tcpip`, open a second connection, this time to the stream server's socket path
(``krpc/stream`` by default), and send a ``ConnectionRequest`` with its ``type`` field set to
``ConnectionRequest.STREAM`` and its ``client_identifier`` field set to the value received above.

Connecting to the stream server is optional. If the client doesn't require stream functionality,
there is no need to connect.

Socket Paths
------------

A socket path is limited to 100 bytes, as the address is copied into a fixed size structure by the
operating system. The limit is on the bytes a path takes rather than the characters it is written
with, so a path using characters that take more than one byte reaches it sooner. The server
reports a path that is too long rather than failing to start with an unhelpful error.

The socket file is created when the server starts and removed when it stops. A server whose process
is killed rather than stopped leaves the file behind; the next server to use that path removes it
before binding, so a stale file does not prevent a restart. The file alone does not say whether the
server that made it is still there, so that server connects to the path first and refuses to start
if anything answers, rather than taking a path another server is listening on and leaving it
unreachable. A path holding content is not a socket at all and is left alone.

Access to the server is controlled by the permissions on the socket and on the directory holding it.
``XDG_RUNTIME_DIR`` and ``LOCALAPPDATA`` are private to the account they belong to, so sockets
placed there are reachable only by that user. The ``/tmp`` fallback is named after the user rather
than restricted to them: it is created with whatever the process ``umask`` allows, which can leave
the socket open to the user's group. Where that matters, set ``XDG_RUNTIME_DIR`` or configure the
paths.

Invoking Remote Procedures
--------------------------

See :doc:`messages`.

Examples
--------

The following Python code connects to the RPC server over a local socket using the name "Jeb", then
invokes the ``KRPC.GetStatus`` RPC and prints the server version number from the response. The
message encoding is identical to the TCP/IP protocol; only the socket differs.

It is written for the socket API as POSIX has it, so it runs on Linux and macOS. The protocol is
the same on Windows, where the address family has to be reached through winsock, as Python's
socket module does not expose it.

.. literalinclude:: /scripts/communication-protocol-localsocket.py
   :language: python
