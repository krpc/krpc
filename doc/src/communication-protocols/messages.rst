Messaging Protocol
==================

Communication with a kRPC server is performed via Protocol Buffer messages. The `kRPC download
<https://github.com/krpc/krpc/releases/latest>`_ comes with a protocol buffer message definitions
file (`schema/krpc.proto <https://github.com/krpc/krpc/blob/latest-version/protobuf/krpc.proto>`_)
that defines the structure of these messages. It also contains versions of this file for C#, C++,
Java, Lua and Python, compiled using `Google's protocol buffers compiler
<https://github.com/google/protobuf>`_.

Invoking Remote Procedures
--------------------------

Remote procedures are arranged into groups called *services*. These act as a single-level
namespacing to keep things organized. Each service has a unique name used to identify it, and within
a service :ref:`each procedure has a unique name <communication-protocol-procedure-names>`.

Remote procedures are invoked by sending a request message to the RPC server, and waiting for a
response message.

The request message contains one or more procedure calls to run, which include the names of the
procedures and the values of their arguments. The response message contains the value(s) returned by
the procedure(s) and any errors that were encountered.

Requests are processed in order of receipt. The next request from a client will not be processed
until the previous one completes execution and it's response has been received by the client. When
there are multiple client connections, requests are processed in round-robin order.

Within a single request, the procedure calls are also processed in order of receipt. The results
list in the response is also ordered so that the results match the calls.

.. _communication-protocol-anatomy-of-a-request:

Anatomy of a Request
^^^^^^^^^^^^^^^^^^^^

A request is sent to the server using a ``Request`` Protocol Buffer message with the following
format:

.. code-block:: protobuf

   message Request {
     repeated ProcedureCall calls = 1;
   }

   message ProcedureCall {
     string service = 1;
     string procedure = 2;
     uint32 serviceId = 4;
     uint32 procedureId = 5;
     repeated Argument arguments = 3;
   }

   message Argument {
     uint32 position = 1;
     bytes value = 2;
   }

A request message contains one or more procedure calls. This allows efficient batching of multiple
calls in a single message, if desired.

The fields of a procedure call are:

* ``service`` - The name of the service in which the remote procedure is defined.

* ``procedure`` - The name of the remote procedure to invoke.

* ``service_id`` - The integer identifier of the service in which the remote procedure is defined.

* ``procedure_id`` - The integer identifier of the remote procedure to invoke.

* ``arguments`` - A sequence of ``Argument`` messages containing the values of the procedure's
  arguments. The fields of an argument message are:

  * ``position`` - The zero-indexed position of the of the argument in the procedure's
    signature.

  * ``value`` - The value of the argument, encoded in Protocol Buffer format.

The service containing the procedure to call is specified by setting either ``service`` or
``service_id``. The procedure to call is specified by setting either ``procedure`` or
``procedure_id``. Use of ``service`` and ``procedure`` (i.e. descriptive strings) should be
preferred as these will not change between server versions. For clients where code size or
communication overhead must be kept to an absolute minimum, ``service_id`` and ``procedure_id``
can be used. However, note that the identifiers may change between server versions.

The ``Argument`` messages have a position field to allow values for default arguments to be
omitted. See :ref:`communication-protocol-protobuf-encoding` for details on how to encode the
argument values.

.. _communication-protocol-anatomy-of-a-response:

Anatomy of a Response
^^^^^^^^^^^^^^^^^^^^^

A response is sent to the client using a ``Response`` Protocol Buffer message with the following
format:

.. code-block:: protobuf

   message Response {
     Error error = 1;
     repeated ProcedureResult results = 2;
   }

   message ProcedureResult {
     Error error = 1;
     bytes value = 2;
   }

   message Error {
     string service = 1;
     string name = 2;
     string description = 3;
     string stack_trace = 4;
   }

A response message contains one or more results, corresponding to the procedure calls made in the
associated request message.

The ``value`` field of a procedure result message contains the value of the return value of the
remote procedure, if any, encoded in protocol buffer format. See
:ref:`communication-protocol-protobuf-encoding` for details on how to decode the return value.

