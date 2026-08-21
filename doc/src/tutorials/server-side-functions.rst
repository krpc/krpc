.. currentmodule:: KRPC

Server Side Functions
=====================

Server side functions let you build code that runs *inside* the game, rather than in your
program. This has three main uses:

* **Custom events**: wait for a condition to become true. The condition is checked on the
  server every physics tick, and a message is only sent to your program when it fires. Without
  server side functions, your program would have to poll the condition with repeated remote
  procedure calls, adding network latency to every check — and possibly missing a condition
  that only holds for a few ticks.
* **Expression streams**: stream the result of a computation over multiple RPC values. All of
  the values are read in the same physics tick, so they are consistent with one another, and
  only the combined result is sent to your program, only when it changes.
* **Run-once functions**: run a sequence of calls and computation — including side effects,
  such as setting properties and triggering actions — in a single physics tick, started by a
  single RPC. Without server side functions, each step would be a separate round trip, with
  the game advancing between steps.

This tutorial is in two parts. :ref:`Compiled functions <expressions-compiled>` covers the
most convenient way to use the feature — writing the code in your own language and having the
client compile it — which is available in Python and C#. :ref:`The expression API
<expressions-api>` covers the underlying API that compiled code translates into, which is what
the C++ and Java clients use directly, and which explains what functions can and cannot do.

.. note::

   Expressions are consumed through streams, so they are not available in the Lua and C-nano
   clients, which do not support streams.

How server side functions work
------------------------------

A server side function is a tree of *expression objects* held on the server. Each node of the
tree is a constant, an embedded remote procedure call, an operator applied to other
expressions, a statement — such as an assignment, a loop or an if statement — or a nested
function. When the function is first evaluated, the server compiles the tree to native code,
so evaluating it afterwards is fast.

Three RPCs consume these trees:

* ``AddEvent`` takes an expression that evaluates to a boolean, and returns an event object.
  The server evaluates the expression on each stream update and triggers the event when it
  returns true.
* ``AddExpressionStream`` takes an expression of any type that can be sent to a client —
  numbers, strings, collections or objects — and returns a stream. The value is recomputed on
  each stream update and sent to the client when it changes.
* ``RunFunction`` evaluates an expression *once*, within a single physics tick, and returns
  the value it produces. This is the right consumer for functions with side effects: an event
  or stream re-evaluates its expression on every update, repeating the effects each tick.

Two properties of expressions are worth keeping in mind throughout this tutorial:

* **Remote procedure calls embedded in an expression are re-invoked on every evaluation.**
  Everything else — constants, and any values your program supplies when building the
  expression — is fixed when the expression is created.
* **The whole expression is evaluated within a single physics tick.** If an expression reads
  the fuel level of every engine on a vessel, all of the reads happen in the same tick; the
  values cannot change part way through, as they could if your program made the same calls
  itself.

.. _expressions-compiled:

Compiled functions
------------------

The Python and C# clients can build the server side trees automatically, by compiling a
function or lambda that takes no arguments. This is the recommended way to use server side
functions in those languages: the code reads exactly like the client side code it replaces.

A first custom event
^^^^^^^^^^^^^^^^^^^^

The following waits until the vessel's altitude exceeds 1000m. The condition is checked on the
server each physics tick, with no network traffic while waiting:

.. tabs::

   .. group-tab:: C#

      .. code-block:: csharp

         var evnt = connection.AddEvent (() => flight.MeanAltitude > 1000);
         lock (evnt.Condition) {
             evnt.Wait ();
             Console.WriteLine ("Altitude reached 1000m");
         }

   .. group-tab:: Python

      .. code-block:: python

         event = conn.add_event(lambda: flight.mean_altitude > 1000)
         with event.condition:
             event.wait()
             print("Altitude reached 1000m")

The lambda is not run by your program. Instead, the client inspects it and builds an
equivalent server side expression: the ``mean_altitude`` call becomes an embedded RPC,
re-invoked by the server on each check, and ``1000`` becomes a constant.

Remote calls and captured values
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

The compiler splits the function's code into two kinds of sub-expression:

* Anything that interacts with the server — reading a property or calling a method of a remote
  object or service — becomes part of the expression, and is re-evaluated by the server on
  every evaluation.
* Everything else — captured variables, literals, arithmetic on plain values, calls to
  library functions like ``math.sqrt`` — is evaluated *once*, when the expression is compiled,
  and embedded as a constant.

For example:

.. tabs::

   .. group-tab:: C#

      .. code-block:: csharp

         var threshold = 50000;
         var evnt = connection.AddEvent (
             () => vessel.Orbit.ApoapsisAltitude > threshold * 2);

   .. group-tab:: Python

      .. code-block:: python

         threshold = 50000
         event = conn.add_event(lambda: vessel.orbit.apoapsis_altitude > threshold * 2)

Here ``threshold * 2`` is computed by the client when the event is created, and the expression
compares the apoapsis against the constant 100000. If ``threshold`` changes later, the event
does not change — to pick up the new value, compile a new expression. The apoapsis, on the
other hand, is read by the server on every check.

Chains of calls also work as expected: both the ``orbit`` call and the ``apoapsis_altitude``
call are embedded in the expression, so the expression follows the vessel's *current* orbit
object each tick, even if the vessel moves to a different orbit.

Streaming computed values
^^^^^^^^^^^^^^^^^^^^^^^^^

Passing a function when creating a stream compiles it and streams the computed value. The
computation runs on the server on each stream update, and the result is only sent to your
program when it changes:

.. tabs::

   .. group-tab:: C#

      .. code-block:: csharp

         // The vessel's altitude, in kilometers, computed on the server
         var stream = connection.AddStream (() => flight.MeanAltitude / 1000);
         Console.WriteLine ("Altitude: " + stream.Get () + " km");

   .. group-tab:: Python

      .. code-block:: python

         # The vessel's altitude, in kilometers, computed on the server
         stream = conn.add_expression_stream(lambda: flight.mean_altitude / 1000)
         print("Altitude:", stream(), "km")

Expressions can produce collections as well as single values. This streams the thrust of every
engine on the vessel, read in a single physics tick per update:

.. tabs::

   .. group-tab:: C#

      .. code-block:: csharp

         var engines = vessel.Parts.Engines;
         var stream = connection.AddStream (
             () => engines.Select (engine => engine.Thrust).ToList ());
         foreach (var thrust in stream.Get ())
             Console.WriteLine (thrust);

   .. group-tab:: Python

      .. code-block:: python

         engines = vessel.parts.engines
         stream = conn.add_expression_stream(
             lambda: [engine.thrust for engine in engines]
         )
         for thrust in stream():
             print(thrust)

Note the difference between the two places the engine list could come from. Here ``engines``
is a captured variable: the *list of engines* is fixed when the expression is compiled, and
only their thrusts are re-read each update. Writing
``vessel.parts.engines`` *inside* the function instead embeds the engine list call in the
expression, so the server re-fetches the list each update — following staging events, for
example, at the cost of extra work per update.

Working with collections
^^^^^^^^^^^^^^^^^^^^^^^^

Calls can be applied to each element of a collection, entirely on the server. In Python,
comprehensions and the builtin functions ``len``, ``sum``, ``min``, ``max``, ``any``, ``all``
and ``sorted`` are compiled to the server's collection operations; in C#, the LINQ operators
are. The following creates an event that fires when any engine on the vessel runs out of
fuel — including engines revealed by staging, since the engine list is re-read each check:

.. tabs::

   .. group-tab:: C#

      .. code-block:: csharp

         var evnt = connection.AddEvent (
             () => vessel.Parts.Engines.Any (engine => !engine.HasFuel));
         lock (evnt.Condition) {
             evnt.Wait ();
             Console.WriteLine ("An engine has run out of fuel");
         }

   .. group-tab:: Python

      .. code-block:: python

         event = conn.add_event(
             lambda: any(not engine.has_fuel for engine in vessel.parts.engines)
         )
         with event.condition:
             event.wait()
             print("An engine has run out of fuel")

In Python, a plain function can be compiled as well as a lambda, and may contain simple
assignments before its return statement:

.. code-block:: python

   def fuel_fraction():
       resources = vessel.resources
       amount = resources.amount("LiquidFuel")
       capacity = resources.max("LiquidFuel")
       return amount / capacity

   stream = conn.add_expression_stream(fuel_fraction)

Statements, loops and local variables
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

In Python, compiled functions are not limited to a single expression: full statement bodies
compile too, including ``if``/``elif``/``else``, ``while`` and ``for`` loops with ``break``
and ``continue``, early returns, and local variables — created, reassigned and mutated, for
example to build up a collection in a loop. Local variables take their type from their first
assignment; annotate assignments of empty collections so their type is known:

.. code-block:: python

   def burn_ratings():
       ratings: list[str] = []
       for engine in vessel.parts.engines:
           if not engine.active:
               continue
           if engine.thrust > 0.9 * engine.available_thrust:
               ratings.append("full")
           else:
               ratings.append("partial")
       return ratings

   print(conn.run_function(burn_ratings))

(C# cannot compile statement bodies — the C# language only converts single-expression lambdas
to expression trees — so multi-statement functions in C# are built with the
:ref:`expression API <expressions-api>` instead.)

Side effects and running functions once
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

Compiled functions can also *change* things: assign to the properties of remote objects and
services, and call methods for their effects. A function with side effects should be run with
``run_function`` (``RunFunction`` in C#), which evaluates it exactly once, with every step
happening in the same physics tick:

.. tabs::

   .. group-tab:: C#

      .. code-block:: csharp

         // A single side-effecting call, run on the server
         connection.RunFunction (() => vessel.Control.ActivateNextStage ());

         // Compute and return a value in one tick
         var altitude = connection.RunFunction (() => flight.MeanAltitude / 1000);

   .. group-tab:: Python

      .. code-block:: python

         def deploy():
             for parachute in vessel.parts.parachutes:
                 if not parachute.deployed:
                     parachute.deploy()

         conn.run_function(deploy)

Running the function in a single tick is the point: unlike making the same calls from your
program, the game does not advance between the steps, and there is no network round trip per
step. A function that returns nothing gives ``None`` (or use the ``void`` overloads in the
statically typed clients).

.. warning::

   Passing a side-effecting function to an event or a stream re-runs its effects on *every*
   stream update. Use ``run_function`` for effects, and keep event and stream functions to
   pure computation.

Mathematics: the standard library
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

The ``StdLib`` service provides mathematics for use within server side functions: scalar
functions (square roots, trigonometry, clamping and so on, with the constants pi and e), vector
operations on the position and direction tuples used throughout the ``SpaceCenter`` service
(dot and cross products, magnitudes, angles, interpolation), and quaternion operations on its
rotation tuples (composition, inverses, rotating vectors, interpolation). Angles are in
radians, with converters to and from degrees.

Calls to python's ``math`` module functions, and to C#'s ``System.Math`` methods, are
automatically compiled to the equivalent ``StdLib`` procedures when their arguments are
computed on the server — so ordinary mathematical code just works. The service can also be
called directly, which is the way to use the vector and quaternion operations. For example,
streaming the angle between the vessel's facing direction and its velocity, computed entirely
on the server:

.. tabs::

   .. group-tab:: C#

      .. code-block:: csharp

         var stdlib = connection.StdLib ();
         var frame = vessel.SurfaceReferenceFrame;
         var stream = connection.AddStream (
             () => stdlib.RadiansToDegrees (stdlib.VectorAngle (
                 vessel.Direction (frame), vessel.Velocity (frame))));

   .. group-tab:: Python

      .. code-block:: python

         stdlib = conn.std_lib
         frame = vessel.surface_reference_frame
         stream = conn.add_expression_stream(
             lambda: stdlib.radians_to_degrees(
                 stdlib.vector_angle(
                     vessel.direction(frame), vessel.velocity(frame)
                 )
             )
         )

What can be compiled
^^^^^^^^^^^^^^^^^^^^

The compilers support the constructs that have a server side equivalent: arithmetic,
comparisons, boolean operators, conditional expressions, collection constructors, indexing,
membership tests, the collection operations described above, and — in Python — the statements
described above. Anything else — ``try`` statements, string formatting, calls to client side
functions with server side arguments — cannot run on the server, and raises an error
describing the unsupported construct (``krpc.error.ExpressionCompilationError`` in Python,
``ExpressionCompilationException`` in C#).

Some semantics differ slightly from running the same code in your program:

* Boolean operators (``and``/``or``, ``&&``/``||``) do **not** short-circuit when evaluated on
  the server: both sides are always evaluated.
* Sub-expressions that do not interact with the server are evaluated once, at compile time,
  as described above — including calls to client side functions, which must therefore not
  depend on when they are called.
* In C#, lambdas passed to ``CompileExpression``, ``AddEvent`` or ``AddStream`` are expression
  trees, so the C# language rules for expression trees apply — most notably, optional
  arguments cannot be omitted from calls.
* ``round`` rounds a halfway value away from zero, where Python rounds it to the nearest even
  number.

The full lists of supported constructs are in the
`Python client documentation <../python/client.html>`_ and the
`C# client documentation <../csharp/client.html>`_.

