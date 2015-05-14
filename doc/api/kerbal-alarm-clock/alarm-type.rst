AlarmType
=========

.. class:: AlarmType

   The type of an alarm.

   .. data:: Raw

      An alarm for a specific date/time or a specific period in the future.

   .. data:: Maneuver
   .. data:: ManeuverAuto

      An alarm based on the next maneuver node on the current ships flight
      path. This node will be stored and can be restored when you come back to
      the ship.

   .. data:: Apoapsis

      An alarm for furthest part of the orbit from the planet.

   .. data:: Periapsis

      An alarm for nearest part of the orbit from the planet.

   .. data:: AscendingNode

      Ascending node for the targeted object, or equatorial ascending node.

   .. data:: DescendingNode

      Descending node for the targeted object, or equatorial descending node.

   .. data:: Closest

      An alarm based on the closest approach of this vessel to the targeted
      vessel, some number of orbits into the future.

   .. data:: Contract
   .. data:: ContractAuto

      An alarm based on the expiry or deadline of contracts in career modes.

   .. data:: Crew

   .. data:: Distance

   .. data:: EarthTime

      An alarm based on the time in the "Earth" alternative Universe (aka the Real
      World).

   .. data:: LaunchRendevous

      An alarm that fires as your landed craft passes under the orbit of your
      target.

   .. data:: SOIChange
   .. data:: SOIChangeAuto

      An alarm manually based on when the next SOI point is on the flight path
      or set to continually monitor the active flight path and add alarms as it
      detects SOI changes.

   .. data:: Transfer
   .. data:: TransferModelled

      An alarm based on Interplanetary Transfer Phase Angles -- i.e. when should
      I launch to planet X? Based on Kosmo Not's post and used in Olex's
      Calculator.
