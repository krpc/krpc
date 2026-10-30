kRPC - Remote Procedure Calls for Kitten Space Agency
=====================================================

kRPC for KSA lets you control Kitten Space Agency from scripts running outside the game. A
server inside the game exposes the game's API, and the `kRPC <https://krpc.github.io/krpc/>`_
@KRPC_VERSION@ client libraries call it from Python, C#, C++, C, Java and Lua.

.. note::

   kRPC for KSA is experimental, and its API may change between releases.

.. note::

   kRPC for KSA uses the kRPC client libraries for KSP. The C#, C++, C-nano and Java clients
   also need the ``KSA`` :doc:`service stubs <clients>` from the release archive, which you add
   by hand until a later release makes this easier.

This script launches a rocket from Cape Canaveral straight up, then cuts the engines once it
reaches an altitude of 10 km:

.. tabs::

   .. tab:: Python

      .. literalinclude:: /scripts/index/Launch.py
         :language: python

   .. tab:: C#

      .. literalinclude:: /scripts/index/Launch.cs
         :language: csharp

   .. tab:: C++

      .. literalinclude:: /scripts/index/Launch.cpp
         :language: cpp

   .. tab:: C

      .. literalinclude:: /scripts/index/Launch.c
         :language: c

   .. tab:: Java

      .. literalinclude:: /scripts/index/Launch.java
         :language: java

   .. tab:: Lua

      .. literalinclude:: /scripts/index/Launch.lua
         :language: lua

   .. tab:: gRPC (Python)

      .. literalinclude:: /scripts/index/LaunchGrpc.py
         :language: python

.. grid:: 1 2 3 3
   :gutter: 3

   .. grid-item-card:: :fas:`rocket` Getting Started
      :link: getting-started
      :link-type: doc

      Install the server mod, install a client and run your first script.

   .. grid-item-card:: :fas:`book` User Guides
      :link: user-guide
      :link-type: doc

      How the server and the game behave: reference frames, object lifetime, the auto-pilot,
      server side functions and timing.

   .. grid-item-card:: :fas:`graduation-cap` Tutorials
      :link: tutorials
      :link-type: doc

      Complete scripts that launch into orbit, plan burns and change orbits.

   .. grid-item-card:: :fas:`code` Clients
      :link: clients
      :link-type: doc

      The guide and API reference for each client library.

   .. grid-item-card:: :fas:`screwdriver-wrench` Developer Guide
      :link: developer-guide
      :link-type: doc

      Build the mod, its client stubs and its documentation from source, and run the tests.

   .. grid-item-card:: :fas:`clock-rotate-left` Changelog
      :link: changelog
      :link-type: doc

      The changes in each release of kRPC for KSA.

   .. grid-item-card:: :fas:`satellite` kRPC for KSP
      :link: https://krpc.github.io/krpc/
      :columns: 12

      The kRPC documentation, for Kerbal Space Program.

Clients
-------

.. grid:: 2 4 7 7
   :gutter: 2

   .. grid-item-card:: Python
      :link: python
      :link-type: doc
      :text-align: center

   .. grid-item-card:: C#
      :link: csharp
      :link-type: doc
      :text-align: center

   .. grid-item-card:: C++
      :link: cpp
      :link-type: doc
      :text-align: center

   .. grid-item-card:: C-nano
      :link: cnano
      :link-type: doc
      :text-align: center

   .. grid-item-card:: Java
      :link: java
      :link-type: doc
      :text-align: center

   .. grid-item-card:: Lua
      :link: lua
      :link-type: doc
      :text-align: center

   .. grid-item-card:: gRPC
      :link: grpc
      :link-type: doc
      :text-align: center

.. toctree::
   :hidden:

   getting-started
   user-guide
   tutorials
   clients
   developer-guide
   changelog
