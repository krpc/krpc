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