.. _expressions-api:

The expression API
------------------

Compiled expressions are a convenience layer: what they produce, and what the C++ and Java
clients use directly, is a tree of expression objects built by calling the static methods of
the :class:`Expression` class. Every client on this page can use this API — the examples below
are given for all four languages that support streams.

Building an expression
^^^^^^^^^^^^^^^^^^^^^^

Expression objects are remote objects: each factory method is an RPC that creates one node of
the tree on the server and returns a handle to it. Building an expression therefore costs one
RPC per node — a one-off cost when the expression is created, not paid during evaluation.

The altitude event from the first part of this tutorial is built from three nodes: an embedded
call, a constant, and a comparison.

.. tabs::

   .. group-tab:: C#

      .. literalinclude:: /scripts/client/csharp/Event.cs

   .. group-tab:: C++

      .. literalinclude:: /scripts/client/cpp/Event.cpp

   .. group-tab:: Java

      .. literalinclude:: /scripts/client/java/CustomEvent.java

   .. group-tab:: Python

      .. literalinclude:: /scripts/client/python/Event.py

Two kinds of object appear here:

* A **procedure call message** describes an RPC without invoking it. Each client provides a
  way to build one: ``conn.get_call`` in Python, ``Connection.GetCall`` in C#,
  ``connection.getCall`` in Java, and the generated ``..._call()`` methods in C++ (for
  example ``flight.mean_altitude_call()``). Building a call message is a local operation — it
  does not contact the server.
