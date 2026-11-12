Errors
======

This page covers the errors a client reports for an error returned by the server. The error a
response carries is described under :ref:`communication-protocol-anatomy-of-a-response`.

A client reports an error by the mechanism of its language, such as an exception, a status code or
a result type. Each error carries a message, and is of one of these kinds, which the requirements
name throughout:

* The connection error, for a connection that cannot be made, has failed or has closed.
* The server error, for an error the server returns, through a call, a stream or an event.
* The stream error, for a stream or event that has been removed or cannot be added.
* The encoding error, for a value of the right type that fails to encode, or a value that fails
  to decode.
* The argument error, the language's own, for an argument the program passes that the client
  rejects.
* The interrupt error, the language's own, for a thread interrupted in a call or a wait.

In a client with error types, the connection, server, stream and encoding errors derive one type
of the client's own, its remote error type. A server exception with a mapped type raises the
language's own type for it, which does not derive the remote error type.

An error that another error caused carries the cause's message after its own.

.. include:: requirements/errors.txt
