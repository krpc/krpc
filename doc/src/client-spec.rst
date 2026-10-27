Client Specification
====================

This specification states what a kRPC client library does on top of the
:doc:`messaging protocol <communication-protocols/messages>`. It covers the behavior a program
sees, such as how a client reports a refused connection, matches responses to calls and delivers
stream values.

The requirements are generated from the :doc:`conformance tests <client-spec/conformance>`. Each
requirement has a test that checks it, and the text and the test are one source. A change to a
requirement is a change to its test, so the two cannot drift apart.

This specification is versioned with kRPC. The conformance tests in the ``krpctools`` package of
a release check the specification of that release.

Requirements
------------

Each requirement has an id of the form ``<area>-<name>``, such as ``streams-wait``. The area is one
page of this specification:

.. list-table::
   :header-rows: 1

   * - Area
     - Covers
   * - :doc:`connection <client-spec/connection>`
     - Opening, reading and closing a connection
   * - :doc:`calls <client-spec/calls>`
     - Calling the members of a service, passing arguments, and matching responses to calls
   * - :doc:`values <client-spec/values>`
     - The values passed as arguments and read back as results
   * - :doc:`codec <client-spec/codec>`
     - Encoding and decoding values
   * - :doc:`errors <client-spec/errors>`
     - The errors reported for errors returned by the server
   * - :doc:`objects <client-spec/objects>`
     - The objects held for instances of a server's classes
   * - :doc:`streams <client-spec/streams>`
     - Adding streams, reading their values and removing them
   * - :doc:`events <client-spec/events>`
     - Waiting on events and calling back on them
   * - :doc:`threading <client-spec/threading>`
     - Using one connection from several threads
   * - :doc:`functions <client-spec/functions>`
     - Streaming, running and waiting on server side functions

The conformance test for a requirement is named after it. The test for ``streams-wait`` is
``test_wait`` in ``test_streams.py``. The text of a requirement is the docstring of its test.
The results of the clients in this repository are listed under :doc:`client-spec/coverage`.

The key words MUST and SHOULD are used as described in `RFC 2119
<https://www.rfc-editor.org/rfc/rfc2119>`_. A requirement marked SHOULD holds unless a client has a
reason recorded in its documentation.

.. _client-spec-profiles:

Profiles
--------

Each requirement is listed with the profiles a client needs to run its test, then its level:

.. code-block:: none

   connection-serialio-leftovers [serialio] [MUST]

A requirement marked MUST holds in every client that implements its profiles. A requirement listed
``without`` a profile applies only to the clients that do not implement it:

.. code-block:: none

   errors-message-prefix [core, without error-types] [MUST]

A client implements the ``core`` profile, and any of the others:

.. list-table::
   :header-rows: 1

   * - Profile
     - Covers
   * - ``core``
     - Connecting, calls, values, errors and objects
   * - ``error-types``
     - Reporting each exception a service lists with a type of its own
   * - ``dynamic``
     - Building the stubs at run time, and rejecting wrong arguments, in a dynamically typed language
   * - ``defaults``
     - Omitting arguments that have defaults
   * - ``named-arguments``
     - Passing arguments by name at run time
   * - ``streams``
     - Adding streams, reading and waiting on their values, and removing them
   * - ``callbacks``
     - Calling back on stream updates, firings and stream ends
   * - ``conditions``
     - Holding the condition of a stream or event to read and wait on it
   * - ``events``
     - Events
   * - ``functions``
     - Streams, events and runs of server side functions
   * - ``function-types``
     - Checking the type a program names for the value of a server side function
   * - ``connections``
     - Several connections open at once, sharing objects
   * - ``threads``
     - Concurrent calls, and callbacks and closes from other threads
   * - ``ordering``
     - Ordering objects, in a language that orders values
   * - ``races``
     - Forcing races between the update thread and the program, by pausing a thread of the client
   * - ``tcpip``, ``localsocket``, ``serialio``, ``websockets``
     - The :doc:`communication protocols <communication-protocols>`

.. toctree::
   :hidden:

   client-spec/conformance
   client-spec/connection
   client-spec/calls
   client-spec/values
   client-spec/codec
   client-spec/errors
   client-spec/objects
   client-spec/streams
   client-spec/events
   client-spec/threading
   client-spec/functions
   client-spec/coverage
