.. default-domain:: lua
.. highlight:: lua

.. currentmodule:: ServiceA


.. module:: ServiceA

A service whose procedures use another service's enumeration.


.. staticmethod:: frob([mode = ServiceB.Mode.fast])

   Defaults to a member of another service's enumeration.

   :param ServiceB.Mode mode:




.. staticmethod:: frob_all([modes = {ServiceB.Mode.fast}])

   Defaults to a collection containing a member of another service's enumeration.

   :param List modes:
