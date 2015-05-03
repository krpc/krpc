.. _api-parts:

Parts
=====

The following classes allow interaction with a vessels individual parts.

.. contents::
   :local:

Parts
-----

.. class:: Parts

   Instances of this class are used to interact with the parts of a vessel. An
   instance can be obtained by calling :attr:`Vessel.Parts`.

   .. attribute:: All

      Gets a list of all of the vessels parts.

      :type: :class:`List` ( :class:`Part` )

   .. attribute:: Root

      Gets the vessels root part.

      :rtype: :class:`Part`

      .. note:: See the discussion on :ref:`api-parts-trees-of-parts`.

   .. attribute:: Controlling

      Gets or sets the part from which the vessel is controlled.

      :rtype: :class:`Part`

   .. method:: WithName (name)

      Gets a list of parts whose :attr:`Part.Name` is *name*.

      :param string name: The name of the parts
      :rtype: :class:`List` ( :class:`Part` )

   .. method:: WithTitle (title)

      Gets a list of all parts whose :attr:`Part.Title` is *title*.

      :param string title: The title of the parts
      :rtype: :class:`List` ( :class:`Part` )

   .. method:: WithModule (moduleName)

      Gets a list of all parts that contain a :class:`Module` whose
      :attr:`Module.Name` is *moduleName*.

      :param string moduleName: The module name
      :rtype: :class:`List` ( :class:`Part` )

   .. method:: InStage (stage)

      Gets a list of all parts that are activated in the given *stage*.

      :param int32 stage:
      :rtype: :class:`List` ( :class:`Part` )

      .. note:: See the discussion on :ref:`api-parts-staging`.

   .. method:: InDecoupleStage (stage)

      Gets a list of all parts that are decoupled in the given *stage*.

      :param int32 stage:
      :rtype: :class:`List` ( :class:`Part` )

      .. note:: See the discussion on :ref:`api-parts-staging`.

   .. method:: ModulesWithName (moduleName)

      Gets a list of modules (combined across all parts in the vessel) whose
      :attr:`Module.Name` is *moduleName*.

      :param string moduleName:
      :rtype: :class:`List` ( :class:`Module` )

   .. attribute:: Decouplers

      Gets a list of all decouplers in the vessel.

      :rtype: :class:`List` ( :class:`Decoupler` )

   .. attribute:: DockingPorts

      Gets a list of all docking ports in the vessel.

      :rtype: :class:`List` ( :class:`DockingPort` )

   .. method:: DockingPortWithName (name)

      Gets the first docking port in the vessel with the given port name, as
      returned by :attr:`DockingPort.Name`. Returns ``null`` if there are no
      such docking ports.

      :param string name:
      :rtype: :class:`DockingPort`

   .. attribute:: Engines

      Gets a list of all engines in the vessel.

      :rtype: :class:`List` ( :class:`Engine` )

   .. attribute:: LandingGear

      Gets a list of all landing gear attached to the vessel.

      :rtype: :class:`List` ( :class:`LandingGear` )

   .. attribute:: LandingLegs

      Gets a list of all landing legs attached to the vessel.

      :rtype: :class:`List` ( :class:`LandingLeg` )

   .. attribute:: LaunchClamps

      Gets a list of all launch clamps attached to the vessel.

      :rtype: :class:`List` ( :class:`LaunchClamp` )

   .. attribute:: Lights

      Gets a list of all lights in the vessel.

      :rtype: :class:`List` ( :class:`Light` )

   .. attribute:: Parachutes

      Gets a list of all parachutes in the vessel.

      :rtype: :class:`List` ( :class:`Parachute` )

   .. attribute:: ReactionWheels

      Gets a list of all reaction wheels in the vessel.

      :rtype: :class:`List` ( :class:`ReactionWheel` )

   .. attribute:: Sensors

      Gets a list of all sensors in the vessel.

      :rtype: :class:`List` ( :class:`Sensor` )

   .. attribute:: SolarPanels

      Gets a list of all solar panels in the vessel.

      :rtype: :class:`List` ( :class:`SolarPanel` )

Part
----

