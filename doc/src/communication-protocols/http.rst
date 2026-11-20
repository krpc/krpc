HTTP
====

This protocol takes each call as an HTTP request, with its arguments and result in JSON. Each
procedure has a path, such as ``GET /space-center/active-vessel``.

The :doc:`HTTP guide </http>` describes how to use it, and its :ref:`http-client-reference` gives
the path of each procedure. This page gives the rationale behind the design.

One Session
-----------

A request carries no state, but the server keeps state per client, such as an engaged auto-pilot
or the objects a client created. The HTTP server therefore runs every request as a call on one
internal client. The client connects when the server starts and disconnects when it stops, so its
state persists from one request to the next. The server window leaves it out of its client list,
though ``KRPC.Clients`` includes it, under the name ``HTTP``.

Object identifiers stay valid from one request to the next, as the server keeps one object store
for all of its clients. A prompt for each request would make HTTP unusable, so HTTP requests
ignore the *Auto-accept new clients* setting. Starting the server approves its session.

Requests from every caller run one at a time, in the order they arrive, as the server runs one
call of a client at a time.

Methods and Paths
-----------------

``GET`` calls only a property getter, so a link preview, a browser's prefetch or a dashboard
never changes the game. A getter takes no argument besides its object, so a ``GET`` needs no
query string. ``PUT`` calls a setter, and ``POST`` any other procedure.

Paths are in kebab case, the usual form in a URL. JSON keys are in snake case, as JavaScript
cannot write a kebab case key as ``obj.key``, and ``jq`` cannot write it as ``.key``.

An object's identifier is a segment of the path, so the path of a class member cannot match the
path of a static method.

``GET /`` returns an empty response, as a health check. The paths are listed in the OpenAPI
document at ``GET /openapi.json``, in the form tools read.

Values
------

Integers are JSON numbers, 64-bit ones included. JavaScript parses a number as a double, so a
64-bit value above :math:`2^{53}` loses precision there. A second form as a string would give one
value two encodings, for a range few values reach. A caller that needs an exact value can parse
the number with a library such as ``lossless-json``, as the server writes every digit.

An enumeration value is its name, which reads better in a request than a number. Every value of
an enumeration has a name, so a number is never needed.

Supported Calls
---------------

Streams and events have no paths, as each request gets one response. Server side functions have
none either. A function runs through ``RunFunction``, an internal procedure. A function built over
HTTP therefore has no way to run, and the ``Expression`` and ``Type`` classes have no paths.

.. _http-security:

Security
--------

Any web page can send a request to ``127.0.0.1``. A ``POST`` with no body needs no CORS preflight.
A page on another site could then call ``activate-next-stage``, even with no access to the result.
A request from an origin the server does not allow gets status 403, as the guide's
:ref:`http-allowed-origins` section describes.

A page opened from a file has the origin ``null``. So does a page in a sandboxed frame, which any
site can create. Only ``*`` allows ``null``, as allowing it would allow every site.

DNS rebinding points the name of the page's own site at ``127.0.0.1``. Its requests are then
same-origin, and pass the ``Origin`` check. A request whose ``Host`` header names another machine
gets status 403. The attacker's name is never the machine's own, so this check stops rebinding.

The server takes a body only with ``Content-Type: application/json``. A cross-site request with
that content type always sends a preflight first.

The server reads a whole request into memory before it runs the call. A header over 16 KiB or a
body over 16 MiB gets an error, so one request cannot make the game allocate unbounded memory.
