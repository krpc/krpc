.. default-domain:: http
.. highlight:: json

HTTP
====

This guide describes how to call the server over HTTP, and the features it provides. The
:ref:`http-client-reference` gives the path of each procedure.

Each procedure has a URL, with arguments and results in JSON. You can call it from curl, a web
page or any language that sends HTTP requests:

.. code-block:: bash

   curl http://127.0.0.1:50003/space-center/active-vessel
   # {"value": 1}

   curl http://127.0.0.1:50003/space-center/vessel/1/name
   # {"value": "Kerbal X"}

   curl http://127.0.0.1:50003/space-center/vessel/1/control
   # {"value": 2}

   curl -X PUT http://127.0.0.1:50003/space-center/control/2/throttle \
       -H 'Content-Type: application/json' -d '{"value": 0.5}'
   # {}

   curl -X POST \
       http://127.0.0.1:50003/space-center/control/2/activate-next-stage
   # {"value": [3, 4]}

Each request is one call. Streams, events and server side functions are not supported over HTTP.

Connecting to the Server
------------------------

Add a server in the server window, and select *JSON over HTTP* as its protocol. It listens on
address ``127.0.0.1`` and port ``50003`` by default.

HTTP requests ignore the *Auto-accept new clients* setting, and always run.

The server runs the calls one at a time, in the order the requests arrive. An HTTP/1.1
connection stays open for further requests unless a request sends ``Connection: close``.

State Between Requests
----------------------

An HTTP request holds no state, in the style of a REST API. The server holds the state that
requests create until the server stops, so state carries over from one request to the next. An
auto-pilot that one request engages stays engaged until a request disengages it, and a line one
request draws stays until a request removes it.

The other clients instead hold a persistent connection to the server. The connection holds
server-side state, such as the game objects the client creates. The server releases that state
when the client disconnects. An engaged auto-pilot disengages, and the client's drawings and UI
are removed.

Paths
-----

The :ref:`routing table <http-routingtable>` lists every path, and the :ref:`OpenAPI document
<http-openapi>` describes each one for tools to read. ``GET /`` returns an empty response with
status 200, as a health check.

The :ref:`http-client-reference` gives the path of each property and method. The request method
depends on the kind of member:

.. list-table::
   :header-rows: 1

   * - Call
     - Request
   * - Reading a property
     - ``GET /space-center/active-vessel``
   * - Setting a property
     - ``PUT /space-center/active-vessel``
   * - Calling a method
     - ``POST /space-center/launch-vessel``
   * - Reading an object's property
     - ``GET /space-center/vessel/<id>/name``
   * - Setting an object's property
     - ``PUT /space-center/vessel/<id>/name``
   * - Calling an object's method
     - ``POST /space-center/vessel/<id>/recover``
   * - Calling a static method
     - ``POST /space-center/alarm-manager/add-alarm``

``<id>`` is the object's identifier.

A request with the wrong method for its path gets status 405, Method Not Allowed. The response's
``Allow`` header lists the methods the path takes. For example,
``POST /space-center/active-vessel`` gets ``Allow: GET, PUT``, as the path is a property. A
``GET`` request only ever reads a property, and never changes the game.

Requests
--------

A ``GET`` request reads a property, and has no body. A ``PUT`` request sets the property to the
``value`` in its body. A ``POST`` request passes the procedure's arguments as a JSON object, keyed
by the parameter names:

.. code-block:: sh

   curl -X POST http://127.0.0.1:50003/space-center/launch-vessel \
     -H 'Content-Type: application/json' \
     -d '{"craft_directory": "VAB", "name": "Kerbal X",
          "launch_site": "LaunchPad"}'

A parameter with a default value can be left out. A body needs the header
``Content-Type: application/json``. A ``POST`` with no arguments needs no body.

Responses
---------

A call returns status 200, and a JSON object with its result as ``value``. A procedure that
returns nothing gives an empty object. A failed call returns an ``error`` object, with a
description of the error and the stack trace of the exception:

.. code-block:: json

   {"error": {"service": "KRPC", "name": "ArgumentException",
              "description": "...", "stack_trace": "..."}}