.. class:: Part

   Instances of this class represents a part. A vessel is made of multiple
   parts. Instances can be obtained by various methods in :class:`Parts`.

   .. attribute:: Name

      Internal name of the part, as used in `part cfg files
      <http://wiki.kerbalspaceprogram.com/wiki/CFG_File_Documentation>`_. For
      example "Mark1-2Pod".

      :rtype: string

   .. attribute:: Title

      Title of the part, as shown when the part is right clicked in-game. For
      example "Mk1-2 Command Pod".

      :rtype: string

   .. attribute:: Cost

      Gets the cost of the part, in units of funds.

      :rtype: float

   .. attribute:: Vessel

      Gets the vessel that contains this part.

      :rtype: :class:`Vessel`

   .. attribute:: Parent

      Gets the parts parent. Returns ``null`` if the part does not have a
      parent. This, in combination with :attr:`Part.Children`, can be used to
      traverse the vessels parts tree.

      :rtype: :class:`Part`

      .. note:: See the discussion on :ref:`api-parts-trees-of-parts`.

   .. attribute:: Children

      Gets the parts children. Returns an empty list if the part has no
      children. This, in combination with :attr:`Part.Parent`, can be used to
      traverse the vessels parts tree.

      :rtype: :class:`List` ( :class:`Part` )

      .. note:: See the discussion on :ref:`api-parts-trees-of-parts`.

   .. attribute:: AxiallyAttached

      Gets whether the part is *axially* attached to its parent, i.e. on the top
      or bottom of its parent. If the part has no parent, returns ``false``.

      :rtype: bool

      .. note:: See the discussion on :ref:`api-parts-attachment-modes`.

   .. attribute:: RadiallyAttached

      Gets whether the part is *radially* attached to its parent, i.e. on the
      side of its parent. If the part has no parent, returns ``false``.

      :rtype: bool

      .. note:: See the discussion on :ref:`api-parts-attachment-modes`.

   .. attribute:: Stage

      Gets the stage in which this part will be activated. Returns -1 if the
      part is not activated by staging.

      :rtype: int32

      .. note:: See the discussion on :ref:`api-parts-staging`.

   .. attribute:: DecoupleStage

      Gets the stage in which this part will be decoupled. Returns -1 if the
      part is never decoupled from the vessel.

      :rtype: int32

      .. note:: See the discussion on :ref:`api-parts-staging`.

   .. attribute:: Massless

      Gets whether the part is `"massless"
      <http://wiki.kerbalspaceprogram.com/wiki/Massless_part>`_ -- returning
      ``True`` if it is, ``False`` otherwise.

      :rtype: bool

   .. attribute:: Mass

      Gets the current mass of the part, including resources it contains, in
      kilograms. Returns zero if the part is massless.

      :rtype: float

   .. attribute:: DryMass

      Gets the mass of the part, not including any resources it contains, in
      kilograms. Returns zero if the part is massless.

      :rtype: float

   .. attribute:: ImpactTolerance

      Gets the impact tolerance of the part, in meters per second.

      :rtype: float

   .. attribute:: Temperature

      Gets the current temperature of the part, in Kelvin.

      :rtype: float

   .. attribute:: MaxTemperature

      Gets the maximum temperature that the part can survive, in Kelvin.

      :rtype: float

   .. attribute:: Resources

      Gets a resources object for the part.

      :rtype: :class:`PartResources`

   .. attribute:: Crossfeed

      Gets whether this part is crossfeed capable.

      :rtype: bool

   .. attribute:: FuelLinesFrom

      Gets the list of parts that are connected to this part via fuel lines,
      where the direction of the fuel line is *into* this part.

      :rtype: bool

      .. note:: See the discussion on :ref:`api-parts-fuel-lines`.

   .. attribute:: FuelLinesTo

      Gets the list of parts that are connected to this part via fuel lines,
      where the direction of the fuel line is *out of* this part.

      :rtype: bool

      .. note:: See the discussion on :ref:`api-parts-fuel-lines`.

   .. attribute:: Modules

      Gets the modules for this part.

      :rtype: :class:`List` ( :class:`Module` )

   .. attribute:: Decoupler

      A :class:`Decoupler` if the part is a decoupler, otherwise ``null``.

      :rtype: :class:`Decoupler`

   .. attribute:: DockingPort

      A :class:`DockingPort` if the part is a docking port, otherwise ``null``.

      :rtype: :class:`DockingPort`

   .. attribute:: Engine

      An :class:`Engine` if the part is an engine, otherwise ``null``.

      :rtype: :class:`Engine`

   .. attribute:: LandingGear

      A :class:`LandingGear` if the part is landing gear, otherwise ``null``.

      :rtype: :class:`LandingGear`

   .. attribute:: LandingLeg

      A :class:`LandingLeg` if the part is a landing leg, otherwise ``null``.

      :rtype: :class:`LandingLeg`

   .. attribute:: LaunchClamp

      A :class:`LaunchClamp` if the part is a launch clamp, otherwise ``null``.

      :rtype: :class:`LaunchClamp`

   .. attribute:: Light

      A :class:`Light` if the part is a light, otherwise ``null``.

      :rtype: :class:`Light`

   .. attribute:: Parachute

      A :class:`Parachute` if the part is a parachute, otherwise ``null``.

      :rtype: :class:`Parachute`

   .. attribute:: ReactionWheel

      A :class:`ReactionWheel` if the part is a reaction wheel, otherwise ``null``.

      :rtype: :class:`ReactionWheel`

   .. attribute:: Sensor

      A :class:`Sensor` if the part is a sensor, otherwise ``null``.

      :rtype: :class:`Sensor`

   .. attribute:: SolarPanel

      A :class:`SolarPanel` if the part is a solar panel, otherwise ``null``.

      :rtype: :class:`SolarPanel`

   .. method:: Position (referenceFrame)

      Gets the position of the part in the given reference frame.

      :param ReferenceFrame referenceFrame:
      :rtype: :class:`Vector3`

   .. method:: Direction (referenceFrame)

      Gets the direction of the part in the given reference frame.

      :param ReferenceFrame referenceFrame:
      :rtype: :class:`Vector3`

   .. method:: Velocity (referenceFrame)

      Gets the velocity of the part in the given reference frame.

      :param ReferenceFrame referenceFrame:
      :rtype: :class:`Vector3`

   .. method:: Rotation (referenceFrame)

      Gets the rotation of the part in the given reference frame.

      :param ReferenceFrame referenceFrame:
      :rtype: :class:`Quaternion`

   .. attribute:: ReferenceFrame

      Gets the reference frame that is fixed relative to this part.

      * The origin is at the position of the part.

      * The axes rotate with the part.

      * The x, y and z axis directions depend on the design of the part.

      :rtype: :class:`ReferenceFrame`

      .. figure:: /images/reference-frames/part.png
         :align: center

         Mk1 Command Pod reference frame origin and axes

      .. note:: For docking port parts, this reference frame is not necessarily
                equivalent to the reference frame for the docking port, returned
                by :attr:`DockingPort.ReferenceFrame`.

Module
------

.. class:: Module

   In KSP, each part has zero or more `PartModules`_ associated with it. Each
   one contains some of the functionality of the part. For example, an engine has
   a "ModuleEngines" PartModule that contains all the functionality of an
   engine.

   This class allows you to interact with KSPs PartModules, and any PartModules
   that have been added by other mods.

   .. attribute:: Name

      Name of the `PartModule`_.
      For example, "ModuleEngines".

      :rtype: string

   .. attribute:: Part

      The part that contains this module.

      :rtype: :class:`Part`

   .. attribute:: Fields

      The modules field names and their associated values, as a
      dictionary. These are the values visible in the right-click menu of the
      part.

      :rtype: :class:`Dictionary` ( string , string )

   .. method:: HasField (name)

      Returns ``true`` if the module has a field with the given name.

      :param string name: name of the field
      :rtype: bool

   .. method:: GetField (name)

      Returns the value of a field.

      :param string name: name of the field
      :rtype: string

   .. attribute:: Events

      A list of the names of all of the modules events. Events are the clickable
      buttons visible in the right-click menu of the part.

      :rtype: :class:`List` ( string )

   .. method:: HasEvent (name)

      True if the module has an event with the given name.

      :rtype: bool

   .. method:: TriggerEvent (name)

      Trigger the named event. Equivalent to clicking the button in the
      right-click menu of the part.

   .. attribute:: Actions

      A list of all the names of the modules actions. These are the parts actions that
      can be assigned to action groups in the in-game editor.

      :rtype: :class:`List` ( string )

   .. method:: HasAction (name)

      True if the part has an action with the given name.

      :rtype: bool

   .. method:: SetAction (name, [value = true])

      Set the value of an action with the given name.

PartResources
-------------

.. class:: PartResources

   Used to examine the resources stored in a part. An instance can be obtained
   via :attr:`Part.Resources`.

   .. attribute:: Names

      Gets a list of the resources that the part can store.

      :rtype: :class:`List` ( string )

   .. method:: HasResource (name)

      Gets whether the part has the named resource.

      :param string name:
      :rtype: bool

   .. method:: Max (name)

      Gets the maximum amount of the named resource that the part can store.

      :param string name:
      :rtype: float

   .. method:: Amount (name)

      Gets the current amount of the named resource that the part is storing.

      :param string name:
      :rtype: float

