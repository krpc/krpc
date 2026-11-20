Local Socket
============

This protocol carries the :doc:`messages <messages>` over a unix domain socket, for a client on
the same machine as the game. It bypasses the network stack, so a client that makes many calls in
quick succession completes more of them per physics update. A client that makes one call and then
waits runs at the game's update rate either way.

.. note:: If a client library is available for your language, you do not need to implement this
          protocol.

.. note:: Unix domain sockets are available on Linux, macOS, and Windows 10 1803 and Windows
          Server 2019 or later.

Connecting
----------

To use this protocol, add a server in the server window and select *Protobuf over local socket* as
its protocol. The window shows the socket path, which defaults to the path under
`Default Socket Path`_.

Open a unix domain socket to that path. Then send the
:ref:`connection handshake <communication-protocol-handshake>`. The framing and the rest of the
connection are the same as for :doc:`tcpip`.

.. _communication-protocol-localsocket-default-path:

Default Socket Path
-------------------

The server and its clients each compute the default path for themselves, by the same rule:

1. If ``XDG_RUNTIME_DIR`` is set and not empty, the path is ``krpc.sock`` inside it. On Windows
   ``LOCALAPPDATA`` is read instead, as there is no runtime directory there.

2. Otherwise the path is ``krpc-<user>.sock`` inside ``/tmp``, where ``<user>`` is the name of
   the account running the process. ``/tmp`` is fixed, as ``TMPDIR`` is set per process. Windows
   always names a directory in ``LOCALAPPDATA``, and so never reaches this step.

The user name comes from the account database, as ``USER`` is unset in a process started without a
login shell. A client whose language offers only the environment, and finds no user name there,
reports an error.

Path Length and Permissions
---------------------------

A socket path is limited to 100 bytes, as the operating system copies the address into a fixed size
structure. The limit counts bytes rather than characters, so a path using multi-byte characters
reaches it sooner.

The permissions on the socket and on the directory holding it control access to the server.
``XDG_RUNTIME_DIR`` and ``LOCALAPPDATA`` are private to the account they belong to, so a socket
placed there is reachable only by that user.

The ``/tmp`` fallback socket sits directly in ``/tmp``. Its permissions follow the process
``umask``, which can leave the socket open to the user's group. Set ``XDG_RUNTIME_DIR`` or configure
the path where that matters.

Example
-------

.. note::

   This example shows the protocol, and omits much of the behavior a client library needs, such as
   handling errors and failed connections. A client library should
   implement the :doc:`client specification </client-spec>` in its entirety.

The example uses the POSIX socket API, so it runs on Linux and macOS. On Windows, Python's socket
module lacks the address family, and the socket is opened through winsock instead.

The following script connects to the server over a local socket with the name "Example", calls
``KRPC.GetStatus`` and prints the server version. It then ends the connection:

.. literalinclude:: /scripts/communication-protocols/localsocket/RPC.py
   :language: python
