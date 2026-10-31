Errors
======

This page covers the errors a client reports for an error returned by the server. The error a
response carries is described under :ref:`communication-protocol-anatomy-of-a-response`.

A client reports an error by the mechanism of its language, such as an exception, a status code or
a result type. Each error carries a message, and is of one of these kinds, which the requirements
name throughout:

* The connection error, for a connection that cannot be made, has failed or has closed.
* The RPC error, for an error the server returns for a call.
* The stream error, for a stream or event that has been removed or cannot be added.
* The encoding error, for a value that fails to encode or decode.
* The argument error, for an argument the program passes that the client rejects.

.. include:: requirements/errors.txt