Specific Types of Part
----------------------

The following classes provide functionality for specific types of part.

.. contents::
   :local:

Decoupler
^^^^^^^^^

.. class:: Decoupler

   Obtained by calling :attr:`Part.Decoupler`.

   .. attribute:: Part

      Gets the part object for this decoupler.

      :rtype: :class:`Part`

   .. method:: Decouple ()

      Fires the decoupler. Has no effect if the decoupler has already fired.

   .. attribute:: Decoupled

      Gets whether the decoupler has fired.

      :rtype: bool

   .. attribute:: Impulse

      Gets the impulse, or momentum, that the decoupler imparts when it is
      fired, in Newton seconds.

      :rtype: float

Docking Port
^^^^^^^^^^^^

.. class:: DockingPort

   Obtained by calling :attr:`Part.DockingPort`.

   .. attribute:: Part

      Gets the part object for this docking port.

      :rtype: :class:`Part`

   .. attribute:: Name

      Gets the port name of the docking port. This is the name of the port that
      can be set in the right click menu, when the `Docking Port Alignment
      Indicator`_ mod is installed. If this mod is not installed, returns the
      title of the part (:attr:`Part.Title`).

      :rtype: string

   .. attribute:: State

      Gets the current state of the docking port.

      :rtype: :class:`DockingPortState`

   .. attribute:: DockedPart

      Gets the part that this docking port is docked to. Returns ``null`` if
      this docking port is not docked to anything.

      :rtype: :class:`Part`

   .. method:: Undock ()

      Undocks the docking port and returns the vessel that was undocked
      from.

      Note that after undocking, the active vessel may change
      (:attr:`SpaceCenter.ActiveVessel`). This method can be called for either
      docking port in a docked pair -- both calls will have the same
      effect. Returns ``null`` if the docking port is not docked to anything.

      :rtype: :class:`Vessel`

   .. attribute:: ReengageDistance

      Gets the distance a docking port must move away when it undocks before it
      becomes ready to dock with another port, in meters.

      :rtype: float

   .. attribute:: HasShield

      Gets whether the docking port has a shield.

      :rtype: bool

   .. attribute:: Shielded

      Gets or sets the state of the docking ports shield, if it has one.

      Returns ``true`` if the docking port has a shield, and the shield is
      closed. Otherwise returns ``false``. When set to ``true``, the shield is
      closed, and when set to ``false`` the shield is opened. If the docking
      port does not have a shield, setting this attribute has no effect.

   .. method:: Position (referenceFrame)

      Gets the position of the docking port in the given reference frame.

      :param ReferenceFrame referenceFrame:
      :rtype: :class:`Vector3`

   .. method:: Direction (referenceFrame)

      Gets the direction that docking port points in, in the given reference
      frame.

      :param ReferenceFrame referenceFrame:
      :rtype: :class:`Vector3`

   .. method:: Rotation (referenceFrame)

      Gets the rotation of the docking port, in the given reference frame.

      :param ReferenceFrame referenceFrame:
      :rtype: :class:`Quaternion`

   .. attribute:: ReferenceFrame

      Gets the reference frame that is fixed relative to this docking port, and
      oriented with the port.

      * The origin is at the position of the docking port.

      * The axes rotate with the docking port.

      * The x-axis points out to the right side of the docking port.

      * The y-axis points in the direction the docking port is facing.

      * The z-axis points out of the bottom off the docking port.

      :rtype: :class:`ReferenceFrame`

      .. figure:: /images/reference-frames/docking-port.png
         :align: center

         Docking port reference frame origin and axes

      .. figure:: /images/reference-frames/docking-port-inline.png
         :align: center

         Inline docking port reference frame origin and axes

      .. note:: This reference frame is not necessarily equivalent to the
                reference frame for the part, returned by
                :attr:`Part.ReferenceFrame`.

.. class:: DockingPortState

   .. data:: Ready

      The docking port is ready to dock to another docking port.

   .. data:: Docked

      The docking port is docked to another docking port, or docked to another
      part (from the VAB/SPH).

   .. data:: Docking

      The docking port is very close to another docking port, but has not
      docked. It is using magnetic force to acquire a solid dock.

   .. data:: Undocking

      The docking port has just been undocked from another docking port, and is
      disabled until it moves away by a sufficient distance
      (:attr:`DockingPort.ReengageDistance`).

   .. data:: Shielded

      The docking port has a shield, and the shield is closed.

   .. data:: Moving

      The docking ports shield is currently opening/closing.

Engine
^^^^^^

.. class:: Engine

   Obtained by calling :attr:`Part.Engine`.

   .. attribute:: Part

      Gets the part object for this engine.

      :rtype: :class:`Part`

   .. attribute:: Active

      Gets or sets whether the engine is active. Setting this attribute may have
      no effect, depending on :attr:`Engine.CanShutdown` and
      :attr:`Engine.CanRestart`.

      :rtype: bool

   .. attribute:: Thrust

      Gets the current amount of thrust being produced by the engine, in
      Newtons. Returns zero if the engine is not active.

      :rtype: float

   .. attribute:: AvailableThrust

      Gets the maximum available amount of thrust that can be produced by the
      engine, in Newtons. This takes :attr:`Engine.ThrustLimit` into account,
      and is the amount of thrust produced by the engine when activated and the
      main throttle is set to 100%.

      :rtype: float

   .. attribute:: MaxThrust

      Gets the maximum amount of thrust that can be produced by the engine, in
      Newtons. This is the amount of thrust produced by the engine when
      activated, :attr:`Engine.ThrustLimit` is set to 100% and the main vessel's
      throttle is set to 100%.

      :rtype: float

   .. attribute:: MaxVacuumThrust

      Gets the maximum amount of thrust that can be produced by the engine in a
      vacuum, in Newtons. This is the amount of thrust produced by the engine
      when activated, :attr:`Engine.ThrustLimit` is set to 100%, the main
      vessel's throttle is set to 100% and the engine is in a vacuum.

      :rtype: float

   .. attribute:: ThrustLimit

      Gets or sets the thrust limiter of the engine. A value between 0
      and 1. Setting this attribute may have no effect, for example the thrust
      limit for a solid rocket booster cannot be changed in flight.

      :rtype: float

   .. attribute:: SpecificImpulse

      Gets the current specific impulse of the engine, in seconds. Returns zero
      if the engine is not active.

      :rtype: float

   .. attribute:: VacuumSpecificImpulse

      Gets the vacuum specific impulse of the engine, in seconds.

      :rtype: float

   .. attribute:: KerbinSeaLevelSpecificImpulse

      Gets the specific impulse of the engine at sea level on Kerbin, in
      seconds.

      :rtype: float

   .. attribute:: Propellants

      Gets the names of resources that the engine consumes.

      :rtype: :class:`List` ( string )

   .. attribute:: HasFuel

      Gets whether the engine has flamed out, i.e. run out of fuel.

      :rtype: bool

   .. attribute:: Throttle

      Gets the current throttle setting for the engine. A value between 0
      and 1. This is not necessarily the same as the vessel's main throttle
      setting, as some engines take time to adjust their throttle (such as jet
      engines).

      :rtype: float

   .. attribute:: ThrottleLocked

      Gets whether the :attr:`Control.Throttle` affects the engine. For example,
      this is ``true`` for liquid fueled rockets, and ``false`` for solid rocket
      boosters.

      :rtype: bool

   .. attribute:: CanRestart

      Gets whether the engine can be restarted once shutdown. If the engine
      cannot be shutdown, returns ``false``. For example, this is ``true`` for
      liquid fueled rockets and ``false`` for solid rocket boosters.

      :rtype: bool

   .. attribute:: CanShutdown

      Gets whether the engine can be shutdown once activated. For example, this
      is ``true`` for liquid fueled rockets and ``false`` for solid rocket
      boosters.

      :rtype: bool

   .. attribute:: Gimballed

      Gets whether the engine nozzle is gimballed, i.e. can provide a turning
      force.

      :rtype: bool

   .. attribute:: GimbalRange

      Gets the range over which the gimbal can move, in degrees.

      :rtype: float

   .. attribute:: GimbalLocked

      Gets or sets whether the engines gimbal is locked in place. Setting this
      attribute has no effect if the engine is not gimballed.

      :rtype: bool

   .. attribute:: GimbalLimit

      Gets or sets the gimbal limiter of the engine. A value between 0
      and 1. Returns 0 if the gimbal is locked or the engine is not
      gimballed. Setting this attribute has no effect if the engine is not
      gimballed.

      :rtype: float

Landing Gear
^^^^^^^^^^^^

