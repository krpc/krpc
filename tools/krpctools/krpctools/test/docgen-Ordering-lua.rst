.. default-domain:: lua
.. highlight:: lua

.. currentmodule:: ServiceA


.. module:: ServiceA

A service whose procedures use another service's enumeration.


.. staticmethod:: frob(mode?: ServiceB.Mode)

   Defaults to a member of another service's enumeration.

   :param mode: Defaults to ``ServiceB.Mode.fast``.




.. staticmethod:: frob_all(modes?: List)

   Defaults to a collection containing a member of another service's enumeration.

   :param modes: Defaults to ``{ServiceB.Mode.fast}``.