* An **expression object**, created by the :class:`Expression` factory methods.
  :meth:`Expression.call` embeds a procedure call message in an expression; the call is then
  re-invoked by the server on each evaluation, and its result used in the surrounding
  expression. Only procedures that return a value can be embedded.

Operators and types
^^^^^^^^^^^^^^^^^^^

Constants are created with :meth:`Expression.constant_double`,
:meth:`Expression.constant_float`, :meth:`Expression.constant_int`,
:meth:`Expression.constant_bool` and :meth:`Expression.constant_string`. The usual arithmetic,
comparison and boolean operators combine expressions into larger ones, along with
:meth:`Expression.conditional` (an if-then-else value) and the bitwise shifts.

Expressions are statically typed, following the types of the server's procedures. Numeric
operands of differing types are converted automatically to a common type — an integer can be
multiplied by a double without an explicit conversion — and :meth:`Expression.cast` converts a
value to another type explicitly. Types are named by the :class:`Type` class, which has
factory methods for the primitive types (:meth:`Type.double`, :meth:`Type.int`,
:meth:`Type.string` and so on), for collection types, and — via :meth:`Type.class_type`,
:meth:`Type.enumeration_type` and :meth:`Type.struct_type` — for the classes, enumerations
and structures defined by services:

.. tabs::

   .. group-tab:: C#

      .. code-block:: csharp

         // Truncate the altitude to a whole number of meters
         var expr = Expression.Cast (connection,
             Expression.Call (connection, meanAltitude),
             Type.Int (connection));

   .. group-tab:: C++

      .. code-block:: cpp

         // Truncate the altitude to a whole number of meters
         auto expr = Expr::cast(conn,
           Expr::call(conn, mean_altitude),
           KType::int_(conn));

   .. group-tab:: Java

      .. code-block:: java

         // Truncate the altitude to a whole number of meters
         Expression expr = Expression.cast(
             connection,
             Expression.call(connection, meanAltitude),
             Type.int_(connection));

   .. group-tab:: Python

      .. code-block:: python

         # Truncate the altitude to a whole number of meters
         expr = expression.cast(
             expression.call(mean_altitude), types.int()
         )

Expression streams
^^^^^^^^^^^^^^^^^^

Any expression whose type can be sent to a client can be streamed. The statically typed
clients supply the expected type when creating the stream; the Python client asks the server
for the expression's return type, so decoding is automatic:

.. tabs::

   .. group-tab:: C#

      .. literalinclude:: /scripts/client/csharp/ExpressionStream.cs

   .. group-tab:: C++

      .. literalinclude:: /scripts/client/cpp/ExpressionStream.cpp

   .. group-tab:: Java

      .. literalinclude:: /scripts/client/java/ExpressionStream.java

   .. group-tab:: Python

      .. literalinclude:: /scripts/client/python/ExpressionStream.py

Expression streams behave like ordinary streams: the value is recomputed on each stream
update, sent only when it changes, and the stream's rate can be limited in the usual way.

Functions and collections
^^^^^^^^^^^^^^^^^^^^^^^^^

Expressions can process collections using operations such as :meth:`Expression.select` (apply
a function to every element), :meth:`Expression.where` (filter by a predicate),
:meth:`Expression.any`, :meth:`Expression.all`, :meth:`Expression.count`,
:meth:`Expression.sum`, :meth:`Expression.min`, :meth:`Expression.max`,
:meth:`Expression.average`, :meth:`Expression.order_by`, :meth:`Expression.aggregate`,
:meth:`Expression.contains`, :meth:`Expression.concat` and :meth:`Expression.get` (indexing).
Collections can also be constructed in an expression, with :meth:`Expression.create_list`,
:meth:`Expression.create_set`, :meth:`Expression.create_dictionary` and
:meth:`Expression.create_tuple`.

