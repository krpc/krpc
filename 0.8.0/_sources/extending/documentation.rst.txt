.. _service-api-documentation:

Documentation
=============

Documentation can be added using `C# XML documentation
<https://msdn.microsoft.com/en-us/library/aa288481%28v=vs.71%29.aspx>`_. For dynamic clients, such
as the Python and Lua clients, the documentation will be automatically exported to clients when they
connect.

A ``<see cref='...'/>`` links to another member of a service, or to an exception:

.. literalinclude:: /scripts/extending/documentation/DocumentationCrefs.lib.cs
   :language: csharp
   :start-after: [docs]
   :end-before: [/docs]
   :dedent:

The reference can name a service, procedure, property, class, method, enumeration, structure or
exception. A .NET exception that kRPC maps, such as ``System.InvalidOperationException``, links to
the kRPC exception of the same name. The API reference and the generated client code link to the
same member.

``krpc-clientgen`` needs the definitions of every service the documentation links to, and warns
about a reference to a service it was not given.

Units and formulas are marked up with ``<unit>`` and ``<math>`` tags:

.. literalinclude:: /scripts/extending/documentation/Documentation.lib.cs
   :language: csharp
   :start-after: [docs]
   :end-before: [/docs]
   :dedent:

The API reference typesets them, and client documentation shows them as text, such as ``m/s²`` and
``N·m``. A unit is set upright, and a ``.`` in it joins two factors, as in ``N.m``. Both tags
hold a subset of LaTeX:

.. list-table::
   :header-rows: 1

   * - Markup
     - Example
   * - Superscripts and subscripts
     - ``m^3.s^{-2}``, ``\omega_{max}``
   * - Fractions and roots
     - ``\frac{1}{2}``, ``\sqrt{x + y}``
   * - Text
     - ``\text{air density}``
   * - Operators
     - ``\cdot``, ``\times``, ``\pm``, ``\le``, ``\ge``, ``\approx``, ``\infty``, ``\degree``
   * - Greek letters
     - ``\alpha`` to ``\omega``, and the capitals LaTeX names, such as ``\Delta``

A superscript or subscript must have a Unicode form, so ``^2`` and ``_{max}`` work but ``^q`` does
not. The documentation build rejects markup outside the subset, and ``krpc-clientgen`` warns about
it and writes it as text.
