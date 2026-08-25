Object Lifetime
===============

Many remote procedures return an object: a vessel, a part, a part module, a maneuver node, a
reference frame. Each of these identifies something in the game, and every call made through
it looks up that game state. An object therefore keeps working when the game destroys and
rebuilds what it identifies, and raises ``KRPC.ObjectDestroyedException`` once it has been
destroyed for good.

Objects Across a Load
---------------------

Loading a save, quickloading and reverting all replace the game's own objects. A vessel's
parts are rebuilt the same way whenever it comes back into range.

An object obtained beforehand goes on working, and reads the state of the loaded game. So you
can hold onto a vessel or a part across a quickload:

.. code-block:: python

   vessel = conn.space_center.active_vessel
   parachute = vessel.parts.with_name('parachuteSingle')[0].parachute
   conn.space_center.quickload()
   # reads the parachute of the loaded game
   print(parachute.state)

Destroyed Objects
-----------------

Once what an object identifies is destroyed, every member of it that reads the game raises
``KRPC.ObjectDestroyedException``. Catch it as you would any other exception the client
library defines:

.. code-block:: python

   try:
       print(part.temperature)
   except conn.krpc.ObjectDestroyedException:
       print('that part is gone')

The ``KRPC`` API reference for each client language documents it alongside the other
exceptions the server raises.

What destroys an object depends on what it identifies:

* a part, once it is destroyed, or its vessel is destroyed or recovered
* a vessel, once it is destroyed or recovered
* a maneuver node, once it leaves the vessel's flight plan, which includes every node the
  vessel had before a game was loaded
* a science subject, once the game state it was read from is replaced

A client can also destroy an object itself. A crew member is identified by the kerbal's name,
which is the key the game's roster holds it under. Setting ``CrewMember.Name`` therefore
destroys every object for that kerbal, including the one the name was set through, and the
crew member has to be obtained again under the new name.

An object the server builds on request identifies nothing in the game, and lasts until the
client removes it:

* ``ReferenceFrame.Remove``, for a frame from ``ReferenceFrame.CreateRelative`` or
  ``ReferenceFrame.CreateHybrid``
* ``Orbit.Remove``, for an orbit from ``Orbit.CreateFromPositionAndVelocity`` or
  ``Orbit.CreateFromOrbitalElements``
* ``ResourceTransfer.Remove``, for a transfer between two parts

Removing one destroys whatever was built on it: a reference frame defined against a removed
frame or orbit, and the flight information measured in a removed frame.

Each of these belongs to the client that created it. Two clients that create the same
reference frame get one each, and removing one leaves the other intact. The server removes a
client's remaining objects when it disconnects.

A few members read no game state and so raise nothing, such as the part a part module belongs
to or the name of a field. These return a value as they always did, and the object one of them
returns raises the exception when it is used.

Loading a game also drops the server's own record of a destroyed object. Using such an object
raises the same exception, and a fresh one has to be obtained from the vessel, its parts, or
wherever the first came from.

Unloaded Objects
----------------

A vessel far enough from the active vessel is *unloaded*. The game keeps it as orbital data
and a description of its parts, and instantiates those parts again when it comes back into
range.

The vessel itself reads normally. Its parts are not instantiated, and using one raises an
error saying that the part is not loaded. ``Vessel.Loaded`` reports whether a vessel's parts
are available.

The object stays valid throughout, and the part works again once the game loads the vessel.
Two other cases raise the same error. A maneuver node raises it while the game computes no
flight plan for its vessel, and a vessel raises it while the game is between states, part way
through a load.

Objects in the Editor
---------------------

The vessel under construction in the VAB or the SPH follows the same rules, and everything the
editor holds is loaded. The editor holds one vessel, so a part removed from it is destroyed
and raises ``KRPC.ObjectDestroyedException``.

Loading a craft into the editor starts a new vessel and destroys the parts of the vessel it
replaces, whether or not it is the craft the editor already had. A part is identified by a
number that is unique within one vessel, and a craft file carries those numbers with it, so a
part of the old vessel is indistinguishable from the new part with the same number.

Undo and redo rebuild the vessel the editor already has, and the rebuilt parts keep their
numbers. Objects for the parts an undo leaves in place go on working, and an object for a part
an undo removes raises ``KRPC.ObjectDestroyedException``. Obtain a part again after an undo
followed by a redo.

Leaving the editor destroys the vessel it had open, along with every object reached through
``SpaceCenter.Editor``: its parts, their part modules, its stages and its resources. Obtain
these again on returning to the editor.

Objects in Other Services
-------------------------

The same rules hold for every service.

A line or a marker from the ``Drawing`` service, a panel or a button from ``UI``, and a force
from ``Part.AddForce`` are destroyed by their own removal, by the service's ``Clear``, or by a
scene change. The server also removes what a client created when that client disconnects. A
force is destroyed with the part it is applied to, and stops acting on it.

An object for something carried by a part lives as long as that part. A RemoteTech antenna, an
Infernal Robotics servo, a LaserDist laser and a docking camera are destroyed once their part
is destroyed, or once the part loses the module that provides them.

An Infernal Robotics servo group is identified by its vessel and the group's name. Renaming a
group destroys every object for it, exactly as renaming a kerbal does, and the group has to be
obtained again under the new name.

An object for a record the game keeps looks that record up on every call. An alarm, a contract
and its parameters, and a Kerbal Alarm Clock alarm all keep working across a load, and
removing an alarm or a waypoint destroys the object for it. A waypoint created by a client is
left out of the save, and lasts as long as the game state it was created in.

A comm link is a hop in a vessel's control path, and reports the path as it currently is. If
the path stops using that hop, the link reports itself as not connected, which is the
not-loaded case above. It works again once the path uses the hop again.

Objects from a mod that is not installed, or has yet to start, report themselves as
unavailable in the same way.
