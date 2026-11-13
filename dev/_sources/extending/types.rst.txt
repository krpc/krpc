Types
=====

.. _service-api-identifiers:

Identifiers
-----------

An identifier can only contain the ASCII letters and digits, and must start with an upper case
letter. They should follow `CamelCase <https://en.wikipedia.org/wiki/CamelCase>`_ capitalization
conventions. The names of parameters and enumeration values follow the same rule, but can also start
with a lower case letter. The ``_`` character is reserved for joining the parts of a procedure name,
such as ``Vessel_get_Name``.

Each client converts an identifier to its own case, by splitting it into words:

.. list-table::
   :header-rows: 1

   * - Identifier
     - Python
     - Java
   * - ``RCSThrust``
     - ``rcs_thrust``
     - ``rcsThrust``
   * - ``MeanAnomalyAtUT``
     - ``mean_anomaly_at_ut``
     - ``meanAnomalyAtUT``
   * - ``EVAs``
     - ``evas``
     - ``evas``
   * - ``Vector3D``
     - ``vector_3d``
     - ``vector3D``

A run of capitals is an acronym, and a run of capitals ending in ``s`` is a plural acronym. A word
with more than one capital, such as ``PeA``, splits at each capital unless it is a compound word,
such as ``UInt``. Spell it out, as in ``PeriapsisAltitude``. :ref:`client-design-name-case`
specifies the rules and lists the compound words.

Names are unique within their scope, compared case-insensitively. Some clients convert names that
differ only in case to one name: ``RCSEnabled`` and ``RcsEnabled`` are both ``rcs_enabled`` in
Python. The scopes are:

* the procedures and properties of a service
* the methods, static methods and properties of a class
* the values of an enumeration
* the fields of a structure
* the parameters of a procedure or method

A property ``Foo`` also takes the names ``GetFoo`` and ``SetFoo``, as some clients name its
getter and setter that way. A method ``GetFoo`` beside it is an error.

.. _service-api-serializable-types:

Serializable Types
------------------

A type can only be used as a parameter or return type if kRPC knows how to serialize it. The
following types are serializable:

* The C# types ``double``, ``float``, ``int``, ``long``, ``uint``, ``ulong``, ``bool``, ``string``
  and ``byte[]``

* Any type annotated with :cs:attr:`KRPCClass`

* Any type annotated with :cs:attr:`KRPCEnum`

* Any type annotated with :cs:attr:`KRPCStruct`

* Collections of serializable types:

  * ``System.Collections.Generic.IList<T>`` where ``T`` is a serializable type

  * ``System.Collections.Generic.IDictionary<K,V>`` where ``K`` is one of ``int``, ``long``,
    ``uint``, ``ulong``, ``bool`` or ``string`` and ``V`` is a serializable type

  * ``System.Collections.Generic.HashSet<V>`` where ``V`` is a serializable type

  * ``System.Tuple<...>`` where all of the tuple's value types are serializable types

* Return types can be ``void``

* Protocol buffer message types from namespace ``KRPC.Service.Messages``

.. _service-api-null-values:

Null Values
-----------

A parameter, a return value, a structure field or a position nested inside one of them may be
``null`` only when it is declared *nullable*. This applies uniformly to every serializable
type -- class types, ``string``, collections and value types alike. The server enforces it:

* Passing ``null`` as an argument for a parameter that is not nullable fails the call with an
  error.

* Returning ``null`` from a procedure, method or property getter that is not nullable fails the
  call with an error.

* A ``null`` in a structure field that is not nullable fails the call with an error, naming the
  field.

A **parameter** is nullable if any of the following hold:

* It is annotated with :cs:attr:`KRPCNullable`.

* It has a default value of ``null`` (for example ``Vessel target = null``). A null default is
  itself a declaration that ``null`` is a valid value, so no further annotation is needed.

* Its type is a nullable value type, ``Nullable<T>`` (for example ``int?``); see below.

A **return value** is nullable if the ``Nullable`` parameter of its :cs:attr:`KRPCProcedure`,
:cs:attr:`KRPCMethod` or :cs:attr:`KRPCProperty` attribute is set to true, or if its type
is ``Nullable<T>``. For a property, ``Nullable = true`` makes the getter's return value *and* the
setter's argument nullable.

Return nullability is always declared, as kRPC reads the attribute rather than the method body.

A **structure field** is nullable if the ``Nullable`` parameter of its
:cs:attr:`KRPCProperty` attribute is set, or if its type is ``Nullable<T>``. Setting
``Nullable`` on a value-typed field is an error. Write the field as ``Nullable<T>`` instead.

.. _service-api-nullable-value-types:

Nullable Value Types
^^^^^^^^^^^^^^^^^^^^

A parameter, return value or structure field of type ``Nullable<T>`` -- ``int?``, ``float?``,
``bool?``, an ``enum?`` and so on -- is automatically nullable; it needs neither
:cs:attr:`KRPCNullable` nor ``Nullable = true``. To clients it appears as an ordinary value of
type ``T`` that may be null.

Only value types can be ``Nullable<T>``. A `nullable reference type
<https://learn.microsoft.com/en-us/dotnet/csharp/nullable-references>`_ annotation such as
``string?`` or ``IList<int>?`` is a compile-time annotation, and the ``?`` is erased before kRPC
reads the type. Such a parameter or return value is treated as non-nullable.

Use :cs:attr:`KRPCNullable` or ``Nullable = true`` to make a reference-typed parameter,
return value or structure field nullable, and a path on :cs:attr:`KRPCNullable` to make a
reference-typed position nested inside one nullable. See :ref:`service-api-nullable-positions`.
