kRPC - Remote Procedure Calls for Kerbal Space Program
======================================================

kRPC lets you control Kerbal Space Program from scripts running outside the game. A server
inside the game exposes the game's API, and client libraries call it from Python, C#, C++, C,
Java and Lua. It also supports Ferram Aerospace Research, Kerbal Alarm Clock, Infernal Robotics
and other popular mods.

This script launches the active vessel straight up, then cuts the engines once it reaches
an altitude of 10 km:

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

.. grid:: 1 2 3 3
   :gutter: 3

   .. grid-item-card:: :fas:`rocket` Getting Started
      :link: getting-started
      :link-type: doc

      Install the server plugin, install a client and run your first script.

   .. grid-item-card:: :fas:`book` User Guides
      :link: user-guide
      :link-type: doc

      How kRPC and the game behave: reference frames, object lifetime, the auto-pilot and more.

   .. grid-item-card:: :fas:`graduation-cap` Tutorials
      :link: tutorials
      :link-type: doc

      Complete scripts that launch into orbit, land, dock and draw in the game.

   .. grid-item-card:: :fas:`code` Clients
      :link: clients
      :link-type: doc

      The guide and API reference for each client library.

   .. grid-item-card:: :fas:`screwdriver-wrench` Developer Guide
      :link: developer-guide
      :link-type: doc

      Write your own services, implement the protocol, or build kRPC from source.

   .. grid-item-card:: :fas:`clock-rotate-left` Changelog
      :link: changelog
      :link-type: doc

      The changes in each release of kRPC.

Clients
-------

.. grid:: 2 3 7 7
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

   .. grid-item-card:: Others
      :link: third-party
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