.. class:: LandingGear

   Obtained by calling :attr:`Part.LandingGear`.

   .. attribute:: Part

      Gets the part object for this landing gear.

      :rtype: :class:`Part`

   .. attribute:: State

      Gets the current state of the landing gear.

      :rtype: :class:`LandingGearState`

   .. attribute:: Deployed

      Gets or sets whether the landing gear is deployed.

      :rtype: bool

.. class:: LandingGearState

   .. data:: Deployed

   .. data:: Retracted

   .. data:: Deploying

   .. data:: Retracting

Landing Leg
^^^^^^^^^^^

.. class:: LandingLeg

   Obtained by calling :attr:`Part.LandingLeg`.

   .. attribute:: Part

      Gets the part object for this landing leg.

      :rtype: :class:`Part`

   .. attribute:: State

      Gets the current state of the landing leg.

      :rtype: :class:`LandingLegState`

   .. attribute:: Deployed

      Gets or sets whether the landing leg is deployed.

      :rtype: bool

.. class:: LandingLegState

   .. data:: Deployed

   .. data:: Retracted

   .. data:: Deploying

   .. data:: Retracting

   .. data:: Broken

   .. data:: Repairing

Launch Clamp
^^^^^^^^^^^^

.. class:: LaunchClamp

   Obtained by calling :attr:`Part.LaunchClamp`.

   .. attribute:: Part

      Gets the part object for this launch clamp.

      :rtype: :class:`Part`

   .. method:: Release ()

      Releases the docking clamp. Has no effect if the clamp has already been
      released.

Light
^^^^^

.. class:: Light

   Obtained by calling :attr:`Part.Light`.

   .. attribute:: Part

      Gets the part object for this light.

      :rtype: :class:`Part`

   .. attribute:: Active

      Gets or sets whether the light is switched on.

      :rtype: bool

   .. attribute:: PowerUsage

      Gets the current power usage, in units of charge per second.

      :rtype: float

Parachute
^^^^^^^^^

.. class:: Parachute

   Obtained by calling :attr:`Part.Parachute`.

   .. attribute:: Part

      Gets the part object for this parachute.

      :rtype: :class:`Part`

   .. method:: Deploy ()

      Deploys the parachute. This has no effect if the parachute has already
      been deployed.

   .. attribute:: Deployed

      Gets whether the parachute has been deployed.

      :rtype: bool

   .. attribute:: State

      Gets the current state of the parachute.

      :rtype: :class:`ParachuteState`

   .. attribute:: DeployAltitude

      Gets or sets the altitude at which the parachute will full deploy, in
      meters.

      :rtype: float

   .. attribute:: DeployMinPressure

      Gets or sets the minimum pressure at which the parachute will semi-deploy,
      in atm.

      :rtype: float

.. class:: ParachuteState

   .. attribute:: Stowed

      The parachute is safely tucked away inside its housing.

   .. attribute:: Active

      The parachute is still stowed, but ready to semi-deploy.

   .. attribute:: SemiDeployed

      The parachute has been deployed and is providing some drag, but is not
      fully deployed yet.

   .. attribute:: Deployed

      The parachute is fully deployed.

   .. attribute:: Cut

      The parachute has been cut.

Reaction Wheel
^^^^^^^^^^^^^^

.. class:: ReactionWheel

   Obtained by calling :attr:`Part.ReactionWheel`.

   .. attribute:: Part

      Gets the part object for this reaction wheel.

      :rtype: :class:`Part`

   .. attribute:: Active

      Gets or sets whether the reaction wheel is active.

      :rtype: bool

   .. attribute:: Broken

      Gets whether the reaction wheel is broken.

      :rtype: bool

   .. attribute:: PitchTorque

      Gets the torque in the pitch axis, in Newton meters.

      :rtype: float

   .. attribute:: YawTorque

      Gets the torque in the yaw axis, in Newton meters.

      :rtype: float

   .. attribute:: RollTorque

      Gets the torque in the roll axis, in Newton meters.

      :rtype: float

Sensor
^^^^^^

.. class:: Sensor

   Obtained by calling :attr:`Part.Sensor`.

   .. attribute:: Part

      Gets the part object for this sensor.

      :rtype: :class:`Part`

   .. attribute:: Active

      Gets or sets whether the sensor is active.

      :rtype: bool

   .. attribute:: Value

      Gets the current value of the sensor.

      :rtype: string

   .. attribute:: PowerUsage

      Gets the current power usage of the sensor, in units of charge per second.

      :rtype: float

