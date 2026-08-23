Object Lifetime
===============

Many remote procedures return an object: a vessel, a part, a part module, a maneuver node, a
reference frame. Such an object is a *name* for something in the game, not a copy of it and
not a pointer into the game's memory. Every call made through it looks the thing up in the
game again, which is what lets an object outlive the game tearing down and rebuilding what it
names, and what lets the server say clearly when the thing is gone.

Objects follow the game
-----------------------

Loading a save, quickloading and reverting all replace the game's own objects, and the game
also rebuilds a vessel's parts whenever it comes back into range. An object obtained before
any of that keeps working afterwards, provided the thing it names still exists, and reads the
state of the loaded game rather than the state it was obtained in.

So a script can hold onto a vessel or a part across a quickload. In Python, for example:

.. code-block:: python

   vessel = conn.space_center.active_vessel
   parachute = vessel.parts.with_name('parachuteSingle')[0].parachute
   conn.space_center.quickload()
   # after the load, this reads the parachute of the game that was loaded
   print(parachute.state)

Objects that are gone
---------------------

When the thing an object names no longer exists, every member of that object that reads the
game raises ``KRPC.ObjectDestroyedException`` instead of returning a value. A part raises it
once it has been destroyed, or its vessel destroyed or recovered; a vessel raises it once it
has been recovered or destroyed; a maneuver node raises it once it has been removed from the
vessel's flight plan, which includes every node the vessel had before a game was loaded; a
science subject raises it once the game state it was read from has been replaced.

An object can also become gone because of what a client itself does. A crew member is named
by the kerbal's name, which is what the game keeps its roster under, so renaming a kerbal by
setting ``CrewMember.Name`` leaves every object for that kerbal standing for a kerbal the
roster no longer has, including the one the rename was made through. They all raise the
exception from then on, and an object for the renamed kerbal has to be obtained again under
the new name.

A reference frame built with ``ReferenceFrame.CreateRelative`` or
``ReferenceFrame.CreateHybrid`` is the other case. It names nothing in the game, so nothing
the game does ever finishes with it and the server holds it until the game is unloaded.
Calling ``ReferenceFrame.Remove`` is what says a script is done with one; the frame is then
gone, and so is any frame built on it. The server hands the same object to every client that
builds the same frame, so a frame one of them removes is gone for the others too.

A few members read nothing and so raise nothing: those that hand back what the object was
built from, such as the part a part module belongs to, or the name of a field. They answer as
they always did, and what they hand back raises the exception itself as soon as it is used.

The exception is documented in the ``KRPC`` API reference for each client language, alongside
the other exceptions the server can raise, and is caught like any other exception the client
library defines. In Python:

.. code-block:: python

   try:
       print(part.temperature)
   except conn.krpc.ObjectDestroyedException:
       print('that part is gone')

The server also stops holding an object once the thing it names is gone, which it does when a
game is loaded. Using such an object afterwards raises the same exception, so a client cannot
tell whether the server still had the object or had already let go of it. That is deliberate:
either way, the answer is that the thing is gone and a fresh object has to be obtained from
the vessel, its parts, or wherever the first one came from.

Objects that are not loaded
---------------------------

A vessel far enough from the active vessel is *unloaded*: the game keeps it as orbital data
and a description of its parts, and instantiates the parts again when it comes back into
range. The vessel itself is unaffected and can be read normally, but its parts do not exist to
be read, so using one raises an error saying that the part is not loaded.

That is not the same as the part being destroyed, and it is temporary. The part works again
once the game loads the vessel, and the object stays valid throughout. ``Vessel.Loaded``
reports whether a vessel's parts are there to be used.

Anything the game cannot currently answer for reports itself the same way, rather than
claiming to be gone. A maneuver node of a vessel the game is not computing a flight plan for
is one case, and a call that arrives while the game is between states, part way through
loading one, is another.

Objects in the editor
---------------------

The vessel under construction in the VAB or the SPH follows the same rules, with one
difference: nothing there has an unloaded form. The editor holds exactly one vessel, so a part
that is no longer in it is gone for good rather than waiting to be loaded, and using it raises
``KRPC.ObjectDestroyedException``.

Loading a craft into the editor starts a new vessel, and the parts of the vessel it replaced
are gone. That is so whether or not the craft loaded is the one the editor already had: a
part in the editor is named by an identifier that is only unique within one vessel, and a
craft file saved from another carries those identifiers over, so a part of the old vessel
cannot be told apart from the part of the new one that answers to the same identifier. Rather
than hand back a part of a vessel that was never asked for, both raise
``KRPC.ObjectDestroyedException``.

Undo and redo are not a new vessel. They rebuild the one the editor already has, and the
parts they rebuild keep the identifiers they had, so objects for the parts an undo leaves
alone go on working. An object for a part that an undo takes away raises
``KRPC.ObjectDestroyedException``, as one for any part no longer in the vessel does. An
object is not expected to survive an undo followed by a redo.

Leaving the editor destroys the vessel it had open, and with it every object reached through
``SpaceCenter.Editor``: its parts, their part modules, its stages and its resources. A script
that returns to the editor has to obtain them again.

Objects in other services
-------------------------

The same rules hold for every service, not only ``SpaceCenter``.

An object a client makes itself follows what the game does with it. A line or a marker from
the ``Drawing`` service, a panel or a button from ``UI``, and a force from ``Part.AddForce``
are all gone once they are removed, once the service's ``Clear`` is called, or once the scene
changes, and using one then raises ``KRPC.ObjectDestroyedException``. The server also removes
what a client made when that client disconnects. A force is applied to a part, so it also
stops, and is gone, once that part is destroyed.

An object that stands for something on a part or a vessel is exactly as alive as the part or
the vessel. A RemoteTech antenna, an Infernal Robotics servo, a LaserDist laser and a docking
camera all raise the exception once their part is destroyed, or once the part no longer
carries the module that made it one of those things. An Infernal Robotics servo group is named
by its vessel and the group's name, so renaming a group leaves every object for it standing
for a group the vessel no longer has, exactly as renaming a kerbal does, and an object for the
group has to be obtained again under the new name.

An object that stands for a record the game keeps finds that record again on every call, so it
goes on working when the game rebuilds it and raises the exception once the game no longer has
it. An alarm, a contract and its parameters, and a Kerbal Alarm Clock alarm all keep working
across a load; removing an alarm or a waypoint leaves the object for it gone. A waypoint a
client creates is not written into the save, so it does not outlive the game state it was
created in.

A comm link is a hop in a vessel's control path, and reports that path as it is now rather
than as it was when the object was obtained. A path that no longer takes the hop has lost
contact rather than destroyed anything, so the link reports itself as not currently connected,
which is the not-loaded case above, and works again if the path takes it again.

Where a mod is not installed, or is not ready to be asked, its objects report themselves as
unavailable rather than gone, for the same reason a part that is not loaded does: nothing that
can be asked has said the thing is not there.