The functions passed to these operations are built from two pieces:

* :meth:`Expression.parameter` creates a named parameter with a given type — including class
  types, so a parameter can be, for example, an ``Engine``.
* :meth:`Expression.function` combines a list of parameters and a body into a function. Within
  the body, the parameter expressions stand for the function's arguments. (A function can
  also be called directly with :meth:`Expression.invoke`, binding values to its parameters by
  name.)

To call an RPC on each element of a collection, the call's *instance* must come from the
function's parameter rather than being fixed in advance. :meth:`Expression.call_with_arguments`
does exactly this: like :meth:`Expression.call`, it embeds a procedure call, but some or all
of the call's arguments — including the instance it is called on, at position 0 — are supplied
as expressions, keyed by the position of the parameter they supply. Positions with no
expression fall back to the values encoded in the call message, and then to the parameter's
default value.

Putting these together, the "any engine out of fuel" event from the first part of this
tutorial is built as follows. The template call message can be built from any convenient
engine — only the identity of the procedure is used, since the instance is supplied by the
parameter:

.. tabs::

   .. group-tab:: C#

      .. code-block:: csharp

         var krpc = connection.KRPC ();
         var vessel = connection.SpaceCenter ().ActiveVessel;

         // A function taking an engine, returning true if it is out of fuel
         var engine = Expression.Parameter (connection, "engine",
             Type.ClassType (connection, "SpaceCenter", "Engine"));
         var hasFuel = Connection.GetCall (() => vessel.Parts.Engines [0].HasFuel);
         var outOfFuel = Expression.Function (connection,
             new List<Expression> { engine },
             Expression.Not (connection,
                 Expression.CallWithArguments (connection, hasFuel,
                     new Dictionary<int, Expression> { { 0, engine } })));

         // Whether any engine satisfies it
         var engines = Connection.GetCall (() => vessel.Parts.Engines);
         var expr = Expression.Any (connection,
             Expression.Call (connection, engines), outOfFuel);

         var evnt = krpc.AddEvent (expr);
         lock (evnt.Condition) {
             evnt.Wait ();
             Console.WriteLine ("An engine has run out of fuel");
         }

   .. group-tab:: C++

      .. code-block:: cpp

         krpc::services::KRPC krpc(&conn);
         krpc::services::SpaceCenter sc(&conn);
         auto vessel = sc.active_vessel();

         typedef krpc::services::KRPC::Expression Expr;
         typedef krpc::services::KRPC::Type KType;

         // A function taking an engine, returning true if it is out of fuel
         auto engine = Expr::parameter(conn, "engine",
           KType::class_type(conn, "SpaceCenter", "Engine"));
         auto has_fuel = vessel.parts().engines()[0].has_fuel_call();
         auto out_of_fuel = Expr::function(conn,
           std::vector<Expr>({engine}),
           Expr::not_(conn, Expr::call_with_arguments(
             conn, has_fuel, std::map<int32_t, Expr>({{0, engine}}))));

         // Whether any engine satisfies it
         auto engines = vessel.parts().engines_call();
         auto expr = Expr::any(conn, Expr::call(conn, engines), out_of_fuel);

         auto event = krpc.add_event(expr);
         event.acquire();
         event.wait();
         std::cout << "An engine has run out of fuel" << std::endl;
         event.release();

   .. group-tab:: Java

      .. code-block:: java

         KRPC krpc = KRPC.newInstance(connection);
         SpaceCenter spaceCenter = SpaceCenter.newInstance(connection);
         SpaceCenter.Vessel vessel = spaceCenter.getActiveVessel();

         // A function taking an engine, returning true if it is out of fuel
         Expression engine = Expression.parameter(
             connection, "engine",
             Type.classType(connection, "SpaceCenter", "Engine"));
         ProcedureCall hasFuel = connection.getCall(
             vessel.getParts().getEngines().get(0), "getHasFuel");
         Expression outOfFuel = Expression.function(
             connection, Arrays.asList(engine),
             Expression.not(connection, Expression.callWithArguments(
                 connection, hasFuel, Collections.singletonMap(0, engine))));

         // Whether any engine satisfies it
         ProcedureCall engines = connection.getCall(vessel.getParts(), "getEngines");
         Expression expr = Expression.any(
             connection, Expression.call(connection, engines), outOfFuel);

         Event event = krpc.addEvent(expr);
         synchronized (event.getCondition()) {
             event.waitFor();
             System.out.println("An engine has run out of fuel");
         }

   .. group-tab:: Python

      .. code-block:: python

         expression = conn.krpc.Expression
         types = conn.krpc.Type
         vessel = conn.space_center.active_vessel

         # A function taking an engine, returning true if it is out of fuel
         engine = expression.parameter(
             "engine", types.class_type("SpaceCenter", "Engine")
         )
         has_fuel = conn.get_call(getattr, vessel.parts.engines[0], "has_fuel")
         out_of_fuel = expression.function(
             [engine],
             expression.not_(expression.call_with_arguments(has_fuel, {0: engine})),
         )

         # Whether any engine satisfies it
         engines = conn.get_call(getattr, vessel.parts, "engines")
         expr = expression.any(expression.call(engines), out_of_fuel)

         event = conn.krpc.add_event(expr)
         with event.condition:
             event.wait()
             print("An engine has run out of fuel")