Solar Panel
^^^^^^^^^^^

.. class:: SolarPanel

   Obtained by calling :attr:`Part.SolarPanel`.

   .. attribute:: Part

      Gets the part object for this solar panel.

      :rtype: :class:`Part`

   .. attribute:: Deployed

      Gets or sets whether the solar panel is extended.

      :rtype: bool

   .. attribute:: State

      Gets the current state of the solar panel.

      :rtype: :class:`SolarPanelState`

   .. attribute:: EnergyFlow

      Gets the current amount of energy being generated by the solar panel, in
      units of charge per second.

      :rtype: float

   .. attribute:: SunExposure

      Gets the current amount of sunlight that is incident on the solar panel,
      as a percentage. A value between 0 and 1.

      :rtype: float

.. class:: SolarPanelState

   .. data:: Extended

   .. data:: Retracted

   .. data:: Extending

   .. data:: Retracting

   .. data:: Broken

.. _api-parts-trees-of-parts:

Trees of Parts
--------------

Vessels in KSP are comprised of a number of parts, connected to one another in a
*tree* structure. An example vessel is shown in Figure 1, and the corresponding
tree of parts in Figure 2. The craft file for this example can also be
:download:`downloaded here </crafts/PartsTree.craft>`.

.. figure:: /images/api/parts.png
   :align: left
   :figwidth: 275

   **Figure 1** -- Example parts making up a vessel.

.. figure:: /images/api/parts-tree.png
   :align: right
   :figwidth: 275

   **Figure 2** -- Tree of parts for the vessel in Figure 1. Arrows point from
   the parent part to the child part.

.. container:: clearer

   ..

Traversing the Tree
^^^^^^^^^^^^^^^^^^^

The tree of parts can be traversed using the attributes :attr:`Parts.Root`,
:attr:`Part.Parent` and :attr:`Part.Children`.

The root of the tree is the same as the vessels *root part* (part number 1 in
the example above) and can be obtained by calling :attr:`Parts.Root`. A parts
children can be obtained by calling :attr:`Part.Children`. If the part does not
have any children, :attr:`Part.Children` returns an empty list. A parts parent
can be obtained by calling :attr:`Part.Parent`. If the part does not have a
parent (as is the case for the root part), :attr:`Part.Parent` returns ``null``.

The following python example uses these attributes to perform a depth-first
traversal over all of the parts in a vessel:

.. code-block:: python

   root = vessel.parts.root
   stack = [(root, 0)]
   while len(stack) > 0:
       part,depth = stack.pop()
       print(' '*depth, part.title)
       for child in part.children:
           stack.append((child, depth+1))

When this code is execute using the craft file for the example vessel pictured
above, the following is printed out::

    Command Pod Mk1
     TR-18A Stack Decoupler
      FL-T400 Fuel Tank
       LV-909 Liquid Fuel Engine
        TR-18A Stack Decoupler
         FL-T800 Fuel Tank
          LV-909 Liquid Fuel Engine
          TT-70 Radial Decoupler
           FL-T400 Fuel Tank
            TT18-A Launch Stability Enhancer
            FTX-2 External Fuel Duct
            LV-909 Liquid Fuel Engine
            Aerodynamic Nose Cone
          TT-70 Radial Decoupler
           FL-T400 Fuel Tank
            TT18-A Launch Stability Enhancer
            FTX-2 External Fuel Duct
            LV-909 Liquid Fuel Engine
            Aerodynamic Nose Cone
       LT-1 Landing Struts
       LT-1 Landing Struts
     Mk16 Parachute

.. _api-parts-attachment-modes:

Attachment Modes
^^^^^^^^^^^^^^^^

Parts can be attached to other parts either *radially* (on the side of the
parent part) or *axially* (on the end of the parent part, to form a stack).

For example, in the vessel pictured above, the parachute (part 2) is *axially*
connected to its parent (the command pod -- part 1), and the landing leg
(part 5) is *radially* connected to its parent (the fuel tank -- part 4).

The root part of a vessel (for example the command pod -- part 1) does not have
a parent part, so does not have an attachment mode. However, the part is
consider to be *axially* attached to nothing.

The following python example does a depth-first traversal as before, but also
prints out the attachment mode used by the part:

.. code-block:: python

   root = vessel.parts.root
   stack = [(root, 0)]
   while len(stack) > 0:
       part,depth = stack.pop()
       if part.axially_attached:
           attach_mode = 'axial'
       else: # radially_attached
           attach_mode = 'radial'
       print(' '*depth, part.title, '-', attach_mode)
       for child in part.children:
           stack.append((child, depth+1))

