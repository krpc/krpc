.. _extending:

Extending kRPC
==============

The kRPC Architecture
---------------------

kRPC consists of two components: a server and a client. The server plugin runs inside the game.
It provides a collection of *procedures* that clients can run. These procedures are arranged in
groups called *services* to keep things organized. It also provides an in-game user interface that
can be used to start/stop the server, change settings and monitor active clients.

Clients run outside of the game. This gives you the freedom to run scripts in whatever environment
you want. A client communicates with the server to run procedures using one of the supported
:doc:`communication-protocols`.  kRPC comes with several client libraries that implement one of
these protocols, making it easier to write programs in these languages.

kRPC comes with a collection of standard functionality for interacting with the game. This provides
procedures for things like getting flight/orbital data and controlling a vessel. Services are
pluggable, and a new one is written using the :ref:`Service API <service-api-attributes>`.
Dropping a new service DLL into the mod directory will cause kRPC to automatically load it, and
provide its functionality to clients.

Example Service
---------------

Third party mods can add functionality to kRPC using the *Service API*. This is done by adding
:ref:`attributes <service-api-attributes>` to your own classes, methods and properties to make them
visible through the server. When the kRPC server starts, it scans all the assemblies loaded by the
game, looking for classes, methods and properties with these attributes.

The following example implements a service that can control the throttle and staging of the active
vessel. To add this to the server, compile the code and place the DLL in the mod directory.

.. literalinclude:: /scripts/extending/ServiceAPIExample.lib.cs
   :language: csharp

The following example shows how this service can then be used from a python client:

.. literalinclude:: /scripts/extending/ServiceAPIExample.py
   :language: python

The Python and Lua clients pick up new or changed services automatically when they connect. The
other clients need *stubs*, code generated from the service assembly. See
:ref:`clientgen <service-api-clientgen>` for how to generate them.

Service API Reference
---------------------

The following pages cover each part of the Service API:

* :doc:`extending/attributes` that make classes, methods and properties visible to clients.
* :doc:`extending/types` that can be passed to and returned from a procedure, and where a value
  may be null.
* :doc:`extending/events` that a procedure returns to a client.
* :doc:`extending/multi-tick` that span more than one physics update.
* :doc:`extending/game-scenes` in which a procedure is available.
* :doc:`extending/deprecation`, and how clients report it.
* :doc:`extending/extending-other-services`, by adding members to their classes.
* :doc:`extending/documentation` for a service, sent to clients and rendered in the API reference.
* :doc:`extending/clientgen`, the stubs for clients that need them.

.. toctree::
   :hidden:

   extending/attributes
   extending/types
   extending/events
   extending/multi-tick
   extending/game-scenes
   extending/deprecation
   extending/extending-other-services
   extending/documentation
   extending/clientgen
