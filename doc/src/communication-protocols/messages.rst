Messaging
=========

A client and the server exchange Protocol Buffers messages. The file
:download:`krpc.proto </communication-protocols/krpc.proto>` defines them. This page describes the
messages and what they mean. The :doc:`client specification </client-spec>` states the precise
behavior of a client, such as how it handles a refused connection or a failed one.

.. _communication-protocol-message:

Messages
--------

Every message sent to or from the server is a ``Message``, which holds one of the following:

.. literalinclude:: /communication-protocols/krpc.proto
   :language: protobuf
   :start-at: message Message {
   :end-before: message Disconnect {

A client sends ``request``, ``connection_request`` and ``disconnect`` messages, and the server sends
``response``, ``stream_update``, ``connection_response`` and ``disconnect`` messages.

Each protocol frames the messages in its own way, as its page describes. For example,
:doc:`tcpip` sends the length of each encoded message as a varint, followed by the message bytes.

.. _communication-protocol-handshake:

Connection Handshake
^^^^^^^^^^^^^^^^^^^^

A client connects by sending a ``connection_request`` as its first ``Message``, and reading the
server's ``connection_response``. The following Python code connects over TCP/IP:

.. literalinclude:: /scripts/communication-protocols/tcpip/RPC.py
   :start-at: # Send a connection request
   :end-at: print("Connected to the server")

The two messages are:

.. literalinclude:: /communication-protocols/krpc.proto
   :language: protobuf
   :start-at: message ConnectionRequest {
   :end-before: // Messages for calling remote procedures

Set ``protocol_version`` to 2. The ``client_name`` field can be set to the name of the client to
display on the in-game UI. The ``connection_response`` holds one of the following statuses:

.. list-table::
   :header-rows: 1

   * - Status
     - Meaning
   * - ``OK``
     - The connection is established.
   * - ``MALFORMED_MESSAGE``
     - The first message failed to parse, or was not a ``connection_request``.
   * - ``TIMEOUT``
     - The ``connection_request`` did not arrive in time.
   * - ``WRONG_PROTOCOL_VERSION``
     - The server does not support the protocol version. See :ref:`communication-protocol-version`.
   * - ``DENIED``
     - The player, or a mod, denied the connection.

The ``message`` field describes a refusal. On success, ``protocol_version`` holds the version the
server accepted, and ``client_identifier`` holds 16 bytes identifying the client. The identifier is
the value that procedure ``KRPC.GetClientID`` returns.

The client specification states how a client connects, and how it reports a refused connection.
See :doc:`/client-spec/connection`.

.. _communication-protocol-disconnect:

Disconnecting
^^^^^^^^^^^^^

Either side ends a connection by sending a ``disconnect``:

.. literalinclude:: /communication-protocols/krpc.proto
   :language: protobuf
   :start-at: message Disconnect {
   :end-before: // Messages for connecting to the server

The reasons are:

.. list-table::
   :header-rows: 1

   * - Reason
     - Sent when
   * - ``CLOSED``
     - The client closes the connection.
   * - ``SERVER_STOPPED``
     - The server stops.
   * - ``REMOVED``
     - The player removes the client from the server window.
   * - ``MALFORMED_MESSAGE``
     - A message fails to parse.
   * - ``PROTOCOL_ERROR``
     - A message arrives in the wrong direction, or a response carries an id that matches no
       waiting or abandoned request.
   * - ``TIMEOUT``
     - A message stops arriving partway.

The reason and the ``message`` field are informational. The following Python code closes a
connection over TCP/IP:

.. literalinclude:: /scripts/communication-protocols/tcpip/RPC.py
   :start-at: # End the connection

The client specification states the closing sequence, and how a client handles a connection that
ends. See :doc:`/client-spec/connection`.

Invoking Remote Procedures
--------------------------

Remote procedures are grouped into *services*. Each service has a unique name, and each procedure
has a :ref:`unique name <communication-protocol-procedure-names>` within its service.

A client sends a ``Request`` holding one or more procedure calls. The server replies with a
``Response`` holding each call's return value or error.

Each request carries an ``id``, which the server copies onto the response to it. The procedure
calls in a request run in order, and the results in the response are in the same order as the calls.
The client specification states how a client numbers and matches requests. See
:doc:`/client-spec/calls`.

.. _communication-protocol-anatomy-of-a-request:

Anatomy of a Request
^^^^^^^^^^^^^^^^^^^^

A request is a ``Request`` message:

.. literalinclude:: /communication-protocols/krpc.proto
   :language: protobuf
   :start-at: message Request {
   :end-before: message Response {

The fields of a procedure call are:

* ``service`` - The name of the service that defines the procedure.

* ``procedure`` - The name of the procedure.

* ``service_id`` - The integer identifier of the service that defines the procedure.

* ``procedure_id`` - The integer identifier of the procedure.

* ``arguments`` - A list of ``Argument`` messages holding the values of the procedure's
  arguments. The fields of an argument are:

  * ``position`` - The zero-indexed position of the argument in the procedure's signature.

  * ``value`` - The value of the argument, :ref:`encoded <communication-protocol-protobuf-encoding>`
    according to the parameter's type. Ignored when ``is_null`` is set.

  * ``is_null`` - Whether the argument's value is null. When set, ``value`` is unset. Only
    permitted for parameters that accept null. See :ref:`why null has a flag of its own
    <communication-protocol-null-values>`.

A call names its service with ``service`` or ``service_id``, and its procedure with ``procedure``
or ``procedure_id``. The names are the same in every server version. The integer identifiers make
both the messages and the client code smaller, which suits a client on a microcontroller. They can
change between server versions.

A parameter can have a default value, listed in the procedure's
:ref:`description <communication-protocol-service-description-message>`. A call can leave out the
``Argument`` for such a parameter, and the procedure runs with the default. The ``position`` field
says which parameter each of the remaining arguments is for.

.. _communication-protocol-anatomy-of-a-response:

Anatomy of a Response
^^^^^^^^^^^^^^^^^^^^^

A response is a ``Response`` message:

.. literalinclude:: /communication-protocols/krpc.proto
   :language: protobuf
   :start-at: message Response {
   :end-before: // Messages for receiving stream updates

A response holds the id of the request it answers, and one result per call. The ``value`` field of
a result holds the procedure's return value,
:ref:`encoded <communication-protocol-protobuf-encoding>` according to its return type.

The ``is_null`` field marks a null return value. When it is set, ``value`` is unset. It is only
set for procedures whose return value is nullable. See :ref:`null values
<communication-protocol-null-values>`.

A call that fails sets the ``error`` field of its result. A request that fails as a whole sets the
``error`` field of the response, and the response holds no results.

The fields of an error are:

* ``service`` - The service that defines the exception type, when the error is one of the
  exceptions a service lists. Empty otherwise.

* ``name`` - The name of that exception type. Empty otherwise.

* ``description`` - A description of the error.

* ``stack_trace`` - The stack trace on the server. Empty unless the server's verbose errors
  setting is on.

Example Invocation
^^^^^^^^^^^^^^^^^^

The following Python code invokes ``KRPC.GetStatus`` over an open connection ``conn``:

.. literalinclude:: /scripts/communication-protocols/tcpip/RPC.py
   :start-at: # Invoke the KRPC.GetStatus RPC
   :end-before: # End the connection

``KRPC`` is the module the Protocol Buffers compiler generates from ``krpc.proto``, imported from
``krpc.schema.KRPC_pb2``. The ``send_message`` and ``recv_message`` helpers are defined earlier in
the script, shown under :doc:`tcpip`.

.. _communication-protocol-protobuf-encoding:

Protocol Buffers Encoding
^^^^^^^^^^^^^^^^^^^^^^^^^

An argument or a return value is encoded according to its
:ref:`type <communication-protocol-type>`, using the `Protocol Buffers encoding
<https://protobuf.dev/programming-guides/encoding/>`_ of a single value with no field tag:

.. list-table::
   :header-rows: 1

   * - Type code
     - Encoding
   * - ``DOUBLE``, ``FLOAT``
     - Little-endian IEEE 754, in 8 and 4 bytes.
   * - ``SINT32``, ``SINT64``
     - A zigzag encoded varint.
   * - ``UINT32``, ``UINT64``, ``BOOL``
     - A varint.
   * - ``STRING``
     - The length of its UTF-8 bytes as a varint, then the bytes.
   * - ``BYTES``
     - The length as a varint, then the bytes.
   * - ``ENUMERATION``
     - The value, encoded as a ``SINT32``.
   * - ``CLASS``
     - The object's identifier, encoded as a ``UINT64``. See
       :ref:`communication-protocol-proxy-objects`.
   * - ``STRUCT``
     - A ``Tuple`` message. See :ref:`communication-protocol-structures`.
   * - ``TUPLE``, ``LIST``, ``SET``, ``DICTIONARY``
     - The ``Tuple``, ``List``, ``Set`` or ``Dictionary`` message, with each item encoded according
       to its own type.
   * - ``EVENT``, ``PROCEDURE_CALL``, ``STREAM``, ``STATUS``, ``SERVICES``
     - The serialized message of the same name.

.. _communication-protocol-null-values:

Null Values
^^^^^^^^^^^

The ``is_null`` field on the ``Argument`` or ``ProcedureResult`` message marks a null value of any
type. When it is set, the ``value`` field is unset.

A separate flag lets every type carry null in the same way. A zero-length ``value`` cannot stand
for null, as it is already the encoding of an empty collection. Reserving a value of each type for
null would take that value away from the type.

A value nested inside another value has no flag of its own. A nullable list element, tuple item,
dictionary value or structure field carries a presence bool before its value instead. For example,
a ``LIST`` of nullable ``SINT32`` holding 42 and null::

   List.items[0] = 01 54         present, then the SINT32 42
   List.items[1] = 00            null

The bool is one byte, encoded as a ``BOOL`` value. The value's encoding follows it when the bool is
true. A position that is not nullable is encoded as its value alone, and a null in one is an error.

.. _communication-protocol-streams:

Streams
-------

A stream runs a remote procedure on every update, and sends its result to the client. The client
receives the values without a call for each one.

:ref:`communication-protocol-add-stream` adds a stream of a remote procedure, and
:ref:`communication-protocol-add-function-stream` adds a stream of a server side function. Both
return a ``Stream`` message, holding the stream's identifier:

.. literalinclude:: /communication-protocols/krpc.proto
   :language: protobuf
   :start-at: message Stream {
   :end-before: message Event {

An :ref:`event <communication-protocol-events>` is also a stream.

Each stream belongs to the client that created it. :ref:`communication-protocol-remove-stream`
removes a stream, and the server removes a client's streams when the client disconnects.
Removing a stream, or setting its rate, does nothing once the stream has ended. An identifier the
server never issued is an error.

A stream runs on every physics tick, unless :ref:`communication-protocol-set-stream-rate` gives it
a lower target rate. The results of a tick's streams are sent in one ``stream_update``. A stream's
first result is sent in the update after it is added. A later result that has not changed since
the last update is left out.

The client specification states how a client delivers stream values, and how updates are ordered
relative to responses. See :doc:`/client-spec/streams`.

Anatomy of a Stream Update
^^^^^^^^^^^^^^^^^^^^^^^^^^

A stream update is a ``StreamUpdate`` message:

.. literalinclude:: /communication-protocols/krpc.proto
   :language: protobuf
   :start-at: message StreamUpdate {
   :end-before: message StreamResult {

A ``StreamUpdate`` holds one ``StreamResult`` for each of the client's streams whose result
changed:

.. literalinclude:: /communication-protocols/krpc.proto
   :language: protobuf
   :start-at: message StreamResult {
   :end-before: // Messages for receiving information about the server

The fields are:

* ``id`` - The identifier of the stream, from the ``Stream`` message that added it.

* ``result`` - The result of the stream's procedure, as the same ``ProcedureResult`` a call to it
  returns. See :ref:`communication-protocol-anatomy-of-a-response`.

A result with its ``error`` field set is the last result for that stream. See
:ref:`streams-error-ends-the-stream <spec-streams-error-ends-the-stream>` in the client
specification.

.. _communication-protocol-events:

Events
------

An event is a stream of a ``bool``, and fires when the stream's value is true. An event is created
by :ref:`communication-protocol-add-event`, or by another procedure with an event return type. The
procedure returns an ``Event`` message, holding the ``Stream`` of the event. After its first
value, the stream holds only firings, as the server sends a value only when the event fires:

.. literalinclude:: /communication-protocols/krpc.proto
   :language: protobuf
   :start-at: message Event {
   :end-before: message Status {

.. _communication-protocol-krpc-service:

KRPC Service
------------

The server provides a core service called ``KRPC``. Its procedures give clients the basic
functionality they build on: the server's status, the definitions of its services, and streams and
server side functions. A client library calls these procedures for the program using it. They are
described below.

The service also has members for programs to use directly, such as the ``GameScene``, ``Paused``
and ``Clients`` properties and the ``HoldTick`` procedure. A program calls them through its client
library, like the members of any other service. They are listed in each client's API reference,
such as the :doc:`Python one </python/api/krpc/krpc>`.

GetStatus
^^^^^^^^^

The ``GetStatus`` procedure returns status information about the server, as a ``Status`` message:

.. literalinclude:: /communication-protocols/krpc.proto
   :language: protobuf
   :start-at: message Status {
   :end-at: uint32 protocol_version = 20;
   :append: }

The ``version`` field holds the version of the server, and ``protocol_version`` the protocol
version of the connection. The remaining fields hold performance statistics and the server's
configuration settings.

.. _communication-protocol-get-services:

GetServices
^^^^^^^^^^^

The ``GetServices`` procedure describes every service the server provides. A client can use it to
introspect the services, as the Python and Lua clients do to build their API when they connect. It
returns a ``Services`` message, described under
:ref:`communication-protocol-service-description-message`.

.. _communication-protocol-add-stream:

AddStream
^^^^^^^^^

The ``AddStream`` procedure adds a stream of a remote procedure. Its argument is the call, as a
``ProcedureCall`` message. See :ref:`communication-protocol-anatomy-of-a-request`. It returns a
``Stream`` message, described under :ref:`communication-protocol-streams`.

.. _communication-protocol-add-function-stream:

AddFunctionStream
^^^^^^^^^^^^^^^^^

The ``AddFunctionStream`` procedure adds a stream that evaluates a server side function on
each update. Its argument is the function, built using the ``KRPC.Expression`` class.

The procedure returns a ``Stream`` message. The stream's values have the type given by the
function's ``ReturnType`` property. The function must take no parameters. Any exception from the
function ends the stream with that error.

.. _communication-protocol-add-event:

AddEvent
^^^^^^^^

The ``AddEvent`` procedure adds an event that fires when a server side function evaluates to true.
Its argument is the function, built using the ``KRPC.Expression`` class. The function must take no
parameters and return a ``bool``.

The procedure returns an ``Event`` message. The function is evaluated on each stream update, and
the event fires on every update it returns true. Any exception from the function ends the event's
stream with that error.

.. _communication-protocol-run-function:

RunFunction
^^^^^^^^^^^

The ``RunFunction`` procedure evaluates a server side function once, within a single physics tick.
Its first argument is the function, built using the ``KRPC.Expression`` class.

The second argument is a list of the function's arguments, one per parameter of a function built
with ``Expression.Lambda``. Each is :ref:`encoded <communication-protocol-protobuf-encoding>`
according to the type given by the function's ``ParameterTypes`` property, and is null for a null
value. A function that is not a lambda takes no arguments, and the list defaults to empty.

The result is a ``bytes`` value, holding the function's value encoded according to its
``ReturnType``. A null value sets ``is_null``. A function with no value returns empty bytes, which
``HasReturnType`` tells apart from an empty collection.

The client specification states how a client builds and runs functions. See
:doc:`/client-spec/functions`.

.. _communication-protocol-compile-function:

CompileFunction
^^^^^^^^^^^^^^^

The ``CompileFunction`` procedure compiles a server side function in advance, and returns nothing.
This keeps the compile time of a large function out of its first run.

.. _communication-protocol-set-stream-rate:

SetStreamRate
^^^^^^^^^^^^^

The ``SetStreamRate`` procedure sets the target update rate of a stream, in hertz. Its arguments
are the identifier of the stream and the rate, as a ``float``. A rate of zero, the default, updates
the stream on every physics tick.

.. _communication-protocol-remove-stream:

RemoveStream
^^^^^^^^^^^^

The ``RemoveStream`` procedure removes a stream. Its argument is the stream's identifier, from
:ref:`communication-protocol-add-stream`, :ref:`communication-protocol-add-function-stream` or an
``Event`` message.

.. _communication-protocol-service-description-message:

Service Description Message
---------------------------

The :ref:`GetServices procedure <communication-protocol-get-services>` returns a ``Services``
message, holding a ``Service`` message for each service the server provides:

.. literalinclude:: /communication-protocols/krpc.proto
   :language: protobuf
   :start-at: message Services {
   :end-before: message Procedure {

The fields of a ``Service`` are:

* ``name`` - The name of the service.

* ``procedures`` - A list of ``Procedure`` messages, one for each procedure defined by the service.

* ``classes`` - A list of ``Class`` messages, one for each :cs:attr:`KRPCClass` defined by the
  service.

* ``enumerations`` - A list of ``Enumeration`` messages, one for each :cs:attr:`KRPCEnum`
  defined by the service.

* ``structs`` - A list of ``Struct`` messages, one for each :cs:attr:`KRPCStruct`
  defined by the service.

* ``exceptions`` - A list of ``Exception`` messages, one for each :cs:attr:`KRPCException`
  defined by the service.

* ``documentation`` - Documentation for the service.

* ``deprecated`` - Whether the service is deprecated.

* ``deprecated_reason`` - The reason for the deprecation.

The ``documentation`` field of each description message holds `C# XML documentation
<https://learn.microsoft.com/dotnet/csharp/language-reference/xmldoc/>`_, with the markup described
under :ref:`service-api-documentation`. A member marked ``deprecated`` still works, and its
``deprecated_reason`` may be empty.

.. seealso:: :ref:`extending`, for :cs:attr:`KRPCClass`, :cs:attr:`KRPCEnum`, :cs:attr:`KRPCStruct`
   and :cs:attr:`KRPCException`.

Procedures
^^^^^^^^^^

Details about a procedure are given by a ``Procedure`` message, with the format:

.. literalinclude:: /communication-protocols/krpc.proto
   :language: protobuf
   :start-at: message Procedure {
   :end-at: string deprecated_reason = 8;
   :append: }

.. literalinclude:: /communication-protocols/krpc.proto
   :language: protobuf
   :start-at: message Parameter {
   :end-before: message Class {

The fields are:

* ``name`` - The name of the procedure. See :ref:`communication-protocol-procedure-names` for more
  information.

* ``parameters`` - A list of ``Parameter`` messages containing details of the
  procedure's parameters, with the following fields:

  * ``name`` - The name of the parameter, to allow parameter passing by name.

  * ``type`` - The :ref:`type <communication-protocol-type>` of the parameter. Its ``nullable``
    field says whether null can be passed as the argument for this parameter.

  * ``has_default_value`` - Whether the parameter has a default value. When it is not set, the
    parameter is required and ``default_value`` and ``default_value_is_null`` are unset.

  * ``default_value`` - The default value of the parameter, :ref:`encoded
    <communication-protocol-protobuf-encoding>` according to the parameter's type. Set only when
    ``has_default_value`` is set and ``default_value_is_null`` is not. A zero-length value is a
    valid default, for example an empty collection.

  * ``default_value_is_null`` - Whether the default value is null. Set only when ``has_default_value``
    is set, in which case ``default_value`` is unset.

  The presence and nullability of the default value are given by these three fields as follows:

  .. list-table::
     :header-rows: 1

     * - Default value
       - ``has_default_value``
       - ``default_value_is_null``
       - ``default_value``
     * - none (required parameter)
       - unset
       - unset
       - unset
     * - a value (including an empty collection)
       - set
       - unset
       - encoded bytes (may be zero-length)
     * - null
       - set
       - set
       - unset

* ``return_type`` - The :ref:`return type <communication-protocol-type>` of the procedure. If the
  procedure does not return anything the field is unset, so its ``code`` reads as ``NONE``. Its
  ``nullable`` field says
  whether the procedure can return null.

* ``game_scenes`` - The :ref:`game scenes <communication-protocol-game-scene>` that the procedure is
  available in. If this repeated field is empty, the procedure is available in all game scenes.

* ``documentation`` - Documentation for the procedure.

* ``deprecated`` - Whether the procedure is deprecated.

* ``deprecated_reason`` - The reason for the deprecation.

Classes
^^^^^^^

Details about each :cs:attr:`KRPCClass` are specified in a ``Class`` message, with the format:

.. literalinclude:: /communication-protocols/krpc.proto
   :language: protobuf
   :start-at: message Class {
   :end-before: message Enumeration {

The fields are:

* ``name`` - The name of the class.

* ``documentation`` - Documentation for the class.

* ``deprecated`` - Whether the class is deprecated.

* ``deprecated_reason`` - The reason for the deprecation.

Enumerations
^^^^^^^^^^^^

Details about each :cs:attr:`KRPCEnum` are specified in an ``Enumeration`` message, with the
format:

.. literalinclude:: /communication-protocols/krpc.proto
   :language: protobuf
   :start-at: message Enumeration {
   :end-before: message Exception {

The fields are:

* ``name`` - The name of the enumeration.

* ``values`` - A list of ``EnumerationValue`` messages, indicating the values that the enumeration
  can be assigned. The fields are:

  * ``name`` - The name associated with the value for the enumeration.

  * ``value`` - The possible value for the enumeration as a 32-bit integer.

  * ``documentation`` - Documentation for the enumeration value.

  * ``deprecated`` - Whether the enumeration value is deprecated.

  * ``deprecated_reason`` - The reason for the deprecation.

* ``documentation`` - Documentation for the enumeration.

* ``deprecated`` - Whether the enumeration is deprecated.

* ``deprecated_reason`` - The reason for the deprecation.

.. _communication-protocol-structures:

Structures
^^^^^^^^^^

A structure is a compound value with named fields, sent as the values of its fields. Details about
each :cs:attr:`KRPCStruct` are specified in a ``Struct`` message, with the format:

.. literalinclude:: /communication-protocols/krpc.proto
   :language: protobuf
   :start-at: message Struct {
   :end-before: message Type {

The fields are:

* ``name`` - The name of the structure.

* ``fields`` - A list of ``StructField`` messages, one for each field of the structure, in the
  order their values are encoded in. The fields are:

  * ``name`` - The name of the field.

  * ``type`` - The type of the field, as a :ref:`Type message
    <communication-protocol-type>`. A field may have any type a parameter or return value may
    have, including a class, a collection or another structure. Its ``nullable`` field says
    whether the field can be null.

  * ``documentation`` - Documentation for the field.

  * ``deprecated`` - Whether the field is deprecated.

  * ``deprecated_reason`` - The reason for the deprecation.

* ``documentation`` - Documentation for the structure.

* ``deprecated`` - Whether the structure is deprecated.

* ``deprecated_reason`` - The reason for the deprecation.

A value of a structure type is encoded as a ``Tuple`` message whose items are the encoded values of
the structure's fields, in the order the ``fields`` list gives them. This is the encoding of a tuple
of the field types. The field names appear only in the definition above.

A nullable field carries a presence bool, as described under
:ref:`communication-protocol-null-values`. ``is_null`` on the enclosing ``Argument`` or
``ProcedureResult`` applies to the whole structure.

Exceptions
^^^^^^^^^^

Details about each :cs:attr:`KRPCException` are specified in an ``Exception`` message, with the
format:

.. literalinclude:: /communication-protocols/krpc.proto
   :language: protobuf
   :start-at: message Exception {
   :end-before: message Struct {

The fields are:

* ``name`` - The name of the exception type.

* ``documentation`` - Documentation for the exception.

* ``deprecated`` - Whether the exception is deprecated.

* ``deprecated_reason`` - The reason for the deprecation.

* ``mapped_type`` - The full name of the .NET exception the server reports as this exception, for
  example ``System.ArgumentException``. Empty if there is none.

.. _communication-protocol-procedure-names:

Procedure Names
---------------

Procedure names are CamelCase. Whether a procedure is a service procedure, class method, class
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

.. literalinclude:: /communication-protocols/krpc.proto
   :language: protobuf
   :start-at: message Type {
   :end-before: // Collection data structures

The ``code`` field specifies the type. ``NONE`` is the code of an unset type, such as the return
type of a procedure that returns no value.

For ``CLASS``, ``ENUMERATION`` and ``STRUCT`` types the ``service`` and ``name`` fields specify the
service that defines the class, enumeration or structure and its name. For all other types these
fields are empty.

For collection types the ``types`` repeated field will contain the sub-types:

* ``TUPLE`` types contain one or more types in the ``types`` field.
* ``LIST`` and ``SET`` types contain a single type in the ``types`` field.
* ``DICTIONARY`` types contain two types in the ``types`` field: the key type, then the value type.

For all other types the ``types`` field is empty. A ``STRUCT`` type carries only the name of its
structure, and its field types are in the :ref:`Struct message <communication-protocol-structures>`
of the service that defines it.

The ``nullable`` field says whether the position the type describes can hold null. It is a
property of that position rather than of the type, so the same type is nullable in one place and
not in another. The positions that can be nullable are a parameter, a return value, a
:ref:`structure field <communication-protocol-structures>`, a list element, a tuple item and a
dictionary value. A dictionary key and a set element cannot.

How the null itself is carried depends on the position. An argument, a result and a default value
each have a flag of their own, ``Argument.is_null``, ``ProcedureResult.is_null`` and
``Parameter.default_value_is_null``. A value nested inside another value has no such flag, and
carries a leading presence bool instead, as described under
:ref:`communication-protocol-null-values`.

.. _communication-protocol-game-scene:

GameScene
---------

A procedure's ``game_scenes`` field lists the ``GameScene`` values it is available in. An empty
list means every scene:

.. literalinclude:: /communication-protocols/krpc.proto
   :language: protobuf
   :start-at: enum GameScene {
   :end-at: };
   :dedent:

``ASTRONAUT_COMPLEX`` through ``ADMINISTRATION`` each name a facility open within ``SPACE_CENTER``.

.. _communication-protocol-proxy-objects:

Proxy Objects
-------------

kRPC allows procedures to create objects on the server, and pass a unique identifier for them to the
client. This allows the client to create a *proxy* object for the actual object, whose methods and
properties make remote procedure calls to the server. Object identifiers have type ``uint64``.

When a procedure returns a proxy object or takes one as a parameter, the type code is ``CLASS``.

The ``is_null`` field of the ``ProcedureResult`` or ``Argument`` message signals a null object
reference, as it does a null value of any other type. The object identifier is never used to
represent null on the wire.
