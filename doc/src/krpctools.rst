krpctools
=========

`krpctools <https://pypi.org/project/krpctools>`_ is a Python package of command line tools for
developing services and client libraries. Install it with pip:

.. code-block:: sh

   pip install krpctools

Its version matches the kRPC release it comes from. ``krpc-servicedefs``, ``krpc-testserver`` and
``krpc-conformance`` run on Linux and Windows, and need the
`.NET 10 runtime <https://dotnet.microsoft.com/download/dotnet/10.0>`_. Every tool takes
``--help``.

Tools
-----

.. list-table::
   :header-rows: 1

   * - Tool
     - Purpose
     - Described in
   * - ``krpc-servicedefs``
     - Generates the service definitions, a JSON file, from a service's assemblies.
     - :ref:`service-api-clientgen`
   * - ``krpc-clientgen``
     - Generates the stubs of a service for the clients that need them.
     - :ref:`service-api-clientgen`
   * - ``krpc-docgen``
     - Generates the API reference pages of a service.
     - :doc:`extending/docgen`
   * - ``krpc-testserver``
     - Runs a kRPC server without the game, for testing a client.
     - :doc:`client-spec/testserver`
   * - ``krpc-conformance``
     - Runs the conformance tests against a client library.
     - :doc:`client-spec/conformance`
