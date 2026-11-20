Client Coverage
===============

.. role:: coverage-pass
   :class: coverage-pass

.. role:: coverage-fail
   :class: coverage-fail

.. role:: coverage-partial
   :class: coverage-partial

.. role:: coverage-untested
   :class: coverage-untested

The conformance tests run against each client in this repository. Each table below gives the
result of a client on each requirement of a page:

* :coverage-pass:`✓` The client passes the test.
* :coverage-fail:`✗` The client fails the test, and the failure is recorded as a known defect.
* :coverage-partial:`!` The client fails some of the test's cases, or fails it over some
  protocols.
* :coverage-untested:`?` The requirement has no test.
* A blank cell means the requirement does not apply to the client. It belongs to a profile the
  client does not implement, or the client lists its test as unsupported.

.. include:: requirements/coverage.txt