``service`` and ``name`` name the exception, and are empty when the error has no exception type.
The status is:

.. list-table::
   :header-rows: 1

   * - Status
     - Error
   * - 400
     - A malformed request, a missing or invalid argument, or an argument exception from the
       procedure.
   * - 403
     - A request from a web page whose origin the server does not allow, or for another host name.
       See `Allowed Origins`_.
   * - 404
     - The path does not map to a procedure.
   * - 405
     - The path does not accept the HTTP method, such as a ``POST`` to a property.
   * - 413
     - A body larger than 16 MiB.
   * - 415
     - A body without the JSON content type.
   * - 431
     - A header larger than 16 KiB.
   * - 500
     - Any other exception from the procedure, or a failure to handle the request.
   * - 501
     - A request with ``Transfer-Encoding``. Send ``Content-Length`` instead.
   * - 505
     - An HTTP version other than 1.0 or 1.1.

Values
------

Arguments and results are encoded as JSON values, based on the type in the procedure's definition:

.. list-table::
   :header-rows: 1

   * - Type
     - JSON
   * - Integers
     - A number. In JavaScript, a 64-bit integer above :math:`2^{53}` loses precision.
   * - ``float``, ``double``
     - A number, or the string ``"NaN"``, ``"Infinity"`` or ``"-Infinity"``.
   * - ``bool``
     - ``true`` or ``false``.
   * - ``string``
     - A string.
   * - ``bytes``
     - A base64 string.
   * - Enumeration
     - The value's name, e.g. ``"stability_assist"``. A flags enumeration is a list of names.
   * - Class
     - The object's identifier, a number.
   * - Structure
     - An object, with a field for each of the structure's fields.
   * - Tuple, list, set
     - A list.
   * - Dictionary
     - An object for a dictionary with string keys, otherwise a list of ``[key, value]`` pairs.
   * - Nullable
     - The value, or ``null``.

.. _http-openapi:

OpenAPI
-------

``GET /openapi.json`` returns an `OpenAPI <https://www.openapis.org>`_ 3.0 document describing every
path, with the types of its parameters and results:

.. code-block:: sh

   curl -o krpc.json http://127.0.0.1:50003/openapi.json

The document covers the services of the server it comes from, including those of the mods
installed in the game. Its server URL is the address it was fetched from, so tools send their
calls there.

Tools that read OpenAPI documents work with it:

* Postman and Insomnia import it as a collection of requests.
* Swagger UI lists the paths and tries out calls, once the server allows its origin, as
  `Allowed Origins`_ describes.
* `OpenAPI Generator <https://openapi-generator.tech>`_ generates a client from it, in many
  languages:

  .. code-block:: sh

     openapi-generator generate -i krpc.json -g go -o krpc-go

.. _http-allowed-origins:

Allowed Origins
---------------

A web page on another site can call the server only if the server allows the page's origin. To
allow origins, edit the HTTP server in the server window, and set *Origins*. It is a
comma-separated list of origins, each a scheme, a host and an optional port, or ``*`` for any
origin. It is empty by default, which allows the server's own origin only.

A response to a page from an allowed origin carries the CORS headers the browser needs. A request
from any other origin gets status 403. The check applies to web pages only, as curl and other
tools send no ``Origin`` header.

A request gets status 403 unless its ``Host`` header names ``localhost``, an IP address or the
name of the machine running the game. See :ref:`http-security` for more details.

For example, this runs Swagger UI on ``localhost:8080``, to browse the paths and try out calls:

.. code-block:: sh

   docker run -p 8080:8080 \
     -e URL=http://127.0.0.1:50003/openapi.json swaggerapi/swagger-ui

Its page calls the server once *Origins* holds ``http://localhost:8080``.

.. note:: A page opened from a file has the origin ``null``, which only ``*`` allows. Serve the
          page from a local web server instead, and add its origin.

.. note:: A page from a public site, such as ``editor.swagger.io``, also needs your browser's
          permission to reach a local address. Chrome asks for it the first time.

.. _http-client-reference:

Service API Reference
---------------------

.. include:: http/api.inc
