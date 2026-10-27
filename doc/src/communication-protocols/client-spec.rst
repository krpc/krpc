Client Specification
====================

This specification states what a kRPC client library does on top of the
:doc:`messaging protocol <messages>`. It covers the behavior a program sees, such as how a client
reports a refused connection, matches responses to calls and delivers stream values.

The key words MUST and SHOULD are used as described in `RFC 2119
<https://www.rfc-editor.org/rfc/rfc2119>`_. A requirement marked MUST holds in every client that
implements its profile. A requirement marked SHOULD holds unless a client has a reason recorded in
its documentation.

Requirements
------------

Each requirement has an id of the form ``<area>-<name>``, such as ``streams-wait``. The area is one
page of this specification. An id is never reused or renamed, so a dropped requirement leaves a gap.

The conformance test for a requirement is named after it. The test for ``streams-wait`` is
``test_wait`` in ``test_streams.py``. The requirement lists on these pages are generated from the
tests, with each test's docstring as the text of its requirement.

.. _client-spec-profiles:

Profiles
--------

A client implements the ``core`` profile, and any of the others:

.. list-table::
   :header-rows: 1

   * - Profile
     - Covers
   * - ``core``
     - Connecting, calls, values, errors and objects
   * - ``dynamic``
     - Rejecting wrong arguments, in a dynamically typed language
   * - ``defaults``
     - Omitting arguments that have defaults
   * - ``named-args``
     - Passing arguments by name at run time
   * - ``streams``
     - Streams
   * - ``events``
     - Events
   * - ``functions``
     - Streams, events and runs of server side functions
   * - ``threads``
     - Concurrent calls, and callbacks and closes from other threads
   * - ``first-call-on-add``
     - Making the first call of a callback before adding it returns
   * - ``tcpip``, ``localsocket``, ``serialio``
     - The transports

A requirement belongs to the ``core`` profile unless its page or its text names another.

.. toctree::
   :maxdepth: 1

   client-spec/connection
   client-spec/calls
   client-spec/values
   client-spec/codec
   client-spec/errors
   client-spec/objects
   client-spec/coverage
   client-spec/conformance
