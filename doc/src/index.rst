kRPC Documentation
==================

kRPC allows you to control Kerbal Space Program from scripts running outside of
the game.
It comes with client libraries for many popular languages including
:doc:`C# <csharp/client>`,
:doc:`C++ <cpp/client>`,
:doc:`Java <java/client>`,
:doc:`Lua <lua/client>` and
:doc:`Python <python/client>`.
Clients, made by others, are also available for
`Ruby <https://github.com/TeWu/krpc-rb>`_ and
`Haskell <https://github.com/Cahu/krpc-hs>`_.

 * :doc:`Getting Started Guide <getting-started>`
 * :doc:`Tutorials and Examples <tutorials>`
 * :doc:`Clients, services and tools made by others <third-party>`

The mod exposes most of KSPs API for controlling and interacting with rockets,
and also includes support several popular mods including Ferram Aerospace
Research, Kerbal Alarm Clock and Infernal Robotics.

This functionality is provided to client programs via a server running in the
game. Client scripts connect to this server and use it to execute 'remote
procedures'. This communication can be done on local machine only, over a local
network, or even over the wider internet if configured correctly. The server is
extensible - additional remote procedures (grouped into "services") can be added
to the server using the :doc:`Service API <extending>`.

.. toctree::
   :hidden:

   getting-started
   tutorials
   cnano
   csharp
   cpp
   java
   lua
   python
   third-party
   compiling
   extending
   communication-protocols
   internals
