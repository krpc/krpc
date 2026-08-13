.. default-domain:: c
.. highlight:: c




A service whose procedures use another service's enumeration.


.. function:: krpc_error_t krpc_ServiceA_Frob(krpc_connection_t connection, krpc_ServiceB_Mode_t mode)

   Defaults to a member of another service's enumeration.

   :Parameters:



   




.. function:: krpc_error_t krpc_ServiceA_FrobAll(krpc_connection_t connection, const krpc_list_enum_t * modes)

   Defaults to a collection containing a member of another service's enumeration.

   :Parameters:
