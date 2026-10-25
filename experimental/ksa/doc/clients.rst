Clients
=======

kRPC for KSA works with the kRPC @KRPC_VERSION@ client libraries. Each client has a guide to
installing and using it with the ``KSA`` service, and a reference for every procedure the
service provides.

.. toctree::
   :maxdepth: 1

   python
   csharp
   cpp
   cnano
   java
   lua

Python and Lua build the ``KSA`` service when they connect. The C#, C++, C-nano and Java
clients compile against the stubs in ``krpc-ksa-stubs-@VERSION@.zip``. The archive also holds
``KRPC.KSA.json``, the service definitions, from which ``krpc-clientgen`` in the ``krpctools``
package generates stubs for another client version.
