.. _communication-protocol:

Communication Protocol
======================

Clients invoke Remote Procedure Calls (RPCs) by communicating with the server
using `Protocol Buffer v3 messages
<https://developers.google.com/protocol-buffers/docs/proto3>`_ sent over a
TCP/IP connection. The kRPC `download
<https://github.com/krpc/krpc/releases/latest>`_ comes with a protocol buffer
message definitions file (`schema/krpc.proto
<https://github.com/krpc/krpc/blob/latest-version/protobuf/krpc.proto>`_) that
defines the structure of these messages. It also contains versions of this file
for C#, C++, Java, Lua and Python, compiled using `Google's protocol buffers
compiler <https://github.com/google/protobuf>`_.

The following sections describe how to communicate with kRPC using snippets of
Python code. A complete example script made from these snippets can be
:download:`downloaded here </scripts/communication-protocol.py>`.

Establishing a Connection
-------------------------

kRPC consists of two servers: an *RPC server* (over which clients send and
receive RPCs) and a *stream server* (over which clients receive
:ref:`communication-protocol-streams`). A client first connects to the *RPC
Server*, then (optionally) to the *Stream Server*.

.. _communication-protocol-rpc-connect:

Connecting to the RPC Server
^^^^^^^^^^^^^^^^^^^^^^^^^^^^

To establish a connection to the RPC server, a client must do the following:

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

For example, this python code will connect to the RPC server at address
``127.0.0.1:50000`` using the identifier ``Jeb``:

.. literalinclude:: /scripts/communication-protocol.py
   :lines: 1-17

Connecting to the Stream Server
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

To establish a connection to the stream server, a client must first
:ref:`connect to the RPC Server <communication-protocol-rpc-connect>` then do
the following:

1. Open a TCP socket to the server on its stream port (which defaults to 50001).

2. Send this 12 byte hello message:
   ``0x48 0x45 0x4C 0x4C 0x4F 0x2D 0x53 0x54 0x52 0x45 0x41 0x4D``

3. Send a 16 byte message containing the client's unique identifier. This
   identifier is given to the client after it successfully connects to the RPC
   server.

4. Receive a 2 byte OK message: ``0x4F 0x4B`` This indicates a successful
   connection.

.. note:: Connecting to the Stream Server is optional. If the client doesn't
          require stream functionality, there is no need to connect.

For example, this python code will connect to the stream server at address
``127.0.0.1:50001``. Note that ``identifier`` is the unique client identifier
received when :ref:`connecting to the RPC server
<communication-protocol-rpc-connect>`.

.. literalinclude:: /scripts/communication-protocol.py
   :lines: 19-30

Remote Procedures
-----------------

Remote procedures are arranged into groups called *services*. These act as a
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

Requests are processed in order of receipt. The next request from a client will
not be processed until the previous one completes execution and it's response
has been received by the client. When there are multiple client connections,
requests are processed in round-robin order.

.. _communication-protocol-anatomy-of-a-request:

Anatomy of a Request
^^^^^^^^^^^^^^^^^^^^

A request is sent to the server using a ``Request`` Protocol Buffer message with
the following format:

.. code-block:: protobuf

   message Request {
     string service = 1;
     string procedure = 2;
     repeated Argument arguments = 3;
   }

   message Argument {
     uint32 position = 1;
     bytes value = 2;
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
     double time = 1;
     bool has_error = 2;
     string error = 3;
     bool has_return_value = 4;
     bytes return_value = 5;
   }

The fields are:

* ``time`` - The universal time (in seconds) when the request completed
  processing.

* ``has_error`` - True if there was an error executing the remote procedure.

* ``error`` - If ``has_error`` is true, contains a description of the error.

* ``has_return_value`` - True if the remote procedure returned a value.

* ``return_value`` - If ``has_return_value`` is true and ``has_error`` is false,
  contains the value returned by the remote procedure, encoded in protocol
  buffer format.

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

.. literalinclude:: /scripts/communication-protocol.py
   :lines: 32-

.. _communication-protocol-protobuf-encoding:

Protocol Buffer Encoding
------------------------

Values passed as arguments or received as return values are encoded using the
Protocol Buffer version 3 serialization format:

* Documentation for this encoding can be found here:
  https://developers.google.com/protocol-buffers/docs/encoding

* Protocol Buffer libraries in many languages are available here:
  https://github.com/google/protobuf/releases

.. _communication-protocol-streams:

Streams
-------

Streams allow the client to repeatedly execute an RPC on the server and receive
its results, without needing to repeatedly call the RPC directly, avoiding the
communication overhead that this would involve.

A client can create a stream on the server by calling
:ref:`communication-protocol-add-stream`. Once the client is finished with the
stream, it can remove it from the server by calling
:ref:`communication-protocol-remove-stream`. Streams are automatically removed
when the client that created it disconnects from the server. Streams are local
to each client and there is no way to share a stream between clients.

The RPC for each stream is invoked every `fixed update
<http://docs.unity3d.com/ScriptReference/MonoBehaviour.FixedUpdate.html>`_ and
the return values for all of these RPCs are collected together into a *stream
message*. This is then sent to the client over the stream server's TCP/IP
connection. If the value returned by a stream's RPC does not change since the
last update that was sent, its value is omitted from the update message in order
to minimize network traffic.

Anatomy of a Stream Message
^^^^^^^^^^^^^^^^^^^^^^^^^^^

A stream message is sent to the client using a ``StreamMessage`` Protocol Buffer
message with the following format:

.. code-block:: protobuf

   message StreamMessage {
     repeated StreamResponse responses = 1;
   }

This contains a list of ``StreamResponse`` messages, one for each stream that
exists on the server for that client, and whose return value changed since the
last update was sent. It has the following format:

.. code-block:: protobuf

   message StreamResponse {
     uint32 id = 1;
     Response response = 2;
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
used to retrieve information about the server and to manage streams.

GetStatus
^^^^^^^^^

The ``GetStatus`` procedure returns status information about the server. It
returns a Protocol Buffer message with the format:

.. code-block:: protobuf

   message Status {
     string version = 1;
     uint64 bytes_read = 2;
     uint64 bytes_written = 3;
     float bytes_read_rate = 4;
     float bytes_written_rate = 5;
     uint64 rpcs_executed = 6;
     float rpc_rate = 7;
     bool one_rpc_per_update = 8;
     uint32 max_time_per_update = 9;
     bool adaptive_rate_control = 10;
     bool blocking_recv = 11;
     uint32 recv_timeout = 12;
     float time_per_rpc_update = 13;
     float poll_time_per_rpc_update = 14;
     float exec_time_per_rpc_update = 15;
     uint32 stream_rpcs = 16;
     uint64 stream_rpcs_executed = 17;
     float stream_rpc_rate = 18;
     float time_per_stream_update = 19;
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
     string name = 1;
     repeated Procedure procedures = 2;
     repeated Class classes = 3;
     repeated Enumeration enumerations = 4;
     string documentation = 5;
   }

The fields are:

* ``name`` - The name of the service.

* ``procedures`` - A list of ``Procedure`` messages, one for each procedure
  defined by the service.

* ``classes`` - A list of ``Class`` messages, one for each :csharp:attr:`KRPCClass`
  defined by the service.

* ``enumerations`` - A list of ``Enumeration`` messages, one for each
  :csharp:attr:`KRPCEnum` defined by the service.

* ``documentation`` - Documentation for the service, as `C# XML documentation`_.

.. note:: See the :ref:`extending` documentation for more details about
          :csharp:attr:`KRPCClass` and :csharp:attr:`KRPCEnum`.

Procedures
^^^^^^^^^^

Details about a procedure are given by a ``Procedure`` message, with the format:

.. code-block:: protobuf

   message Procedure {
     string name = 1;
     repeated Parameter parameters = 2;
     bool has_return_type = 3;
     string return_type = 4;
     repeated string attributes = 5;
     string documentation = 6;
   }

   message Parameter {
     string name = 1;
     string type = 2;
     bool has_default_argument = 3;
     bytes default_argument = 4;
   }

The fields are:

* ``name`` - The name of the procedure.

* ``parameters`` - A list of ``Parameter`` messages containing details of the
  procedure's parameters, with the following fields:

   * ``name`` - The name of the parameter, to allow parameter passing by name.

   * ``type`` - The :ref:`type <communication-protocol-type-names>` of the
     parameter.

   * ``has_default_argument`` - True if the parameter has a default value.

   * ``default_argument`` - If ``has_default_argument`` is true, contains the
     value of the default value of the parameter, :ref:`encoded using Protocol
     Buffer format <communication-protocol-protobuf-encoding>`.

* ``has_return_type`` - True if the procedure returns a value.

* ``return_type`` - If ``has_return_type`` is true, contains the :ref:`return
  type <communication-protocol-type-names>` of the procedure.

* ``attributes`` - The procedure's :ref:`attributes
  <communication-protocol-attributes>`.

* ``documentation`` - Documentation for the procedure, as `C# XML documentation`_.

Classes
^^^^^^^

Details about each :csharp:attr:`KRPCClass` are specified in a ``Class``
message, with the format:

.. code-block:: protobuf

   message Class {
     string name = 1;
     string documentation = 2;
   }

The fields are:

* ``name`` - The name of the class.

* ``documentation`` - Documentation for the class, as `C# XML documentation`_.

Enumerations
^^^^^^^^^^^^

Details about each :csharp:attr:`KRPCEnum` are specified in an ``Enumeration``
message, with the format:

.. code-block:: protobuf

   message Enumeration {
     string name = 1;
     repeated EnumerationValue values = 2;
     string documentation = 3;
   }

   message EnumerationValue {
     string name = 1;
     int32 value = 2;
     string documentation = 3;
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

 * A Protocol Buffer value type. One of ``float``, ``double``, ``int32``,
   ``int64``, ``uint32``, ``uint64``, ``bool``, ``string`` or ``bytes``.

 * A :csharp:attr:`KRPCClass` in the format ``Class(ClassName)``

 * A :csharp:attr:`KRPCEnum` in the format ``Enum(ClassName)``

 * A Protocol Buffer message type, in the format ``KRPC.MessageType``. Only
   message types defined in ``krpc.proto`` are permitted.

.. _communication-protocol-proxy-objects:

Proxy Objects
^^^^^^^^^^^^^

kRPC allows procedures to create objects on the server, and pass a unique
identifier for them to the client. This allows the client to create a *proxy*
object for the actual object, whose methods and properties make remote procedure
calls to the server. Object identifiers have type ``uint64``.

When a procedure returns a proxy object, the procedure will have the attribute
``ReturnType.Class(ClassName)`` where ``ClassName`` is the name of the class.

When a procedure takes a proxy object as a parameter, the procedure will have
the attribute ``ParameterType(n).Class(ClassName)`` where ``n`` is the position
of the parameter and ``ClassName`` is the name of the class.

.. _C# XML documentation: https://msdn.microsoft.com/en-us/library/aa288481%28v=vs.71%29.aspx