When this code is execute using the craft file for the example vessel pictured
above, the following is printed out::

 Command Pod Mk1 - axial
  TR-18A Stack Decoupler - axial
   FL-T400 Fuel Tank - axial
    LV-909 Liquid Fuel Engine - axial
     TR-18A Stack Decoupler - axial
      FL-T800 Fuel Tank - axial
       LV-909 Liquid Fuel Engine - axial
       TT-70 Radial Decoupler - radial
        FL-T400 Fuel Tank - radial
         TT18-A Launch Stability Enhancer - radial
         FTX-2 External Fuel Duct - radial
         LV-909 Liquid Fuel Engine - axial
         Aerodynamic Nose Cone - axial
       TT-70 Radial Decoupler - radial
        FL-T400 Fuel Tank - radial
         TT18-A Launch Stability Enhancer - radial
         FTX-2 External Fuel Duct - radial
         LV-909 Liquid Fuel Engine - axial
         Aerodynamic Nose Cone - axial
    LT-1 Landing Struts - radial
    LT-1 Landing Struts - radial
  Mk16 Parachute - axial

.. _api-parts-fuel-lines:

Fuel Lines
----------

.. figure:: /images/api/parts-fuel-lines.png
   :align: right
   :figwidth: 200

   **Figure 5** -- Fuel lines from the example in Figure 1. Fuel flows from the
   parts highlighted in green, into the part highlighted in blue.

.. figure:: /images/api/parts-fuel-lines-tree.png
   :align: right
   :figwidth: 200

   **Figure 4** -- A subset of the parts tree from Figure 2 above.

Fuel lines are considered parts, and are included in the parts tree (for
example, as pictured in Figure 4). However, the parts tree does not contain
information about which parts fuel lines connect to. The parent part of a fuel
line is the part from which it will take fuel (as shown in Figure 4) however the
part that it will send fuel to is not represented in the parts tree.

Figure 5 shows the fuel lines from the example vessel pictured earlier. Fuel
line part 15 (in red) takes fuel from a fuel tank (part 11 -- in green) and
feeds it into another fuel tank (part 9 -- in blue). The fuel line is therefore
a child of part 11, but its connection to part 9 is not represented in the tree.

The attributes :attr:`Part.FuelLinesFrom` and :attr:`Part.FuelLinesTo` can be
used to discover these connections. In the example in Figure 5, when
:attr:`Part.FuelLinesTo` is called on fuel tank part 11, it will return a list
of parts containing just fuel tank part 9 (the blue part). When
:attr:`Part.FuelLinesFrom` is called on fuel tank part 9, it will return a list
containing fuel tank parts 11 and 17 (the parts colored green).

.. _api-parts-staging:

Staging
-------

.. figure:: /images/api/parts-staging.png
   :align: right
   :figwidth: 340

   **Figure 6** -- Example vessel from Figure 1 with a staging sequence.

Each part has two staging numbers associated with it: the stage in which the
part is *activated* and the stage in which the part is *decoupled*. These values
can be obtained using :attr:`Part.Stage` and :attr:`Part.DecoupleStage`
respectively. For parts that are not activated by staging, :attr:`Part.Stage`
returns -1. For parts that are never decoupled, :attr:`Part.DecoupleStage`
returns a value of -1.

Figure 6 shows an example staging sequence for a vessel. Figure 7 shows the
stages in which each part of the vessel will be *activated*. Figure 8 shows the
stages in which each part of the vessel will be *decoupled*.

.. container:: clearer

   ..

.. figure:: /images/api/parts-staging-activate.png
   :align: left
   :figwidth: 250

   **Figure 7** -- The stage in which each part is *activated*.

.. figure:: /images/api/parts-staging-decouple.png
   :align: right
   :figwidth: 250

   **Figure 8** -- The stage in which each part is *decoupled*.

.. container:: clearer

   ..

.. _PartModule:
   http://wiki.kerbalspaceprogram.com/wiki/CFG_File_Documentation#MODULES>`
.. _PartModules: http://wiki.kerbalspaceprogram.com/wiki/CFG_File_Documentation#MODULES>`
.. _Docking Port Alignment Indicator: http://forum.kerbalspaceprogram.com/threads/43901-0-90-Docking-Port-Alignment-Indicator-%28Version-6-1-Updated-03-07-2015%29