If an error occurs processing a request message, the ``error`` field in the response message will
contain a description of the error. If an individual procedure call encounters an error, the
``error`` field in the corresponding procedure result message will contain a description of the
error.

The fields of an error message are:

 * ``service`` - If the error was caused by an exception, this is the name of the service in which the
   exception type is defined.

 * ``name`` - If the error was caused by an exception, this is the name of the exception type.

 * ``description`` - A human readable description of the error

 * ``stack_trace`` - If the error was caused by an exception, this is a server-side stack trace for
   the exception.

Example RPC invocation
^^^^^^^^^^^^^^^^^^^^^^

The following Python code invokes the ``GetStatus`` procedure from the :ref:`KRPC service
<communication-protocol-krpc-service>` using an already established connection to the server (the
``rpc_conn`` variable).

The ``krpc.schema.KRPC`` package contains the various Protocol Buffer message formats compiled to
python code using the Protocol Buffer compiler. The ``send_message`` and ``recv_message`` are helper
functions used to send/receive messages from the server.

.. literalinclude:: /scripts/communication-protocol-rpc.py
   :lines: 64-

.. _communication-protocol-protobuf-encoding:

Protocol Buffer Encoding
^^^^^^^^^^^^^^^^^^^^^^^^

Values passed as arguments or received as return values are encoded using the
Protocol Buffer version 3 serialization format:

* Documentation for this encoding can be found here:
  https://developers.google.com/protocol-buffers/docs/encoding

* Protocol Buffer libraries in many languages are available here:
  https://github.com/google/protobuf/releases

.. _communication-protocol-streams:

Streams
-------

Streams allow the client to repeatedly execute an RPC on the server and receive its results, without
needing to repeatedly call the RPC directly, avoiding the communication overhead that this would
involve.

A client can create a stream on the server by calling :ref:`communication-protocol-add-stream`. This
procedure takes an optional boolean argument that controls whether the stream starts sending data to
the client or not. If not, :ref:`communication-protocol-start-stream` can be called later on to
start the stream. Once the client is finished with the stream, it can remove it from the server by
calling :ref:`communication-protocol-remove-stream`. Streams are automatically removed when the
client that created it disconnects from the server. Streams are local to each client and there is no
way to share a stream between clients.

The RPC for each stream is invoked every `fixed update
<https://docs.unity3d.com/ScriptReference/MonoBehaviour.FixedUpdate.html>`_ and the return values
for all of these RPCs are collected together into a stream update message. This is then sent to the
client over its stream server connection. If the value returned by a streams RPC does not change
since the last update that was sent, its value is omitted from the update message in order to
minimize network traffic. A client can also control the rate of the stream, by specifying a target
number of Hertz. The server computes a time delay from the target rate, and only updates the stream
if at least that time has passed since the last time the stream was updated.

Anatomy of a Stream Update
^^^^^^^^^^^^^^^^^^^^^^^^^^

Stream updates are sent to the client using a ``StreamUpdate`` Protocol Buffer message with the
following format:

.. code-block:: protobuf

   message StreamUpdate {
     repeated StreamResult results = 1;
   }

This contains a list of ``StreamResult`` messages, one for each stream that exists on the server for
that client, and whose return value changed since the last update was sent. It has the following
format:

.. code-block:: protobuf

   message StreamResult {
     uint64 id = 1;
     ProcedureResult result = 2;
   }

The fields are:

* ``id`` - The identifier of the stream. This is the value returned by
  :ref:`communication-protocol-add-stream` when the stream is created.

* ``result`` - A ``ProcedureResult`` message containing the result of the streams RPC. This is
  identical to the ``ProcedureResult`` message returned when calling the RPC directly. See
  :ref:`communication-protocol-anatomy-of-a-response` for details on the format and contents of this
  message.

Events
------

Events are a wrapper over a stream that returns a boolean value. The event is triggered when the
stream returns to true. Remote procedures that return an event return an ``Event`` message that
contains a ``Stream`` message describing the underlying stream for the event.

The format for an ``Event`` message is simply the following:

