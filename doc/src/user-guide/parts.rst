.. _user-guide-parts:

Parts
=====

A vessel is made of parts, connected to one another in a tree. This guide shows how to walk that
tree, how fuel lines fit into it, and how the staging numbers of each part relate to the staging
sequence. It then shows how to reach any part module, including those of mods with no kRPC
service. The examples use the vessel below, whose craft file you can
:download:`download here </crafts/PartsTree.craft>`.

.. container:: clearer

   ..

.. figure:: /images/user-guide/parts/parts.png
   :align: left
   :figwidth: 275

   **Figure 1** -- The parts that make up the example vessel.

.. figure:: /images/user-guide/parts/parts-tree.png
   :align: right
   :figwidth: 275

   **Figure 2** -- The tree of parts for the vessel in Figure 1. Arrows point from each parent
   part to its children.

.. container:: clearer

   ..

.. _user-guide-parts-tree:

Traversing the Tree
-------------------

``Parts.Root`` is the root of the tree, the vessel's root part: part 1, the command pod, in the
example. ``Part.Children`` gives the parts attached to a part, and is empty for a part with
none. ``Part.Parent`` gives the part it is attached to, and is null for the root part.

This example walks the tree depth first, and prints each part indented by its depth:

.. tabs::

   .. group-tab:: Python

      .. literalinclude:: /scripts/user-guide/parts/TreeTraversal.py
         :language: python

   .. group-tab:: C#

      .. literalinclude:: /scripts/user-guide/parts/TreeTraversal.cs
         :language: csharp

   .. group-tab:: C++

      .. literalinclude:: /scripts/user-guide/parts/TreeTraversal.cpp
         :language: cpp

   .. group-tab:: C

      .. literalinclude:: /scripts/user-guide/parts/TreeTraversal.c
         :language: c

   .. group-tab:: Java

      .. literalinclude:: /scripts/user-guide/parts/TreeTraversal.java
         :language: java

   .. group-tab:: Lua

      .. literalinclude:: /scripts/user-guide/parts/TreeTraversal.lua
         :language: lua

   .. group-tab:: JavaScript

      .. literalinclude:: /scripts/user-guide/parts/TreeTraversal.js
         :language: javascript

For the example vessel, it prints:

.. code-block:: none

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

.. _user-guide-parts-attachment-modes:

Attachment Modes
----------------

A part is attached to its parent either *axially*, on the end of the parent to form a stack, or
*radially*, on its side. In the example, the parachute (part 2) is attached axially to the command
pod (part 1), and the landing leg (part 5) radially to the fuel tank (part 4). The root part has no
parent, and counts as attached axially.

``Part.AxiallyAttached`` and ``Part.RadiallyAttached`` give the mode. This example walks the tree
as before, and prints each part's attachment mode:

.. tabs::

   .. group-tab:: Python

      .. literalinclude:: /scripts/user-guide/parts/AttachmentModes.py
         :language: python

   .. group-tab:: C#

      .. literalinclude:: /scripts/user-guide/parts/AttachmentModes.cs
         :language: csharp

   .. group-tab:: C++

      .. literalinclude:: /scripts/user-guide/parts/AttachmentModes.cpp
         :language: cpp

   .. group-tab:: C

      .. literalinclude:: /scripts/user-guide/parts/AttachmentModes.c
         :language: c

   .. group-tab:: Java

      .. literalinclude:: /scripts/user-guide/parts/AttachmentModes.java
         :language: java

   .. group-tab:: Lua

      .. literalinclude:: /scripts/user-guide/parts/AttachmentModes.lua
         :language: lua

   .. group-tab:: JavaScript

      .. literalinclude:: /scripts/user-guide/parts/AttachmentModes.js
         :language: javascript

For the example vessel, it prints:

.. code-block:: none

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

.. _user-guide-parts-fuel-lines:

Fuel Lines
----------

.. figure:: /images/user-guide/parts/parts-fuel-lines-tree.png
   :align: right
   :figwidth: 200

   **Figure 3** -- Part of the tree from Figure 2, with fuel line part 15.

.. figure:: /images/user-guide/parts/parts-fuel-lines.png
   :align: right
   :figwidth: 200

   **Figure 4** -- The fuel lines of the example vessel. Fuel flows from the parts in green into
   the part in blue.

A fuel line is a part in the tree, as Figure 3 shows. Its parent is the part it takes fuel from.
The part it feeds is not in the tree.

In Figure 4, fuel line part 15 takes fuel from fuel tank part 11, in green, and feeds fuel tank
part 9, in blue. The fuel line is a child of part 11, and nothing in the tree connects it to part 9.

``Part.FuelLinesTo`` and ``Part.FuelLinesFrom`` give these connections. For fuel tank part 11,
``Part.FuelLinesTo`` returns a list holding fuel tank part 9. For fuel tank part 9,
``Part.FuelLinesFrom`` returns fuel tank parts 11 and 17, both in green.

.. container:: clearer

   ..

.. _user-guide-parts-staging:

Staging
-------

.. figure:: /images/user-guide/parts/parts-staging.png
   :align: right
   :figwidth: 340

   **Figure 5** -- The example vessel with a staging sequence.

Each part has two stage numbers: the stage in which it is *activated*, from ``Part.Stage``, and
the stage in which it is *decoupled*, from ``Part.DecoupleStage``. Both are -1 for a part that
staging never activates or decouples.

Figure 5 shows a staging sequence for the example vessel. Figure 6 shows the stage in which each
part is activated, and Figure 7 the stage in which each part is decoupled.

.. container:: clearer

   ..

.. figure:: /images/user-guide/parts/parts-staging-activate.png
   :align: left
   :figwidth: 250

   **Figure 6** -- The stage in which each part is *activated*.

.. figure:: /images/user-guide/parts/parts-staging-decouple.png
   :align: right
   :figwidth: 250

   **Figure 7** -- The stage in which each part is *decoupled*.

.. container:: clearer

   ..

For totals per stage, such as delta-v, thrust and fuel, use ``Vessel.Stages`` and
``Vessel.DecoupleStages``, which return a ``Stage`` for each stage.

.. _user-guide-parts-modules:

Part Modules
------------

A part's behavior comes from its modules, such as ``ModuleEngines`` for an engine. kRPC wraps the
common modules in classes like ``Engine`` and ``SolarPanel``. Every module, including those of a
mod, is also available directly, through ``Part.Modules`` or ``Parts.ModulesWithName``. A module
has three lists:

* ``Module.FieldList``: the module's fields, such as an engine's thrust limiter. The right-click
  menu shows the fields whose ``PartField.Visible`` is true.
* ``Module.EventList``: the module's events, the buttons in the right-click menu. The menu shows
  the events whose ``PartEvent.Visible`` and ``PartEvent.Active`` are both true.
* ``Module.ActionList``: the module's actions, the ones an action group can hold.

This example reads the engine fields the right-click menu shows, limits the thrust of each engine,
and extends the solar panels:

.. literalinclude:: /scripts/user-guide/parts/PartModules.py
   :language: python
   :start-after: [docs]
   :end-before: [/docs]
   :dedent:

``PartField.Name``, ``PartEvent.Name`` and ``PartAction.Name`` are the identifiers in the module's
code, which stay the same across game versions and languages. The names the menu shows are
``GuiName``, and they change with the game's language.

``PartField.Type`` gives the type of a field, and the matching property reads and sets it, such as
``PartField.FloatValue``. ``PartField.Value`` reads any field as a string, and sets string fields
only. Setting a field works as the right-click menu does, so the module reacts to the change, such
as B9PartSwitch switching a part's subtype. ``PartField.Reset`` restores the value the field had
when the part was loaded.

``PartEvent.Trigger`` clicks an event's button, and ``PartAction.Activated`` triggers an action as
its action group does. ``Module.Config`` gives the module's configuration from the part's
configuration file, for values the module does not expose as fields.
