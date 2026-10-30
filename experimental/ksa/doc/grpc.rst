gRPC
====

kRPC includes a `gRPC <https://grpc.io>`_ server. It lets you control the game from any
language gRPC supports, with a client generated from ``.proto`` files.

.. note::

   Where your language has a :doc:`kRPC client <clients>`, use it instead. The kRPC clients
   support streams and events, and have a more concise syntax.

Configuring the Server
----------------------

The gRPC server is not enabled by default. To add it in the server window:

1. Open the server window from **kRPC > Server** in the game's menu bar at the top.
2. Click **Add server**, then **Edit** on the new server.
3. Set **Protocol** to **gRPC**, and the **Port**, by default 50002. Click **Save**.
4. Start the servers with **Start servers**.

The server auto-accepts a new client as the other servers do. With **Auto-accept new clients** turned
off, the game asks before a client's first call runs.

Generating a Client
-------------------

The ``stubs/grpc`` folder of the release archive holds a ``.proto`` file for each kRPC service.
Generate a client from them with ``protoc`` and the gRPC plugin for your language.

The `gRPC documentation <https://grpc.io/docs/languages/>`_ has a quick start and a basics
tutorial for each supported language, covering code generation, channels and calls.

For Python, with the ``KSA`` kRPC service:

.. code-block:: bash

   pip install grpcio grpcio-tools
   python -m grpc_tools.protoc -I stubs/grpc --python_out=. --grpc_python_out=. \
       stubs/grpc/KSA.proto stubs/grpc/StdLib.proto

This Python script uses the generated client to print the name of the active vehicle and the
universal time:

.. literalinclude:: /scripts/grpc/ActiveVehicle.py
   :language: python

Names
-----

A gRPC client calls *methods*, which are grouped into gRPC *services*. Each method takes one
request message and returns one response message. The ``.proto`` file of a kRPC service lays
out its procedures this way:

* Each class becomes a gRPC service named after it, such as ``VehicleService``.
* Each procedure becomes a method. A property becomes a ``Get`` method, and a ``Set`` method if
  it can be set.
* The arguments are fields of the request message, and the return value is the ``value`` field
  of the response. For a method on an object, the object is the request's ``this`` field.

In the script above, this call reads the ``Vehicle.Name`` property:

.. code-block:: python

   vehicles.GetName(ksa.Vehicle.GetNameRequest(this=vehicle))

Each kind of procedure maps as follows. Each request message has a matching response message,
such as ``Vehicle.GetNameResponse``.

.. list-table::
   :header-rows: 1

   * - Kind
     - kRPC
     - gRPC method
     - gRPC request message
   * - Procedure
     - ``KSA.WarpTo``
     - ``KSAService.WarpTo``
     - ``KSA.WarpToRequest``
   * - Property
     - ``KSA.UT``
     - ``KSAService.GetUT``
     - ``KSA.GetUTRequest``
   * - Class property getter
     - ``Vehicle.Name``
     - ``VehicleService.GetName``
     - ``Vehicle.GetNameRequest``
   * - Class property setter
     - ``Vehicle.Name``
     - ``VehicleService.SetName``
     - ``Vehicle.SetNameRequest``
   * - Class method
     - ``Vehicle.Position``
     - ``VehicleService.Position``
     - ``Vehicle.PositionRequest``
   * - Static class method
     - ``UI.Clear``
     - ``UIService.Clear``
     - ``UI.ClearRequest``

Types
-----

The values in the messages have these forms:

.. list-table::
   :header-rows: 1

   * - kRPC
     - gRPC
   * - Parameter
     - Field in ``snake_case``, such as ``reference_frame``
   * - Parameter with a default value
     - Field left out to use the default
   * - Object
     - Message holding the id, such as ``Vehicle { uint64 id = 1; }``, with id 0 for ``null``
   * - Tuple
     - Message with fields ``item1``, ``item2`` and so on
   * - List and set
     - ``repeated`` field
   * - Dictionary
     - ``map`` field
   * - Enumeration value
     - Prefixed with the enumeration name, such as ``AUTO_PILOT_MODE_HOLD``

Features
--------

The gRPC server provides procedures as unary calls:

* **Calls only.** Streams and events have no gRPC form, so poll a value to follow it.
* **One client per connection.** Each HTTP/2 connection is one kRPC client, named by the
  ``krpc-client-name`` metadata on its calls. The client connects with its first call, and the
  server list shows it like any other.
* **Calls run in game.** A call that waits, such as ``WarpTo``, holds up only the calls of its
  own connection. The game finishes a call even after the client cancels it or its deadline
  passes, and drops the response.
* **Errors are status codes.** An argument error is ``INVALID_ARGUMENT``, an invalid operation
  is ``FAILED_PRECONDITION`` and any other error is ``UNKNOWN``. The ``krpc-error-name`` and
  ``krpc-error-service`` trailers name the exception, as the other clients raise it.