:meth:`Expression.select` and :meth:`Expression.where` produce lazily evaluated sequences,
which cannot be sent to a client directly; convert them to a concrete collection with
:meth:`Expression.to_list` or :meth:`Expression.to_set` before streaming them. The
aggregations (:meth:`Expression.sum`, :meth:`Expression.min`, :meth:`Expression.max`,
:meth:`Expression.any`, :meth:`Expression.all`) accept lazy sequences directly.

Structures
^^^^^^^^^^

A structure a service defines is an ordinary value in an expression: it can be returned by a
call, streamed, passed as an argument, and built. :meth:`Expression.get_field` reads a field
of one by name, and :meth:`Expression.create_struct` builds one from its type and the values
of its fields, in the order the structure declares them:

.. tabs::

   .. group-tab:: C#

      .. code-block:: csharp

         // The Name field of a structure a call returns
         var expr = Expression.GetField (connection,
             Expression.Call (connection, siteInfo), "Name");

   .. group-tab:: C++

      .. code-block:: cpp

         // The Name field of a structure a call returns
         auto expr = Expr::get_field(conn, Expr::call(conn, site_info), "Name");

   .. group-tab:: Java

      .. code-block:: java

         // The Name field of a structure a call returns
         Expression expr = Expression.getField(
             connection, Expression.call(connection, siteInfo), "Name");

   .. group-tab:: Python

      .. code-block:: python

         # The Name field of a structure a call returns
         expr = expression.get_field(expression.call(site_info), "Name")

A field is named by the name the service declares it with, which is the name that appears in
the API documentation, rather than by the name it has in a particular client.

The Python and C# compilers produce these nodes from ordinary syntax: reading an attribute of
a structure valued expression compiles to :meth:`Expression.get_field`, and constructing one —
calling the structure type in Python, or ``new`` in C# — compiles to
:meth:`Expression.create_struct`.

Statements, variables and loops
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

Everything the Python statement compiler produces can be built directly with the factory API,
in any client. Statements are ordinary expression nodes: :meth:`Expression.block` sequences
statements (its value is the value of its last statement), :meth:`Expression.block_with_variables`
also declares local variables created with :meth:`Expression.variable` and assigned with
:meth:`Expression.assign`, :meth:`Expression.if_then` and :meth:`Expression.if_then_else` are
if statements, and :meth:`Expression.while_` and :meth:`Expression.for_each` are loops, with
:meth:`Expression.break_` and :meth:`Expression.continue_` applying to the nearest enclosing
loop. :meth:`Expression.return_` ends the evaluation of the enclosing function early. Calls to
procedures without a return value can be used as statements, for their effects, and
collections can be built imperatively with :meth:`Expression.create_empty_list` (and set and
dictionary counterparts), :meth:`Expression.list_add`, :meth:`Expression.list_set`,
:meth:`Expression.set_add` and :meth:`Expression.dictionary_set`.

The following builds and runs a small program — ``total = 0; for x in [1, 2, 3]: total += x``
— returning 6:

.. tabs::

   .. group-tab:: C#

      .. code-block:: csharp

         var total = Expression.Variable (connection, "total", Type.Int (connection));
         var x = Expression.Variable (connection, "x", Type.Int (connection));
         var values = Expression.CreateList (connection, new List<Expression> {
             Expression.ConstantInt (connection, 1),
             Expression.ConstantInt (connection, 2),
             Expression.ConstantInt (connection, 3)
         });
         var program = Expression.BlockWithVariables (
             connection,
             new List<Expression> { total, x },
             new List<Expression> {
                 Expression.Assign (connection, total, Expression.ConstantInt (connection, 0)),
                 Expression.ForEach (connection, x, values,
                     Expression.Assign (connection, total,
                         Expression.Add (connection, total, x))),
                 total
             });
         Console.WriteLine (connection.RunFunction<int> (program));

   .. group-tab:: C++

      .. code-block:: cpp

         auto total = Expr::variable(conn, "total", KType::int_(conn));
         auto x = Expr::variable(conn, "x", KType::int_(conn));
         auto values = Expr::create_list(conn, std::vector<Expr>({
           Expr::constant_int(conn, 1),
           Expr::constant_int(conn, 2),
           Expr::constant_int(conn, 3)}));
         auto program = Expr::block_with_variables(
           conn,
           std::vector<Expr>({total, x}),
           std::vector<Expr>({
             Expr::assign(conn, total, Expr::constant_int(conn, 0)),
             Expr::for_each(conn, x, values,
               Expr::assign(conn, total, Expr::add(conn, total, x))),
             total}));
         std::cout << krpc::run_function<int32_t>(program) << std::endl;

   .. group-tab:: Java

      .. code-block:: java

         Expression total = Expression.variable(
             connection, "total", Type.int_(connection));
         Expression x = Expression.variable(connection, "x", Type.int_(connection));
         Expression values = Expression.createList(connection, Arrays.asList(
             Expression.constantInt(connection, 1),
             Expression.constantInt(connection, 2),
             Expression.constantInt(connection, 3)));
         Expression program = Expression.blockWithVariables(
             connection,
             Arrays.asList(total, x),
             Arrays.asList(
                 Expression.assign(connection, total,
                     Expression.constantInt(connection, 0)),
                 Expression.forEach(connection, x, values,
                     Expression.assign(connection, total,
                         Expression.add(connection, total, x))),
                 total));
         int result = connection.runFunction(program);
         System.out.println(result);

   .. group-tab:: Python

      .. code-block:: python

         expression = conn.krpc.Expression
         types = conn.krpc.Type

         total = expression.variable("total", types.int())
         x = expression.variable("x", types.int())
         values = expression.create_list(
             [expression.constant_int(i) for i in [1, 2, 3]]
         )
         program = expression.block_with_variables(
             [total, x],
             [
                 expression.assign(total, expression.constant_int(0)),
                 expression.for_each(
                     x, values, expression.assign(total, expression.add(total, x))
                 ),
                 total,
             ],
         )
         print(conn.run_function(program))

