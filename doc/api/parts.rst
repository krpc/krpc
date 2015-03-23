Parts
=====

.. class:: Parts

   Instances of this class are used to interact with the parts of a vessel. An
   instance can be obtained by calling :attr:`Vessel.Parts`.

   .. attribute:: Parts.All

      Gets a list of all of the vessels parts.

      :type: :class:`List` ( :class:`Part` )

   .. attribute:: Parts.Root

      Gets the vessels root part.

      :rtype: :class:`Part`

   .. method:: Parts.WithName (name)

      Gets a list of parts whose :attr:`Part.Name` is *name*.

      :param string name: The name of the parts
      :rtype: :class:`List` ( :class:`Part` )

   .. method:: Parts.WithTitle (title)

      Gets a list of all parts whose :attr:`Part.Title` is *title*.

      :param string name: The title of the parts
      :rtype: :class:`List` ( :class:`Part` )

   .. method:: Parts.WithModule (moduleName)

      Gets a list of all parts that contain a :class:`Module` whose
      :attr:`Module.Name` is *moduleName*.

      :param string moduleName: The module name
      :rtype: :class:`List` ( :class:`Part` )

   .. method:: Parts.InStage (stage)

      .. warning:: Not yet implemented

      :rtype: :class:`List` ( :class:`Part` )

   .. method:: Parts.ModulesWithName (moduleName)

      Gets a list of modules (combined across all parts in the vessel) whose
      :attr:`Module.Name` is *moduleName*.

      :rtype: :class:`List` ( :class:`Module` )

   .. method:: Parts.ModulesInStage (stage)

      .. warning:: Not yet implemented

      :rtype: :class:`List` ( :class:`Module` )

   .. attribute:: Parts.Engines

      Gets a list of all engines in the vessel.

      :rtype: :class:`List` ( :class:`Engine` )

   .. attribute:: Parts.SolarPanels

      Gets a list of all solar panels in the vessel.

      :rtype: :class:`List` ( :class:`SolarPanel` )

   .. attribute:: Parts.Sensors

      Gets a list of all sensors in the vessel.

      :rtype: :class:`List` ( :class:`Sensor` )

   .. attribute:: Parts.Decouplers

      Gets a list of all decouplers in the vessel.

      :rtype: :class:`List` ( :class:`Decoupler` )

   .. attribute:: Parts.Lights

      Gets a list of all lights in the vessel.

      :rtype: :class:`List` ( :class:`Light` )

   .. attribute:: Parts.Parachutes

      Gets a list of all parachutes in the vessel.

      :rtype: :class:`List` ( :class:`Parachute` )

Part
----

.. class:: Part

   Instances of this class represents a part. A vessel is made of multiple
   parts. Instances can be obtained by various methods in :class:`Parts`.

   .. attribute:: Part.Name

      Internal name of the part, as used in `part cfg files
      <http://wiki.kerbalspaceprogram.com/wiki/CFG_File_Documentation>`_. For
      example "Mark1-2Pod".

      :rtype: string

   .. attribute:: Part.Title

      Title of the part, as shown when the part is right clicked in-game. For
      example "Mk1-2 Command Pod".

      :rtype: string

   .. attribute:: Part.Cost

      Gets the cost of the part, in units of funds.

      :rtype: float

   .. attribute:: Part.Vessel

      The vessel this contains this part.

      :rtype: :class:`Vessel`

   .. attribute:: Part.Parent

      The parts parent. Returns ``null`` if the part does not have a
      parent. This, in combination with :attr:`Part.Children`, can be used to
      traverse the vessels parts tree.

      :rtype: :class:`Part`

   .. attribute:: Part.Children

      The parts children. Returns an empty list if the part has no
      children. This, in combination with :attr:`Part.Parent`, can be used to
      traverse the vessels parts tree.

      :rtype: :class:`List` ( :class:`Part` )

   .. attribute:: Part.State

      The current state of the part.

      :rtype: :class:`PartState`

   .. attribute:: Part.Stage

      .. warning:: Not yet implemented

      :rtype: int

   .. attribute:: Part.DecoupleStage

      .. warning:: Not yet implemented

      :rtype: int

   .. attribute:: Part.Massless

      Gets whether the part is `"massless"
      <http://wiki.kerbalspaceprogram.com/wiki/Massless_part>`_ -- returning
      ``True`` if it is, ``False`` otherwise.

      :rtype: bool

   .. attribute:: Part.Mass

      Gets the current mass of the part, including resources it contains, in
      kilograms. Returns zero if the part is massless.

      :rtype: float

   .. attribute:: Part.DryMass

      Gets the mass of the part, not including any resources it contains, in
      kilograms. Returns zero if the part is massless.

      :rtype: float

   .. attribute:: Part.ImpactTolerance

      Gets the impact tolerance of the part, in meters per second.

      :rtype: float

   .. attribute:: Part.Temperature

      Gets the current temperature of the part, in Kelvin.

      :rtype: float

   .. attribute:: Part.MaxTemperature

      Gets the maximum temperature that the part can survive, in Kelvin.

      :rtype: float

   .. attribute:: Part.Resources

      .. todo:: Not implemented correctly

      :rtype: :class:`PartResources`

   .. attribute:: Part.Modules

      Gets the modules for this part.

      :rtype: :class:`List` ( :class:`Module` )

   .. attribute:: Part.Engine

      An :class:`Engine` if the part is an engine, otherwise ``null``.

      :rtype: :class:`Engine`

   .. attribute:: Part.SolarPanel

      A :class:`SolarPanel` if the part is a solar panel, otherwise ``null``.

      :rtype: :class:`SolarPanel`

   .. attribute:: Part.Sensor

      A :class:`Sensor` if the part is a sensor, otherwise ``null``.

      :rtype: :class:`Sensor`

   .. attribute:: Part.Decoupler

      A :class:`Decoupler` if the part is a decoupler, otherwise ``null``.

      :rtype: :class:`Decoupler`

   .. attribute:: Part.Light

      A :class:`Light` if the part is a light, otherwise ``null``.

      :rtype: :class:`Light`

   .. attribute:: Part.Parachute

      A :class:`Parachute` if the part is a parachute, otherwise ``null``.

      :rtype: :class:`Parachute`

