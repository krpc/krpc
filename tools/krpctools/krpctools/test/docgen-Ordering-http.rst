.. default-domain:: http
.. highlight:: json




.. _http-ServiceA:

A service whose procedures use another service's enumeration.


.. http:post:: /service-a/frob

   Defaults to a member of another service's enumeration.


   :reqjson ServiceB.Mode mode: Optional, defaults to ``"fast"``.





.. http:post:: /service-a/frob-all

   Defaults to a collection containing a member of another service's enumeration.


   :reqjson list<ServiceB.Mode> modes: Optional, defaults to ``["fast"]``.
