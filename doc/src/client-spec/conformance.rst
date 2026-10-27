Conformance Tests
=================

The conformance tests check a client library against this specification. A driver, written in
Python, runs the tests and talks to a small program built on the client library, called the
testee. The testee turns each command it reads into a call on the client, and writes back the
result. To test your own client, write a testee for it.

Running the Tests
-----------------

The driver ships in the `krpctools <https://pypi.org/project/krpctools>`_ Python package, with a
TestServer to run the tests against. Install it, then run the tests against your testee:

.. code-block:: sh

   pip install krpctools
   krpc-conformance --testee "./my-testee --some-option" \
       --expected-failures my-client-failures.txt

The TestServer runs on Linux, and needs the
`.NET 10 runtime <https://dotnet.microsoft.com/download/dotnet/10.0>`_. The tests over serial IO
also need ``socat``.

The driver starts the TestServer for each run, and ``--server`` names a different one.
``--protocol`` picks the :doc:`communication protocol </communication-protocols>` to test
over: ``tcpip`` (the default), ``localsocket``, ``serialio`` or ``websockets``. Every test runs
over each protocol, except over ``serialio``, which has no streams or events and takes one
client. Any other option is passed to ``pytest``, such as ``-k`` to select tests.

The Testee
----------

The driver starts the testee, and writes one command per line to its standard input. The testee
writes its replies to standard output. Here the testee writes its ``Hello``, then the driver
connects to a server and calls a procedure:

.. code-block:: none

   < {"hello":{"client":"python","profiles":["core","streams"],"protocols":["tcpip"]}}
   > {"id":"1","connect":{"protocol":"tcpip","address":"127.0.0.1","port":50000}}
   < {"reply":{"id":"1","value":{},"thread":"140213"}}
   > {"id":"2","invoke":{"service":"TestService","procedure":"Int32ToString","arguments":[{"sint32":42}]}}
   < {"reply":{"id":"2","value":{"string":"42"},"thread":"140213"}}

Each line is a message in the proto3 JSON mapping: a ``Command`` from the driver, and an
``Output`` from the testee. :download:`testee.proto </client-spec/testee.proto>` defines the
messages, and is the contract between the driver and a testee. Its comments state what the testee
does for each command. The file also ships in the ``krpctools`` package, as
``krpctools/conformance/testee.proto``.

* The testee writes a ``Hello`` before it reads any command. It names the client, and the
  :ref:`profiles <client-spec-profiles>` and protocols the client implements. The driver skips a
  test that needs a profile the testee does not report.
* Each command gets one ``Reply`` with the command's id. The reply holds a ``Value``, a stream
  handle, or an ``Error`` for the error the client raised.
* A ``Value`` holds a kRPC value without using the client's encoder. The testee converts it to the
  language's own value, so a bug in the client's encoder cannot cancel out a bug in its decoder.
* A callback the driver adds writes a ``Notification`` for each of its calls. A notification can
  arrive between a command and its reply.
* A testee with the ``threads`` profile runs commands on worker threads, named in the command. A
  testee without it runs every command in order on one thread.
* A command the client offers no way to carry out gets an ``Error`` of kind ``UNSUPPORTED``.

The testee exits when its standard input closes.

Expected Failures
-----------------

A client can list the tests it fails in an expected failures file, one test per line, with an
optional comment:

.. code-block:: none

   test_errors.py::test_exception_name  # RPC errors carry only a message
   test_streams.py::test_wait_timeout_values[nan]

A listed test must fail, or the run fails, so the list cannot go stale. A test of a timing
requirement is the exception, as it can pass by chance. A parametrized test can be listed as a
whole, or by its cases.

A test name ending in ``@`` and a protocol is listed only over that protocol. An ``@flaky`` after
the test name marks a test the client fails only now and then, which may pass:

.. code-block:: none

   test_connection.py::test_lost[partway]@serialio @flaky  # Raises an encoding error, except now and then

An ``@skip`` after the test name marks a test that hangs the testee. The test is not run:

.. code-block:: none

   test_threading.py::test_wait_holding_another @skip  # The update is never announced

Unsupported Tests
-----------------

A client lists the tests its language has no way to carry out in an unsupported file, passed with
``--unsupported``. It has the same form as the expected failures file:

.. code-block:: none

   test_objects.py::test_hash  # The language has no hash of an object
   test_calls.py::test_abandoned_id  # A thread cannot be interrupted

A listed test is skipped when the testee replies with an ``UNSUPPORTED`` error. An ``UNSUPPORTED``
reply in a test that is not listed fails the test, as does a listed test that runs. A limitation
of the testee belongs in the expected failures file.

Conventions
-----------

A client with the ``error-types`` or ``dynamic`` profile describes its language's conventions in
a JSON file, passed with ``--conventions``:

.. code-block:: json

   {
     "native_types": {
       "System.ArgumentException": "ValueError",
       "System.InvalidOperationException": "RuntimeError"
     },
     "naming": {"case": "snake", "accessors": false, "exceptions": true}
   }

``native_types`` is for the ``error-types`` profile. It gives the native error type the client
raises for each mapped exception, by the exception's mapped type, and needs an entry for each
one. A ``null`` entry is an exception the client raises only as a type of its own. ``naming`` is for the ``dynamic`` profile. It gives the case of member names, whether a
property is a pair of ``get_`` and ``set_`` functions, and whether the client exposes exception
types. The case is ``snake``, the one case the driver converts names to. The run stops if the
file lacks a section a profile needs.
