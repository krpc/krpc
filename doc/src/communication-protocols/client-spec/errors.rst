Errors
======

This page covers the errors a client reports for an error returned by the server. The error a
response carries is described under :ref:`communication-protocol-anatomy-of-a-response`.

A client reports an error by the mechanism of its language, such as an exception, a status code or
a result type. Each error is of a kind, such as the RPC error for a server error or the
encoding error for a value, and carries a message.

.. include:: requirements/errors.txt
