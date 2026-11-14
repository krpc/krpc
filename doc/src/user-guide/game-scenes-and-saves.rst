.. _user-guide-game-scenes-and-saves:

Game Scenes and Saves
=====================

KSP is divided into scenes: the space center, flight, the tracking station, and the editors, the
VAB and the SPH. A remote call works in some scenes and not others. This guide describes the
scenes, how to move between them, and how to save, load and launch.

The Current Scene
-----------------

``KRPC.GameScene`` gives the current scene: ``SpaceCenter``, ``Flight``, ``TrackingStation``,
``EditorVAB`` or ``EditorSPH``. While a facility in the space center is open, it gives that
facility instead: ``AstronautComplex``, ``MissionControl``, ``ResearchAndDevelopment`` or
``Administration``.

The server runs only while a save is loaded. Returning to the main menu stops it, and closes every
connection.

What Works Where
----------------

A call outside the scenes it supports raises ``KRPC.UnsupportedSceneException``. For example:

* ``Control``, ``Flight``, ``AutoPilot``, ``Camera`` and ``Node`` work in flight only.
* ``SpaceCenter.Editor`` works in the VAB and the SPH only.
* Time warp works in flight, the space center and the tracking station.
* Saving, loading and launching work in every scene.

A stream of a call that the current scene does not support pauses, keeps its last value, and
resumes when the scene supports the call again. The :doc:`streams and events guide
</user-guide/streams-and-events>` describes this.

Changing Scene
--------------

Setting ``KRPC.GameScene`` changes scene, or opens or closes a facility. It returns straight away,
and the scene changes over the next few frames. This example goes to the space center, and waits
until it is there:

.. literalinclude:: /scripts/user-guide/game-scenes-and-saves/ChangeScene.py
   :language: python
   :start-after: [docs]
   :end-before: [/docs]
   :dedent:

Setting the scene to ``Flight`` resumes the save's active vessel. Leaving flight saves the game
first, unless the game cannot be saved. ``SpaceCenter.CanSave`` gives whether it can, and is false
while the vessel accelerates or moves over the surface.

Setting ``SpaceCenter.ActiveVessel`` switches to another vessel. Outside flight, it enters flight
with that vessel. The call returns once the switch has finished.

Saving and Loading
------------------

``SpaceCenter.Save`` and ``SpaceCenter.Load`` save and load a game by name, in the folder of the
current save. ``SpaceCenter.Quicksave`` and ``SpaceCenter.Quickload`` use the game's quicksave.

Loading enters flight with the save's active vessel, and the call returns once that vessel has
loaded. ``SpaceCenter.RevertToLaunch`` reverts the flight in the same way.
``SpaceCenter.CanRevertToLaunch`` gives whether a revert is possible. A revert that is not
possible does nothing.

Objects you obtained before a load go on working, and read the loaded game. The :doc:`object
lifetime guide </user-guide/object-lifetime>` describes this. ``KRPC.GameStateLoads`` goes up by
one each time a load, a revert or a change of scene finishes, so a stream of it tells you when the
game has been replaced.

Launching Vessels
-----------------

``SpaceCenter.LaunchableVessels`` lists the craft in the save's VAB or SPH folder.
``SpaceCenter.LaunchVesselFromVAB`` launches one onto the launch pad, and
``SpaceCenter.LaunchVesselFromSPH`` onto the runway. The call returns once the vessel has loaded
and settled:

.. literalinclude:: /scripts/user-guide/game-scenes-and-saves/LaunchVessel.py
   :language: python
   :start-after: [docs]
   :end-before: [/docs]
   :dedent:

``SpaceCenter.LaunchVessel`` also takes the launch site, the crew and a flag. The launch runs the
game's pre-flight checks, and raises an exception if they fail. By default it recovers a vessel
already on the launch site. Otherwise an occupied site raises an exception.

In the VAB or the SPH, ``Editor.LoadVessel`` loads a craft into the editor, and
``Editor.LaunchVessel`` launches the vessel being built.

Calls That Take Several Ticks
-----------------------------

Loading, reverting, launching, switching vessel and loading a craft into the editor each take many
physics ticks. A call to one of them returns once it has finished, and ends any :ref:`hold on the
tick <holding-a-tick>` that your client has.