Object constants
^^^^^^^^^^^^^^^^

:meth:`Expression.constant_object` embeds a reference to an object — a vessel, a reference
frame, an engine — in an expression, so objects can be compared for equality or passed as
arguments to calls built with :meth:`Expression.call_with_arguments`. Objects are identified
by the same identifier used to reference them over the communication protocol, which each
client exposes on its remote object wrappers: the ``id`` field of a remote object in C# and
Java, ``_id`` in C++, and the ``_object_id`` attribute in Python. For example, an event that
fires when the vessel's target changes to a particular vessel:

.. code-block:: python

   target = conn.get_call(getattr, conn.space_center, "target_vessel")
   expr = expression.equal(
       expression.call(target),
       expression.constant_object(some_vessel._object_id),
   )
   event = conn.krpc.add_event(expr)

(The compiled expression layer does this automatically whenever a remote object is captured
by a compiled function.)

Extending server side functions
-------------------------------

Server side functions can call the procedures of *any* service installed on the server — the
compilers resolve remote calls through the server's service definitions, and the factory API
embeds any procedure with :meth:`Expression.call_with_arguments`. This means the extension
mechanism for server side functions is the same as for the rest of kRPC: **write a service**.
A third party service DLL dropped into the game's plugin folder contributes its procedures to
server side functions on every client, with documentation and client stubs generated in the
usual way. The ``StdLib`` service is implemented exactly like this; it is only special in
that it ships with the server.

Errors, edge cases and performance
----------------------------------

**Errors.** If evaluating an expression raises an error — for example, an embedded call refers
to a part that has been destroyed — the error is delivered to the client through the event or
stream, and raised when your program accesses it. The expression is not removed: if the
condition causing the error clears, evaluation resumes.

**Procedures that pause.** A small number of RPCs pause execution and resume on a later tick.
Such a procedure cannot produce a value within a function's single-tick evaluation, and there
is no way to resume the function around it: the only way to make progress would be to evaluate
it again from the start, repeating everything it had already done. Calling one is therefore
reported as an error, by a run-once function and by an event or stream alike. Functions that
only read values — the vast majority — are unaffected.

**Loops run to completion.** A ``while`` loop is evaluated within a single tick, and nothing
interrupts it. A loop whose condition never becomes false hangs the game, with no way to
recover other than closing it, so bound every loop by something that is certain to change.

**Null values.** Expressions cannot contain null constants, and a call embedded in an
expression that returns null will fail to evaluate if the null flows into an operator.

**Side effects.** An event or a stream re-evaluates its expression on every update, so any
side effects it has are repeated on every update. Run a function that changes the game with
``run_function`` instead, which evaluates it exactly once.

**Strings are not collections.** A string satisfies none of the collection operations, and
passing one to them is reported as an error rather than being treated as a sequence of
characters.

**Costs.** Building an expression costs one RPC per node of the tree, once. Evaluating it
costs nothing on the network — evaluation happens entirely on the server, and is compiled to
native code, so even large expressions evaluate quickly. An expression stream then behaves
like any other stream: updates are only sent when the value changes, and the update rate can
be limited with the stream's rate control.

**Frozen inputs.** Everything in an expression other than its embedded calls is fixed at
creation time. To change a threshold, a target object or a captured collection, build a new
expression and remove the old event or stream.
