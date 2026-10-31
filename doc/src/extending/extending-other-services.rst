.. _service-api-extension-members:

Extending Other Services
========================

A mod can add methods and properties to a class belonging to another service, as
:cs:attr:`KRPCClass` lets it add a class to one. A ``TestFlight`` mod can add
``vessel.parts.with_failure(...)`` to ``SpaceCenter.Parts``. The member is written as a C# extension
method, and joins the service that owns the type being extended:

.. literalinclude:: /scripts/extending/extending-other-services/ExtensionMethod.lib.cs
   :language: csharp
   :start-after: [docs]
   :end-before: [/docs]
   :dedent:

The containing class needs no kRPC attribute, only ``public static``. A dynamic client, such as
Python or Lua, exposes the member as an ordinary member of the class it extends. A static client,
such as C#, C++, C-nano or Java, reaches it once its code is regenerated from the mod's assembly
and the assembly declaring the service being extended; see :ref:`service-api-clientgen`.

C# has no extension properties, so a property is a pair of extension methods named ``GetX`` and
``SetX``, each carrying :cs:attr:`KRPCProperty`. The getter takes only the instance and returns
a value. The setter takes the instance and the value to set, and returns nothing. Either one on its
own gives a read-only or a write-only property.

.. literalinclude:: /scripts/extending/extending-other-services/ExtensionProperty.lib.cs
   :language: csharp
   :start-after: [docs]
   :end-before: [/docs]
   :dedent:

An extension member must satisfy the following criteria:

* The method must be a ``public static`` C# extension method, declared in a ``public static`` class
  outside any :cs:attr:`KRPCService`. A class that is not public puts its members out of reach,
  and the server names it in a warning in the log.

* The type it extends must be a :cs:attr:`KRPCClass`. A :cs:attr:`KRPCStruct` is a value
  with a fixed set of fields, and cannot be extended.

* The name of the member must be a valid :ref:`kRPC identifier <service-api-identifiers>`, and must
  not clash with a member the class already has, or with another extension member. The getter and
  the setter of an extension property must be declared in the same class.

* The parameter types and return type must be :ref:`types that kRPC can serialize
  <service-api-serializable-types>`, and may name types from any service.

Deprecation and the ``Nullable`` parameter of the attribute work as they do for a member declared
in the class. A setter's value is an ordinary parameter, so mark it nullable with
:cs:attr:`KRPCNullable`. The ``GameScene`` parameter defaults to the game scenes of the class
being extended.

Exposing a Part Module as a Class
---------------------------------

A mod that adds a ``PartModule`` to a part can expose it as a class of its own, in place of the
string-keyed ``SpaceCenter.Module`` API. Declare a :cs:attr:`KRPCClass` wrapping the module, and
add a nullable extension property on ``SpaceCenter.Part`` that returns it. The wrapper names the
module with a ``KRPC.SpaceCenter.ModuleRef``, as ``SpaceCenter.Module`` does:

.. literalinclude:: /scripts/extending/extending-other-services/PartModuleClass.lib.cs
   :language: csharp
   :start-after: [docs]
   :end-before: [/docs]
   :dedent:

The server keeps the object in a store keyed on the object itself, so the rules the ``SpaceCenter``
classes follow apply here too. A ``ModuleRef`` names one of a part's modules by its class and its
position among the part's modules of that class, which outlives any one of them. The wrapper holds
the part and the reference, and finds the module again on each access.

``Equals`` and ``GetHashCode`` read that identity alone, never a resolved game object, and build the
hash with ``KRPC.Utils.Hash``. The extension property builds a wrapper on each call, so value
identity is what keeps the store to one entry for the module.

``ModuleRef.Get`` raises ``KRPC.Service.KRPC.ObjectDestroyedException`` once the part no longer has
the module. ``ModuleRef.StateOn`` gives the module the state of its part, which is what lets the
store drop the object once the game destroys the part.

The reference records where it last found the module, so the field holding one is declared without
``readonly``. A ``readonly`` field is copied on each access, and the record is written to the copy.

Clients reach the module as ``part.failure_module.failed``.