Module
------

.. class:: Module

   In KSP, each part has zero or more `PartModules`_ associated with it. Each
   one contains some of the functionlity of the part. For example, an engine has
   a "ModuleEngines" PartModule that contains all the functionality of an
   engine.

   This class allows you to interact with KSPs PartModules, and any PartModules
   that have been added by other mods.

   .. attribute:: Module.Name

      Name of the `PartModule`_.
      For example, "ModuleEngines".

      :rtype: string

   .. attribute:: Module.Part

      The part that contains this module.

      :rtype: :class:`Part`

   .. attribute:: Module.Fields

      The modules field names and their associated values, as a
      dictionary. These are the values visible in the right-click menu of the
      part.

      :rtype: :class:`Dictionary` ( string , string )

   .. method:: Module.HasField (name)

      Returns ``true`` if the module has a field with the given name.

      :param string name: name of the field
      :rtype: bool

   .. method:: Module.GetField (name)

      Returns the value of a field.

      :param string name: name of the field
      :rtype: string

   .. attribute:: Module.Events

      A list of the names of all of the modules events. Events are the clickable
      buttons visible in the right-click menu of the part.

      :rtype: :class:`List` ( string )

   .. method:: Module.HasEvent (name)

      True if the module has an event with the given name.

      :rtype: bool

   .. method:: Module.TriggerEvent (name)

      Trigger the named event. Equivalent to clicking the button in the
      right-click menu of the part.

   .. attribute:: Module.Actions

      A list of all the names of the modules actions. These are the parts actions that
      can be assigned to action groups in the in-game editor.

      :rtype: :class:`List` ( string )

   .. method:: Module.HasAction (name)

      True if the part has an action with the given name.

      :rtype: bool

   .. method:: Module.SetAction (name, [value = true])

      Set the value of an action with the given name.

Engine
------

.. class:: Engine

   .. attribute:: Part

      Part object for this engine.

      :rtype: :class:`Part`

Solar Panel
-----------

.. class:: SolarPanel

   .. attribute:: Part

      Part object for this solar panel.

      :rtype: :class:`Part`

Sensor
------

.. class:: Sensor

   .. attribute:: Part

      Part object for this sensor.

      :rtype: :class:`Part`

Decoupler
---------

.. class:: Decoupler

   .. attribute:: Part

      Part object for this decoupler.

      :rtype: :class:`Part`

Light
-----

.. class:: Light

   .. attribute:: Part

      Part object for this light.

      :rtype: :class:`Part`

Parachute
---------

.. class:: Parachute

   .. attribute:: Part

      Part object for this parachute.

      :rtype: :class:`Part`

.. _PartModule: http://wiki.kerbalspaceprogram.com/wiki/CFG_File_Documentation#MODULES>`
.. _PartModules: http://wiki.kerbalspaceprogram.com/wiki/CFG_File_Documentation#MODULES>`