.. code-block:: protobuf

   message Event {
     repeated Stream stream = 1;
   }

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

The ``GetServices`` procedure returns a Protocol Buffer message containing information about all of
the services and procedures provided by the server. The format of the message is:

.. code-block:: protobuf

   message Services {
     repeated Service services = 1;
   }

This contains a single field, which is a list of ``Service`` messages with information about each
service provided by the server. The content of these ``Service`` messages are :ref:`documented below
<communication-protocol-service-description-message>`.

.. _communication-protocol-add-stream:

AddStream
^^^^^^^^^

The ``AddStream`` procedure adds a new stream to the server. Its first argument is the RPC to
invoke, encoded as a ``ProcedureCall`` message. The second argument is a boolean value indicating
whether the stream should start sending data to the client immediately. The procedure returns a
``Stream`` message describing the stream that was added. See
:ref:`communication-protocol-anatomy-of-a-request` for the format and contents of this message. See
:ref:`communication-protocol-streams` for more information on working with streams.

.. _communication-protocol-start-stream:

StartStream
^^^^^^^^^^^

The ``StartStream`` procedure starts a stream sending data to a client, if it has not yet been
started. It takes a single argument -- the identifier of the stream to start. This is the identifier
returned when the stream was added by calling :ref:`communication-protocol-add-stream`. See
:ref:`communication-protocol-streams` for more information on working with streams.

.. _communication-protocol-remove-stream:

RemoveStream
^^^^^^^^^^^^

The ``RemoveStream`` procedure removes a stream from the server. It takes a single argument -- the
identifier of the stream to be removed. This is the identifier returned when the stream was added by
calling :ref:`communication-protocol-add-stream`. See :ref:`communication-protocol-streams` for more
information on working with streams.

.. _communication-protocol-service-description-message:

Service Description Message
---------------------------

The :ref:`GetServices procedure <communication-protocol-get-services>` returns information about all
of the services provided by the server. Details about a service are given by a ``Service`` message,
with the format:

.. code-block:: protobuf

   message Service {
     string name = 1;
     repeated Procedure procedures = 2;
     repeated Class classes = 3;
     repeated Enumeration enumerations = 4;
     repeated Exception exceptions = 5;
     string documentation = 6;
   }

The fields are:

* ``name`` - The name of the service.

* ``procedures`` - A list of ``Procedure`` messages, one for each procedure defined by the service.

* ``classes`` - A list of ``Class`` messages, one for each :csharp:attr:`KRPCClass` defined by the
  service.

* ``enumerations`` - A list of ``Enumeration`` messages, one for each :csharp:attr:`KRPCEnum`
  defined by the service.

* ``exceptions`` - A list of ``Exception`` messages, one for each :csharp:attr:`KRPCException`
  defined by the service.

* ``documentation`` - Documentation for the service, as `C# XML documentation`_.

.. note:: See the :ref:`extending` documentation for more details about :csharp:attr:`KRPCClass`,
          :csharp:attr:`KRPCEnum` and :csharp:attr:`KRPCException`.

Procedures
^^^^^^^^^^

Details about a procedure are given by a ``Procedure`` message, with the format:

.. code-block:: protobuf

   message Procedure {
     string name = 1;
     repeated Parameter parameters = 2;
     Type return_type = 3;
     string documentation = 4;
   }

   message Parameter {
     string name = 1;
     Type type = 2;
     bytes default_value = 3;
   }

The fields are:

* ``name`` - The name of the procedure. See :ref:`communication-protocol-procedure-names` for more
  information.

* ``parameters`` - A list of ``Parameter`` messages containing details of the
  procedure's parameters, with the following fields:

   * ``name`` - The name of the parameter, to allow parameter passing by name.

   * ``type`` - The :ref:`type <communication-protocol-type>` of the parameter.

   * ``default_value`` - The value of the default value of the parameter, if any, :ref:`encoded
     using Protocol Buffer format <communication-protocol-protobuf-encoding>`.

* ``return_type`` - The :ref:`return type <communication-protocol-type>` of the procedure. If the
  procedure does not return anything its type is set to ``NONE``.

