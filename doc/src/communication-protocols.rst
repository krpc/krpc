Communication Protocols
=======================

kRPC provides two communication protocols:

 * :doc:`communication-protocols/tcpip` for languages that can communicate over a
   TCP/IP socket.
 * :doc:`communication-protocols/websockets` for web browsers.
 * :doc:`communication-protocols/serialio` for Arduino and similar.

In each protocol, clients invoke remote procedure calls by :doc:`sending and receiving protobuf
messages <communication-protocols/messages>`.

.. toctree::
   :hidden:

   communication-protocols/tcpip
   communication-protocols/websockets
   communication-protocols/serialio
   communication-protocols/messages
