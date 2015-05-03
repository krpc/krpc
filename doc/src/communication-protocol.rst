.. _communication-protocol:

Communication Protocol
======================

Clients invoke Remote Procedure Calls (RPCs) by communicating with the server
using `Protocol Buffer messages
<https://developers.google.com/protocol-buffers/docs/proto>`_ sent over a TCP/IP
connection.

The kRPC download comes with a protocol buffer message definitions file
`KRPC.proto
<https://github.com/djungelorm/krpc/blob/latest-version/schema/kRPC/Schema/KRPC.proto>`_
that describes the structure of these messages. It also includes versions of
this file compiled for Python, Java and C++ using Google's protocol buffers
compiler.

Establishing a connection
-------------------------

kRPC consists of two servers: an *RPC server* (over which the client sends and
receives RPCs) and a *Stream server* (over which the client receives
:ref:`communication-protocol-streams`). A client first connects to the RPC
server, then the Stream server.

.. _communication-protocol-rpc-connect:

Connecting to the RPC Server
^^^^^^^^^^^^^^^^^^^^^^^^^^^^

To establish a connection to the RPC server, a client must do the following:

 1. Open a TCP socket to the server on its RPC port (which defaults to 50000).

 2. Send this 12 byte hello message:
    ``0x48 0x45 0x4C 0x4C 0x4F 0x2D 0x52 0x50 0x43 0x00 0x00 0x00``

 3. Send a 32 byte message containing a string to identify the new connection.
    This should be a UTF-8 encoded string, up to a maximum of 32 bytes in
    length. If the string is shorter than 32 bytes, it should be padded with
    zeros.

 4. Receive a 16 byte unique client identifier. This is sent to the client when
    the connection is granted, for example after the user has clicked accept on
    the in-game UI.

For example, this python code will connect to the RPC server at address
``127.0.0.1:50000`` using the identifier ``Jeb``:

.. code-block:: python

   import socket
   rpc_conn = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
   rpc_conn.connect(('127.0.0.1', 50000))
   # Send the 12 byte hello message
   rpc_conn.sendall(b'\x48\x45\x4C\x4C\x4F\x2D\x52\x50\x43\x00\x00\x00')
   # Send the 32 byte client name 'Jeb' padded with zeros
   name = 'Jeb'.encode('utf-8')
   name += (b'\x00' * (32-len(name)))
   rpc_conn.sendall(name)
   # Receive the 16 byte client identifier
   identifier = b''
   while len(identifier) < 16:
       identifier += rpc_conn.recv(16 - len(identifier))
   # Connection successful. Print out a message along with the client identifier.
   identifier = ''.join('%02x' % ord(c) for c in identifier)
   print 'Connected to RPC server, client idenfitier = %s' % identifier

Connecting to the Stream Server
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

To establish a connection to the Stream server, a client must first
:ref:`connect to the RPC server <communication-protocol-rpc-connect>` (as
above), then do the following.

1. Open a TCP socket to the server on its Stream port (which defaults to 50001).

2. Send this 12 byte hello message:
   ``0x48 0x45 0x4C 0x4C 0x4F 0x2D 0x53 0x54 0x52 0x45 0x41 0x4D``

3. Send a 16 byte message containing the client's unique identifier. This
   identifier is given to the client after it successfully connects to the RPC
   server.

4. Receive a 2 byte OK message: ``0x4F 0x4B``. This indicates a successful
   connection.

.. note:: Connecting to the Stream Server is optional. If the client doesn't
          require stream functionality, there is no need to connect.

For example, this python code will connect to the Stream server at address
``127.0.0.1:50001``. (Note that ``identifier`` is the client identifier received
by :ref:`connecting to the RPC server <communication-protocol-rpc-connect>` (as
above).

.. code-block:: python

   import socket
   stream_conn = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
   stream_conn.connect(('127.0.0.1', 50001))
   # Send the 12 byte hello message
   stream_conn.sendall(b'\x48\x45\x4C\x4C\x4F\x2D\x53\x54\x52\x45\x41\x4D')
   # Send the 16 byte client identifier
   stream_conn.sendall(identifier)
   # Receive the 2 byte OK message
   ok_message = b''
   while len(ok_message) < 2:
       ok_message += stream_conn.recv(2 - len(ok_message))
   # Connection successful
   print 'Connected to stream server'

Remote Procedures
-----------------

Remote procedures are arranged into groups called services. These act as a
single-level namespacing to keep things organized. Each service has a unique
name used to identify it, and within a service each procedure has a unique name.

Invoking Remote Procedures
^^^^^^^^^^^^^^^^^^^^^^^^^^

Remote procedures are invoked by sending a request message to the RPC server,
and waiting for a response message. These messages are encoded using Protocol
Buffers.

The request message contains the name of the procedure to invoke, and the values
of the arguments to pass it. The response message contains the value returned by
the procedure, or is a blank response if the procedure does not return a value.

Requests are processed in order of receipt. The next request will not be
processed until the previous one completes and it's response has been received
by the client. When there are multiple client connections, the requests are
processed in round-robin order.

Anatomy of a Request
^^^^^^^^^^^^^^^^^^^^

A request is sent to the server using a ``Request`` Protocol Buffer message with
the following format:

.. code-block:: protobuf

   message Request {
     required string service = 1;
     required string procedure = 2;
     repeated Argument arguments = 3;
   }

   message Argument {
     required uint32 position = 1;
     required bytes value = 2;
   }

The fields are:

* ``service`` - The name of the service in which the remote procedure is defined.

* ``procedure`` - The name of the remote procedure to invoke.

* ``arguments`` - A sequence of ``Argument`` messages containing the values of the procedure's
  arguments. The fields are:

  * ``position`` - The zero-indexed position of the of the argument in the procedure's
    signature.

  * ``value`` - The value of the argument, encoded in Protocol Buffer format.

The ``Argument`` messages have a position field to allow values for default
arguments to be omitted. See :ref:`communication-protocol-protobuf-encoding` for
details on how to serialize the argument values.

Anatomy of a response
^^^^^^^^^^^^^^^^^^^^^

A response is sent to the client using a ``Response`` Protocol Buffer message
with the following format:

.. code-block:: protobuf

   message Response {
     required double time = 1;
     optional string error = 2;
     optional bytes return_value = 3;
   }

The fields are:

* ``time`` - The universal time (in seconds) when the request completed
  processing.

* ``error`` - Blank if the remote procedure completed successfully, otherwise
  contains a description of the error encountered.

* ``return_value`` - The value returned by the remote procedure encoded in
  protocol buffer format, or blank if the procedure does not return a value or
  an error occurred.

See :ref:`communication-protocol-protobuf-encoding` for details on how to
unserialize the return value.

Encoding and Sending Requests and Responses
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

To send a request:

1. Encode a ``Request`` message using the
   :ref:`communication-protocol-protobuf-encoding`.

2. Send the size in bytes of the encoded ``Request`` message, encoded as a
   Protocol Buffer varint.

3. Send the message data.

To receive a response:

1. Read a Protocol Buffer varint, which contains the length of the ``Response``
   message data in bytes.

2. Receive and decode the ``Response`` message.

Example RPC invocation
^^^^^^^^^^^^^^^^^^^^^^

The following Python script invokes the ``GetStatus`` procedure from the
:ref:`KRPC service <communication-protocol-krpc-service>` using an already
established connection to the server (the ``conn`` variable).

The ``krpc.schema.KRPC`` package contains the Protocol Buffer message formats
``Request``, ``Response`` and ``Status`` compiled to python code using the
Protocol Buffer compiler. The ``EncodeVarint`` and ``DecodeVarint`` functions
are used to encode/decode integers to/from the Protocol Buffer varint
format. Their implementation is omitted for brevity.

.. code-block:: python

   # Create Request message
   request = krpc.schema.KRPC.Request()
   request.service = 'KRPC'
   request.procedure = 'GetStatus'

   # Encode and send the request
   data = request.SerializeToString()
   header = EncodeVarint(len(data))
   rpc_conn.sendall(header + data)

   # Receive the size of the response data
   data = b''
   while True:
       data += rpc_conn.recv(1)
       try:
           (size, position) = DecodeVarint(data)
           break
       except IndexError:
           pass

   # Receive the response data
   data = b''
   while len(data) < size:
       data += rpc_conn.recv(size - len(data))

   # Decode the response message
   response = krpc.schema.KRPC.Response()
   response.ParseFromString(data)

   # Check for an error response
   if response.HasField('error'):
       print 'ERROR:', response.error

   # Decode the return value as a Status message
   else:
       status = krpc.schema.KRPC.Status()
       status.ParseFromString(response.return_value)

       # Print out the version string from the Status message
       print status.version

.. _communication-protocol-protobuf-encoding:

Protocol Buffer Encoding
------------------------

Values passed as arguments or received as return values are encoded using the
Protocol Buffer serialization format:

* Documentation for this encoding can be found here:
  https://developers.google.com/protocol-buffers/docs/encoding

* Protocol Buffer serialization libraries are available for C++/Java/Python here:
  http://code.google.com/p/protobuf/downloads/list

* There are implementations available for most popular languages here:
  http://code.google.com/p/protobuf/wiki/ThirdPartyAddOns

.. _communication-protocol-streams:

Streams
-------

TODO

.. _communication-protocol-krpc-service:

KRPC Service
------------

The server provides a service called ``KRPC`` containing procedures that are
used to retrieve information about the server and add/remove streams.

GetStatus
^^^^^^^^^

The ``GetStatus`` procedure returns status information about the server. It
returns a Protocol Buffer message with the format:

.. code-block:: protobuf

   message Status {
     required string version = 1;
   }

The field ``version`` is the version string of the server.

.. _communication-protocol-get-services:

GetServices
^^^^^^^^^^^

The ``GetServices`` procedure returns a Protocol Buffer message containing
information about all of the services and procedures provided by the server. It
also provides type information about each procedure, in the form of
:ref:`attributes <communication-protocol-attributes>`. The format of the message
is:

.. code-block:: protobuf

   message Services {
     repeated Service services = 1;
   }

This contains a single field, which is a list of ``Service`` messages with
information about each service provided by the server. The content of these
``Service`` messages are :ref:`documented below
<communication-protocol-service-description-message>`.

AddStream
^^^^^^^^^

The ``AddStream`` procedure adds a stream to the server.

TODO

RemoveStream
^^^^^^^^^^^^

The ``RemoveStream`` procedure removes a stream from the server.

TODO

.. _communication-protocol-service-description-message:

Service Description Message
---------------------------

The :ref:`GetServices procedure <communication-protocol-get-services>` returns
information about all of the services provided by the server. Details about a
service are given by a ``Service`` message, with the format:

.. code-block:: protobuf

   message Service {
     required string name = 1;
     repeated Procedure procedures = 2;
     repeated Class classes = 3;
     repeated Enumeration enumerations = 4;
   }

The fields are:

* ``name`` - The name of the service.

* ``procedures`` - A list of ``Procedure`` messages, one for each procedure
  defined by the service.

* ``classes`` - A list of ``Class`` messages, one for each :class:`KRPCClass`
  defined by the service.

* ``enumerations`` - A list of ``Enumeration`` messages, one for each
  :class:`KRPCEnum` defined by the service.

.. note:: See the :ref:`extending` documentation for more details about
          :class:`KRPCClass` and :class:`KRPCEnum`.

Procedures
^^^^^^^^^^

Details about a procedure are given by a ``Procedure`` message, with the format:

.. code-block:: protobuf

   message Procedure {
     required string name = 1;
     repeated Parameter parameters = 2;
     optional string return_type = 3;
     repeated string attributes = 4;
   }

   message Parameter {
     required string name = 1;
     required string type = 2;
     optional bytes default_argument = 3;
   }

The fields are:

* ``name`` - The name of the procedure.

* ``parameters`` - A list of ``Parameter`` messages containing details of the
  procedure's parameters, with the following fields:

   * ``name`` - The name of the parameter, to allow parameter passing by name.

   * ``type`` - The :ref:`type <communication-protocol-type-names>` of the
     parameter.

   * ``default_argument`` - The value of the default value of the parameter,
     :ref:`encoded using Protocol Buffer format
     <communication-protocol-protobuf-encoding>`, or blank if the parameter has no
     default value.

* ``return_type`` - The :ref:`return type <communication-protocol-type-names>`
  of the procedure.

* ``attributes`` - The procedure's :ref:`attributes
  <communication-protocol-attributes>`.

Classes
^^^^^^^

Details about each :class:`KRPCClass` are specified in a ``Class`` message, with the
format:

.. code-block:: protobuf

   message Class {
     required string name = 1;
   }

The fields are:

* ``name`` - The name of the class.

Enumerations
^^^^^^^^^^^^

Details about each :class:`KRPCEnum` are specified in an ``Enumeration`` message,
with the format:

.. code-block:: protobuf

   message Enumeration {
     required string name = 1;
     repeated EnumerationValue values = 2;
   }

   message EnumerationValue {
     required string name = 1;
     required int32 value = 2;
   }

The fields are:

* ``name`` - The name of the enumeration.

* ``values`` - A list of ``EnumerationValue`` messages, indicating the values
  that the enumeration can be assigned. The fields are:

  * ``name`` - The name associated with the value for the enumeration.

  * ``value`` - The possible value for the enumeration as a 32-bit integer.

.. _communication-protocol-attributes:

Attributes
^^^^^^^^^^

Additional type information about a procedure is encoded as a list of
attributes, and included in the ``Procedure`` message. For example, if the
procedure implements a method for a class (see :ref:`proxy objects
<communication-protocol-proxy-objects>`) this fact will be specified in the
attributes.

The following attributes specify what the procedure implements:

 * ``Property.Get(property-name)``

   Indicates that the procedure is a property getter (for the service) with the
   given ``property-name``.

 * ``Property.Set(property-name)``

   Indicates that the procedure is a property setter (for the service) with the
   given ``property-name``.

 * ``Class.Method(class-name,method-name)``

   Indicates that the procedure is a method for a class with the given
   ``class-name`` and ``method-name``.

 * ``Class.StaticMethod(class-name,method-name)``

   Indicates that the procedure is a static method for a class with the given
   ``class-name`` and ``method-name``.

 * ``Class.Property.Get(class-name,property-name)``

   Indicates that the procedure is a property getter for a class with the given
   ``class-name`` and ``property-name``.

 * ``Class.Property.Set(class-name,property-name)``

   Indicates that the procedure is a property setter for a class with the given
   ``class-name`` and ``property-name``.

The following attributes specify more details about the return and parameter types of the procedure.

 * ``ReturnType.type-name``

   Specifies the actual :ref:`return type <communication-protocol-type-names>`
   of the procedure, if it differs to the type specified in the ``Procedure``
   message. For example, this is used with :ref:`proxy objects
   <communication-protocol-proxy-objects>`.

 * ``ParameterType(parameter-position).type-name``

   Specifies the actual :ref:`parameter type
   <communication-protocol-type-names>` of the procedure, if it differs to the
   type of the corresponding parameter specified in the ``Parameter``
   message. For example, this is used with :ref:`proxy objects
   <communication-protocol-proxy-objects>`.

.. _communication-protocol-type-names:

Type Names
^^^^^^^^^^

The ``GetServices`` procedure returns type information about parameters and
return values as strings. Type names can be any of the following:

 * A Protocol Buffer value type. One of ``double``, ``float``, ``int32``,
   ``int64``, ``uint32``, ``uint64``, ``bool``, ``string`` or ``bytes``

 * A KRPCClass, in the format ``Class(ClassName)``

 * A Protocol Buffer message type, in the format ``ServiceName.MessageName``

 * A Protocol Buffer enumeration type, in the format
   ``ServiceName.EnumerationName``

.. _communication-protocol-proxy-objects:

Proxy Objects
^^^^^^^^^^^^^

kRPC allows procedures to create objects on the server, and passes unique
identifiers for them to the client. This allows the client to create a *proxy*
object for the actual object, whose methods and properties make remote procedure
calls to the server. Object identifiers have type ``uint64``.

When a procedure returns a proxy object, the procedure will have the attribute
``ReturnType.Class(ClassName)`` where ``ClassName`` is the name of the class.

When a procedure takes a proxy object as a parameter, the procedure will have
the attribute ``ParameterType(n).Class(ClassName)`` where ``n`` is the position
of the parameter and ``ClassName`` is the name of the class.