* ``documentation`` - Documentation for the procedure, as `C# XML documentation`_.

Classes
^^^^^^^

Details about each :csharp:attr:`KRPCClass` are specified in a ``Class`` message, with the format:

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

Details about each :csharp:attr:`KRPCEnum` are specified in an ``Enumeration`` message, with the
format:

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

* ``values`` - A list of ``EnumerationValue`` messages, indicating the values that the enumeration
  can be assigned. The fields are:

  * ``name`` - The name associated with the value for the enumeration.

  * ``value`` - The possible value for the enumeration as a 32-bit integer.

  * ``documentation`` - Documentation for the enumeration value, as `C# XML documentation`_.

* ``documentation`` - Documentation for the enumeration, as `C# XML documentation`_.

Exceptions
^^^^^^^^^^

Details about each :csharp:attr:`KRPCException` are specified in an ``Exception`` message, with the
format:

.. code-block:: protobuf

   message Exception {
     string name = 1;
     string documentation = 2;
   }

The fields are:

* ``name`` - The name of the exception type.

* ``documentation`` - Documentation for the exception, as `C# XML documentation`_.

.. _communication-protocol-procedure-names:

Procedure Names
---------------

Procedures names are CamelCase. Whether a procedure is a service procedure, class method, class
property, and what class (if any) it belongs to is determined by its name:

 * ``ProcedureName`` - a standard procedure that is just part of a service.
 * ``get_PropertyName`` - a procedure that returns the value of a property in a service.
 * ``set_PropertyName`` - a procedure that sets the value of a property in a service.
 * ``ClassName_MethodName`` - a class method.
 * ``ClassName_static_StaticMethodName`` - a static class method.
 * ``ClassName_get_PropertyName`` - a class property getter.
 * ``ClassName_set_PropertyName`` - a class property setter.

Only letters and numbers are permitted in class, method and property names. Underscores can
therefore be used to split the name into its constituent parts.

.. _communication-protocol-type:

Type
----

The ``GetServices`` procedure returns type information about parameters, return values and others as
``Type`` messages. The format of these messages is as follows:

.. code-block:: protobuf

   message Type {
     TypeCode code = 1;
     string service = 2;
     string name = 3;
     repeated Type types = 4;

     enum TypeCode {
       NONE = 0;

       // Values
       DOUBLE = 1;
       FLOAT = 2;
       SINT32 = 3;
       SINT64 = 4;
       UINT32 = 5;
       UINT64 = 6;
       BOOL = 7;
       STRING = 8;
       BYTES = 9;

       // Objects
       CLASS = 100;
       ENUMERATION = 101;

       // Messages
       PROCEDURE_CALL = 200;
       STREAM = 201;
       STATUS = 202;
       SERVICES = 203;

       // Collections
       TUPLE = 300;
       LIST = 301;
       SET = 302;
       DICTIONARY = 303;
     };
   }

The ``code`` field specifies the type. ``NONE`` is used as the return type for procedures that do
not return a value.

For ``CLASS`` and ``ENUMERATION`` types the ``service`` and ``name`` fields specify the service that
defines the class/enumeration and the name of the class/enumeration. For all other types these
fields are empty.

For collection types the ``types`` repeated field will contain the sub-types:

 * ``TUPLE`` types contain 1 or more types in the ``types`` field.
 * ``LIST`` and ``SET`` types contain a single type in the ``types`` field.
 * ``DICTIONARY`` types contain a 2 types in the ``types`` field - the key and value types, in that
   order.

For all other types the ``types`` field is empty.

.. _communication-protocol-proxy-objects:

Proxy Objects
-------------

kRPC allows procedures to create objects on the server, and pass a unique identifier for them to the
client. This allows the client to create a *proxy* object for the actual object, whose methods and
properties make remote procedure calls to the server. Object identifiers have type ``uint64``.

When a procedure returns a proxy object or takes one as a parameter, the type code will be set to
``CLASS``.

.. _C# XML documentation: https://msdn.microsoft.com/en-us/library/aa288481%28v=vs.71%29.aspx
