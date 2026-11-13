.. default-domain:: js
.. highlight:: js

.. module:: ServiceA
   :no-index:


.. module:: ServiceA

A service whose procedures use another service's enumeration.


.. function:: frob([mode])

   Defaults to a member of another service's enumeration.

   :param mode: Defaults to ``ServiceB.Mode.fast``.
   :type mode: :class:`ServiceB.Mode`
   




.. function:: frobAll([modes])

   Defaults to a collection containing a member of another service's enumeration.

   :param modes: Defaults to ``[ServiceB.Mode.fast]``.
   :type modes: ``Array`` of :class:`ServiceB.Mode`
