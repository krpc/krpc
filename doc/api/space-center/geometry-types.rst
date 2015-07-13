.. default-domain:: #echo $domain

Geometry Types
==============

.. class:: Vector3

   3-dimensional vectors are represented as a 3-tuple. For example:

   #if $language == 'python'

   .. code-block:: python

      import krpc
      conn = krpc.connect()
      v = conn.space_center.active_vessel.flight().prograde
      print(v[0], v[1], v[2])

   #else if $language == 'lua'

   .. code-block:: lua

      local krpc = require 'krpc.init'
      local conn = krpc.connect()
      local v = conn.space_center.active_vessel:flight().prograde
      print(v[1], v[2], v[3])

   #end if

.. class:: Quaternion

   Quaternions (rotations in 3-dimensional space) are encoded as a 4-tuple
   containing the x, y, z and w components. For example:

   #if $language == 'python'

   .. code-block:: python

      import krpc
      conn = krpc.connect()
      q = conn.space_center.active_vessel.flight().rotation
      print(q[0], q[1], q[2], q[3])

   #else if $language == 'lua'

   .. code-block:: lua

      local krpc = require 'krpc.init'
      local conn = krpc.connect()
      local q = conn.space_center.active_vessel:flight().rotation
      print(q[1], q[2], q[3], q[4])

   #end if
