.. _service-api-docgen:

API Reference Pages
===================

``krpc-docgen`` generates the API reference of a service, in the form of kRPC's own API reference.
It writes one page per template for a client language, as reStructuredText for
`Sphinx <https://www.sphinx-doc.org>`_. It is part of the :doc:`krpctools </krpctools>` Python
package:

.. code-block:: sh

   pip install krpctools

Templates
---------

A template is the source of a page. Here is one for the LaunchControl service from the
:doc:`example </extending>`:

.. literalinclude:: /scripts/extending/docgen/launch-control.tmpl
   :language: jinja

The first four lines set up the page for the client language. ``macros.service`` documents the
service's procedures and properties. A class, enumeration, structure or exception has a macro of
its own, such as ``macros.class(services['SpaceCenter'].classes['Vessel'])``. The templates are
`Jinja <https://jinja.palletsprojects.com>`_, so a page can hold any other text as well.

The ordering file lists each member of the service by its full name, in the order the pages
document them:

.. literalinclude:: /scripts/extending/docgen/order.txt
   :language: none

A member missing from the ordering file stops ``krpc-docgen``, which then lists every member
missing. Start with an empty file to get the full list.

Generating the Pages
--------------------

``krpc-docgen`` reads the service definitions, which ``krpc-servicedefs`` generates from the
service's assembly (see :ref:`service-api-clientgen`). Then it writes a page for each
``--page`` template:

.. code-block:: sh

   krpc-servicedefs --reference-dir=/path/to/ksp/KSP_Data/Managed -o LaunchControl.json \
       LaunchControl LaunchControl.dll
   krpc-docgen python order.txt LaunchControl.json \
       --page launch-control.tmpl python/launch-control.rst

Run ``krpc-docgen`` once for each language: ``cnano``, ``cpp``, ``csharp``, ``java``, ``lua`` or
``python``. Pass the definitions of every service the documentation links to, as well as the
service's own. ``--documented`` writes the full name of each member the pages document, to check
that none is left out.

The text of the pages is the service's :doc:`documentation </extending/documentation>`.
``krpc-servicedefs`` reads it from the XML documentation file beside the assembly. Build the
assembly with ``<GenerateDocumentationFile>true</GenerateDocumentationFile>`` to write that file.

Sphinx Setup
------------

The Python, C++ and C pages use the domains built into Sphinx. The C#, Java and Lua pages need the
`sphinx-csharp <https://pypi.org/project/sphinx-csharp>`_,
`sphinx-java <https://pypi.org/project/sphinx-java>`_ and
`sphinx-lua-ls <https://pypi.org/project/sphinx-lua-ls>`_ extensions. A service whose
documentation has units or formulas also needs ``sphinx.ext.mathjax``.
