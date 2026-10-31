Deprecating Service Members
===========================

To deprecate part of a service's API, annotate it with the standard C# ``System.Obsolete``
attribute. This can be applied to services, procedures, properties, classes, methods and
enumerations. For example:

.. literalinclude:: /scripts/extending/deprecation/Deprecated.lib.cs
   :language: csharp
   :start-after: [docs]
   :end-before: [/docs]
   :dedent:

The deprecation, along with the message from the attribute, is included in the service definition
and sent to clients over the wire, as the ``deprecated`` and ``deprecated_reason`` fields returned
by ``KRPC.GetServices``. Any ``<see cref='...'/>`` references in the message are resolved to the
name of the referenced member; unqualified names are resolved against the declaring type. Clients then expose the deprecation using each language's own
mechanism: for example, the Python client emits a ``DeprecationWarning`` when a deprecated member
is called, and the generated C#, C++, C-nano and Java client code marks deprecated members with
``[Obsolete]``, ``[[deprecated]]``, ``KRPC_DEPRECATED`` and ``@Deprecated`` respectively.
