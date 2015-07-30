.. _communication-protocol:

Communication Protocol
======================

Clients invoke Remote Procedure Calls (RPCs) by communicating with the server
using `Protocol Buffer messages
<https://developers.google.com/protocol-buffers/docs/proto>`_ sent over a TCP/IP
connection.

The kRPC `download <https://github.com/djungelorm/krpc/releases>`_ comes with a
protocol buffer message definitions file (`KRPC.proto
<https://github.com/djungelorm/krpc/blob/latest-version/src/kRPC/Schema/KRPC.proto>`_)
that defines the structure of these messages. It also includes versions of this
file compiled for Python, Java and C++ using `Google's protocol buffers compiler
<https://github.com/google/protobuf>`_.

Establishing a Connection
-------------------------

kRPC consists of two servers: an *RPC Server* (over which clients send and
receive RPCs) and a *Stream Server* (over which clients receive
:ref:`communication-protocol-streams`). A client first connects to the *RPC
Server*, then (optionally) to the *Stream Server*.

.. _communication-protocol-rpc-connect:

Connecting to the RPC Server
^^^^^^^^^^^^^^^^^^^^^^^^^^^^

To establish a connection to the *RPC Server*, a client must do the following:

 1. Open a TCP socket to the server on its RPC port (which defaults to 50000).

 2. Send this 12 byte hello message:
    ``0x48 0x45 0x4C 0x4C 0x4F 0x2D 0x52 0x50 0x43 0x00 0x00 0x00``

 3. Send a 32 byte message containing a name for the connection, that will be
    displayed on the in-game server window. This should be a UTF-8 encoded
    string, up to a maximum of 32 bytes in length. If the string is shorter than
    32 bytes, it should be padded with zeros.

 4. Receive a 16 byte unique client identifier. This is sent to the client when
    the connection is granted, for example after the user has clicked accept on
    the in-game UI.

For example, this python code will connect to the *RPC Server* at address
``127.0.0.1:50000`` using the identifier ``Jeb``:

.. code-block:: python

   import socket
   rpc_conn = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
   rpc_conn.connect(('127.0.0.1', 50000))
   # Send the 12 byte hello message
   rpc_conn.sendall(b'\x48\x45\x4C\x4C\x4F\x2D\x52\x50\x43\x00\x00\x00')
   # Send the 32 byte client name 'Jeb' padded with zeroes
   name = 'Jeb'.encode('utf-8')
   name += (b'\x00' * (32-len(name)))
   rpc_conn.sendall(name)
   # Receive the 16 byte client identifier
   identifier = b''
   while len(identifier) < 16:
       identifier += rpc_conn.recv(16 - len(identifier))
   # Connection successful. Print out a message along with the client identifier.
   printable_identifier = ''.join('%02s' % x for x in identifier)
   print('Connected to RPC server, client idenfitier = %s' % printable_identifier)

Connecting to the Stream Server
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

To establish a connection to the *Stream Server*, a client must first
:ref:`connect to the RPC Server <communication-protocol-rpc-connect>` then do
the following:

1. Open a TCP socket to the server on its Stream port (which defaults to 50001).

2. Send this 12 byte hello message:
   ``0x48 0x45 0x4C 0x4C 0x4F 0x2D 0x53 0x54 0x52 0x45 0x41 0x4D``

3. Send a 16 byte message containing the client's unique identifier. This
   identifier is given to the client after it successfully connects to the *RPC
   Server*.

4. Receive a 2 byte OK message: ``0x4F 0x4B`` This indicates a successful
   connection.

.. note:: Connecting to the Stream Server is optional. If the client doesn't
          require stream functionality, there is no need to connect.

For example, this python code will connect to the *Stream Server* at address
``127.0.0.1:50001``. Note that ``identifier`` is the unique client identifier
received when :ref:`connecting to the RPC server
<communication-protocol-rpc-connect>`.

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
   print('Connected to stream server')

Remote Procedures
-----------------

Remote procedures are arranged into groups called services. These act as a
single-level namespacing to keep things organized. Each service has a unique
name used to identify it, and within a service each procedure has a unique name.

Invoking Remote Procedures
^^^^^^^^^^^^^^^^^^^^^^^^^^

Remote procedures are invoked by sending a request message to the RPC server,
and waiting for a response message. These messages are encoded as Protocol
Buffer messages.

The request message contains the name of the procedure to invoke, and the values
of any arguments to pass it. The response message contains the value returned by
the procedure (if any) and any errors that were encountered.

Requests are processed in order of receipt. The next request will not be
processed until the previous one completes and it's response has been received
by the client. When there are multiple client connections, the requests are
processed in round-robin order.

.. _communication-protocol-anatomy-of-a-request:

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

.. _communication-protocol-anatomy-of-a-response:

Anatomy of a Response
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
established connection to the server (the ``rpc_conn`` variable).

The ``krpc.schema.KRPC`` package contains the Protocol Buffer message formats
``Request``, ``Response`` and ``Status`` compiled to python code using the
Protocol Buffer compiler. The ``EncodeVarint`` and ``DecodeVarint`` functions
are used to encode/decode integers to/from the Protocol Buffer varint
format.

.. code-block:: python

   def EncodeVarint(value):
     return krpc.Encoder.encode(value,krpc.types.ValueType("int32"))
   def DecodeVarint(data) :
     return krpc.Decoder.decode(data,krpc.types.ValueType("int32"))

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
           size = DecodeVarint(data)
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
       print('ERROR:', response.error)

   # Decode the return value as a Status message
   else:
       status = krpc.schema.KRPC.Status()
       status.ParseFromString(response.return_value)

       # Print out the version string from the Status message
       print(status.version)

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

Streams allow the client to repeatedly execute a Remote Procedure Call on the
server and receive its results, without needing to repeatedly call the Remote
Procedure Call directly, avoiding the communication overhead that this would
involve.

A stream is created on the server by calling
:ref:`communication-protocol-add-stream` which returns a unique identifier for
the stream. Once a client is finished with a stream, it can remove it from the
server by calling :ref:`communication-protocol-remove-stream` with the stream's
identifier. Streams are automatically removed when the client that created it
disconnects from the server. Streams are local to each client. There is no way
to share a stream between clients.

The results of the RPCs for each stream are sent to the client over the Stream
Server's TCP/IP connection, as repeated *stream messages*. The RPC for each
stream is invoked every `fixed update
<http://docs.unity3d.com/ScriptReference/MonoBehaviour.FixedUpdate.html>`_.

Anatomy of a Stream Message
^^^^^^^^^^^^^^^^^^^^^^^^^^^

A stream message is sent to the client using a ``StreamMessage`` Protocol Buffer
message with the following format:

.. code-block:: protobuf

   message StreamMessage {
     repeated StreamResponse responses = 1;
   }

This message contains a list of ``StreamResponse`` messages, one for each stream
that exists on the server for that client, with the following format:

.. code-block:: protobuf

   message StreamResponse {
     required uint32 id = 1;
     required Response response = 2;
   }

The fields are:

* ``id`` - The identifier of the stream. This is the value returned by
  :ref:`communication-protocol-add-stream` when the stream is created.

* ``response`` - A ``Response`` message containing the result of the stream's
  RPC. This is identical to the ``Response`` message returned when calling the
  RPC directly. See :ref:`communication-protocol-anatomy-of-a-response` for
  details on the format and contents of this message.

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
     required uint64 bytes_read = 2;
     required uint64 bytes_written = 3;
     required float bytes_read_rate = 4;
     required float bytes_written_rate = 5;
     required uint64 rpcs_executed = 6;
     required float rpc_rate = 7;
     required bool adaptive_rate_control = 8;
     required uint32 max_time_per_update = 9;
     required bool blocking_recv = 10;
     required uint32 recv_timeout = 11;
     required float time_per_rpc_update = 12;
     required float poll_time_per_rpc_update = 13;
     required float exec_time_per_rpc_update = 14;
     required uint32 stream_rpcs = 15;
     required uint64 stream_rpcs_executed = 16;
     required float stream_rpc_rate = 17;
     required float time_per_stream_update = 18;
   }

The ``version`` field contains the version string of the server. The remaining
fields contain performance information about the server.

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

.. _communication-protocol-add-stream:

AddStream
^^^^^^^^^

The ``AddStream`` procedure adds a new stream to the server. It takes a single
argument containing the RPC to invoke, encoded as a ``Request`` object. See
:ref:`communication-protocol-anatomy-of-a-request` for the format and contents
of this object. See :ref:`communication-protocol-streams` for more information
on working with streams.

.. _communication-protocol-remove-stream:

RemoveStream
^^^^^^^^^^^^

The ``RemoveStream`` procedure removes a stream from the server. It takes a
single argument -- the identifier of the stream to be removed. This is the
identifier returned when the stream was added by calling
:ref:`communication-protocol-add-stream`. See
:ref:`communication-protocol-streams` for more information on working with
streams.

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
     optional string documentation = 5;
   }

The fields are:

* ``name`` - The name of the service.

* ``procedures`` - A list of ``Procedure`` messages, one for each procedure
  defined by the service.

* ``classes`` - A list of ``Class`` messages, one for each :class:`KRPCClass`
  defined by the service.

* ``enumerations`` - A list of ``Enumeration`` messages, one for each
  :class:`KRPCEnum` defined by the service.

* ``documentation`` - Documentation for the service, as `C# XML documentation`_.

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
     optional string documentation = 5;
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

* ``documentation`` - Documentation for the procedure, as `C# XML documentation`_.

Classes
^^^^^^^

Details about each :class:`KRPCClass` are specified in a ``Class`` message, with the
format:

.. code-block:: protobuf

   message Class {
     required string name = 1;
  optional string documentation = 2;
   }

The fields are:

* ``name`` - The name of the class.

* ``documentation`` - Documentation for the class, as `C# XML documentation`_.

Enumerations
^^^^^^^^^^^^

Details about each :class:`KRPCEnum` are specified in an ``Enumeration`` message,
with the format:

.. code-block:: protobuf

   message Enumeration {
     required string name = 1;
     repeated EnumerationValue values = 2;
     optional string documentation = 3;
   }

   message EnumerationValue {
     required string name = 1;
     required int32 value = 2;
     optional string documentation = 3;
   }

The fields are:

* ``name`` - The name of the enumeration.

* ``values`` - A list of ``EnumerationValue`` messages, indicating the values
  that the enumeration can be assigned. The fields are:

  * ``name`` - The name associated with the value for the enumeration.

  * ``value`` - The possible value for the enumeration as a 32-bit integer.

  * ``documentation`` - Documentation for the enumeration value, as `C# XML documentation`_.

* ``documentation`` - Documentation for the enumeration, as `C# XML documentation`_.

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

 * A KRPCEnum, in the format ``Enum(ClassName)``

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

.. _C# XML documentation: https://msdn.microsoft.com/en-us/library/aa288481%28v=vs.71%29.aspx
