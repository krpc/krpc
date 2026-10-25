kRPC for KSA
============

kRPC for KSA is an experimental `kRPC <https://krpc.github.io/krpc/>`_ server for Kitten
Space Agency. You write scripts in Python, C#, C++, C, Java or Lua that read telemetry from
the game and fly its vehicles. This script prints the active vehicle's altitude:

.. code-block:: python

   import krpc
   conn = krpc.connect()
   vehicle = conn.ksa.active_vehicle
   print(vehicle.name, vehicle.flight.mean_altitude)

The server is a `StarMap <https://github.com/StarMapLoader/StarMap>`_ code mod, and works
with the kRPC @KRPC_VERSION@ clients.

.. toctree::
   :maxdepth: 1

   getting-started
   clients
   tutorials/raise-orbit
   tutorials/circularize

.. toctree::
   :maxdepth: 1
   :caption: API

   api/python
   api/csharp
   api/cpp
   api/cnano
   api/java
   api/lua
