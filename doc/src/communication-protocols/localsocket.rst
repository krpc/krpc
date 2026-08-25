Protocol Buffers over a Local Socket
====================================

This communication protocol allows a client running on the same machine as the game to interact
with a kRPC server over a unix domain socket, rather than a TCP/IP connection.

A local socket carries the same messages as :doc:`tcpip` over a cheaper path, bypassing the
network stack. A client that makes many calls in quick succession completes more of them per
physics update. A client that makes one call and then waits runs at the game's update rate
either way.

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
   ``XDG_RUNTIME_DIR`` environment variable, or by ``LOCALAPPDATA`` on Windows. See
   `Default Socket Paths`_ for the whole rule.

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

Default Socket Paths
--------------------

The server and its clients each compute the default paths for themselves. They meet only by
following the same rule. For a socket named ``rpc`` or ``stream``:

1. If ``XDG_RUNTIME_DIR`` is set and not empty, the path is ``krpc/<name>`` inside it. On Windows
   ``LOCALAPPDATA`` is read instead, as there is no runtime directory there.

2. Otherwise the path is ``krpc-<user>/<name>`` inside ``/tmp``, where ``<user>`` is the name of
   the account running the process. ``/tmp`` is fixed, as ``TMPDIR`` is set per process and would
   move the directory for the server or the client alone. Windows always names a directory in
   ``LOCALAPPDATA``, and so never reaches this step.

The user name comes from the account database, as ``USER`` is unset in a process started without a
login shell. A client whose language offers only the environment, and finds nothing there, reports
an error. A path built without the user name would be shared between accounts.

Socket Paths
------------

A socket path is limited to 100 bytes, as the operating system copies the address into a fixed size
structure. The limit counts the bytes a path takes rather than its characters, so a path using
multi-byte characters reaches it sooner. The server reports a path that is too long.

The socket file is created when the server starts and removed when it stops. A killed server leaves
the file behind, and the next server to use that path removes it before binding.

A stale file is indistinguishable from a live one, so the server connects to the path first and
refuses to start if the connection succeeds. A path holding a file with content is left alone.

The permissions on the socket and on the directory holding it control access to the server.
``XDG_RUNTIME_DIR`` and ``LOCALAPPDATA`` are private to the account they belong to, so sockets
placed there are reachable only by that user.

The ``/tmp`` fallback is created with whatever the process ``umask`` allows, which can leave the
socket open to the user's group. Set ``XDG_RUNTIME_DIR`` or configure the paths where that
matters.

Invoking Remote Procedures
--------------------------

See :doc:`messages`.

Examples
--------

The following Python code connects to the RPC server over a local socket using the name "Jeb", then
invokes the ``KRPC.GetStatus`` RPC and prints the server version number from the response. The
message encoding is identical to the TCP/IP protocol; only the socket differs.

It is written for the POSIX socket API, so it runs on Linux and macOS. The protocol is the same on
Windows, where the address family is reached through winsock, as Python's socket module does not
expose it.

.. literalinclude:: /scripts/communication-protocol-localsocket.py
   :language: python
