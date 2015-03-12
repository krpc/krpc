Examples
--------

.. code-block:: python

   import krpc

   # Connect to the server
   conn = krpc.connect()

   # Print out the name of the active vessel
   vessel = conn.space_center.active_vessel
   print vessel.name

   # Print out the names of all the vessels in the game
   for vessel in conn.space_center.vessels:
       print vessel.name

   # Print out the mass of Kerbin
   kerbin = conn.space_center.bodies['Kerbin']
   print kerbin.mass

   # Time warp to 1 hour (3600 seconds) from now
   conn.space_center.warp_to (conn.space_center.ut + 3600)
