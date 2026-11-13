.. _service-api-other-mods:

Wrapping Another Mod
====================

A service that wraps another mod binds to it by name as the game starts, and so runs whether or not
the mod is installed. A subclass of ``KRPC.Utils.ModAPI`` holds the binding, and
:cs:attr:`RequiresMod` names it on the service:

.. literalinclude:: /scripts/extending/other-mods/WindService.lib.cs
   :language: csharp
   :start-after: [docs]
   :end-before: [/docs]
   :dedent:

The constructor gives the mod's name and the name of its main assembly. The server keeps one
instance of each subclass. It calls ``Bind`` once, as the game finishes loading, when the game has
loaded the mod's assembly. The constructor only names the mod, as it can run before the game has
loaded.

``Bind`` sets static fields through the ``ModBinder``. Its ``Type`` method finds a type of the mod
by its full name, and binds a getter, setter, field, method or constructor of it. A type or member
missing from the installed version binds a delegate that throws when called. Only the procedures
that use it fail. Bind through ``mod.Require`` to fail the whole integration instead. An exception
from ``Bind`` does the same.

``NotReady`` gives the reason the mod cannot serve a call yet, such as before it creates its
instance in a scene, or null once it can. Each call checks it once the integration is available.

.. cs:attribute:: RequiresMod (params System.Type[] Types)

   The ``ModAPI`` types a service, class, method or property needs, which must all be
   available. A call fails while a mod it needs is not installed, its integration failed to load
   or the mod is not ready, with the reason, such as "Wind is not available: not installed". The
   check comes before the :ref:`game scene check <service-api-game-scenes>`.

   Several attributes on one member mean any one of them will do, for a member that works with
   either of two mods.

   A class inherits the attributes of its service, and a method or property those of its class.
   The nearest attributes apply. An extension member takes them from the member, then from the
   class that declares it, then from the class it extends.

   A static property of the service named ``Available`` is exempt, as it reports whether the mod
   can be used. Give it ``GameScene = GameScene.All`` to report this in every game scene.

A member with a fallback for when the mod is missing calls ``ModAPI.Get<WindAPI> ().CheckInstalled
()`` in place of the attribute. It returns false when the mod is not installed, and throws when the
mod is installed but its integration failed to load.
