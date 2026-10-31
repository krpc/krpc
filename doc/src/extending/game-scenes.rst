.. _service-api-game-scenes:

Game Scenes
===========

Anything that can be called is configured to be available from a particular game scene, or
scenes. This includes service procedures and properties, and class methods and properties. This is
controlled by the following enumeration:

.. cs:enum:: KRPC.Service.GameScene

   .. cs:enumerator:: Inherit

      Inherit the game scene from the containing service or class. This is the default.

   .. cs:enumerator:: All

      All game scenes.

   .. cs:enumerator:: SpaceCenter

      The game scene showing the Kerbal Space Center buildings.

   .. cs:enumerator:: Flight

      The game scene showing a vessel in flight (or on the launchpad/runway).

   .. cs:enumerator:: TrackingStation

      The tracking station.

   .. cs:enumerator:: EditorVAB

      The Vehicle Assembly Building.

   .. cs:enumerator:: EditorSPH

      The Space Plane Hangar.

   .. cs:enumerator:: Editor

      Either the VAB or the SPH.

   .. cs:enumerator:: MissionBuilder

      The mission builder.

   .. cs:enumerator:: AstronautComplex

      The astronaut complex facility, open within the space center scene.

   .. cs:enumerator:: MissionControl

      The mission control facility, open within the space center scene.

   .. cs:enumerator:: ResearchAndDevelopment

      The research and development facility, open within the space center scene.

   .. cs:enumerator:: Administration

      The administration facility, open within the space center scene.

The special enumeration value :cs:value:`KRPC.Service.GameScene.Inherit` can be used to inherit
the game scene setting from the containing service or class. Game scene inheritance works as
follows:

* A service set to :cs:value:`KRPC.Service.GameScene.Inherit` will be available in all game
  scenes. This is equivalent to setting it to :cs:value:`KRPC.Service.GameScene.All`.

* A service procedure or property set to :cs:value:`KRPC.Service.GameScene.Inherit` will be
  available in the same game scenes as the containing service.

* A class set to :cs:value:`KRPC.Service.GameScene.Inherit` will be available in the same game
  scenes as the containing service.

* A class method or property set to :cs:value:`KRPC.Service.GameScene.Inherit` will be available
  in the same game scenes as the containing class. If the containing class is set to
  :cs:value:`KRPC.Service.GameScene.Inherit` the class method or property will be available in
  the same game scenes as the containing service.

**Examples**

* Declare a service whose procedures are available in the
  :cs:value:`KRPC.Service.GameScene.Flight` game scene:

  .. literalinclude:: /scripts/extending/game-scenes/ServiceInScene.lib.cs
     :language: csharp
     :start-after: [docs]
     :end-before: [/docs]
     :dedent:

* Declare a service whose procedures are available in the
  :cs:value:`KRPC.Service.GameScene.Flight` and :cs:value:`KRPC.Service.GameScene.Editor` game
  scenes:

  .. literalinclude:: /scripts/extending/game-scenes/ServiceInScenes.lib.cs
     :language: csharp
     :start-after: [docs]
     :end-before: [/docs]
     :dedent:

* Declare a service whose procedures are available in the
  :cs:value:`KRPC.Service.GameScene.Flight` game scene by default, and declare a procedure which
  overrides this and is available in all game scenes:

  .. literalinclude:: /scripts/extending/game-scenes/ProcedureInScenes.lib.cs
     :language: csharp
     :start-after: [docs]
     :end-before: [/docs]
     :dedent:
