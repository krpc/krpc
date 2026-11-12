.. default-domain:: cs
.. highlight:: csharp

.. namespace:: KRPC.Client.Services.KRPC


.. class:: KRPC

   Main kRPC service, used by clients to interact with basic server functionality.

   .. property:: System.Collections.Generic.IList<ClientInfo> Clients { get; }

      The clients that are currently connected to the server.

      :Game Scenes: All

   .. property:: GameScene GameScene { get; set; }

      The current game scene. Setting this switches the game to the given
      scene, or opens/closes the corresponding facility for the pseudo-scenes.
      Scene changes happen asynchronously: setting this property returns
      immediately, and clients should poll it until it reports the requested
      scene. Setting it to :value:`GameScene.Flight` resumes the save's
      active vessel, and fails if there is none. Moving between
      :value:`GameScene.EditorVAB` and :value:`GameScene.EditorSPH`
      keeps the vessel that is being constructed, as the game's own switch
      editor button does. Leaving flight saves the game first, unless the game
      cannot be saved, e.g. while under acceleration.

      :Game Scenes: All

   .. property:: uint GameStateLoads { get; }

      The number of game states that have finished loading.

      :Game Scenes: All


      .. note::

         Increases once a game load, quickload, revert or scene change has finished.

   .. function:: byte[] GetClientID()

      Returns the identifier for the current client.


      :Game Scenes: All

   .. function:: string GetClientName()

      Returns the name of the current client.
      This is an empty string if the client has no name.


      :Game Scenes: All

   .. function:: KRPC.Schema.KRPC.Services GetServices()

      The services the server provides, with their procedures, classes, enumerations,
      structures and exceptions.


      :Game Scenes: All

   .. function:: ServerStatus GetStatus()

      Returns the status of the server, with its version, settings and performance statistics.


      :Game Scenes: All

   .. function:: void HoldTick()

      Hold the game on its current physics tick until :meth:`KRPC.ReleaseTick <KRPC.KRPC.ReleaseTick>` is called
      or the hold times out. Every call made in between is executed before the game
      advances, so a read, compute and write all happen in the state of one tick.


      :Game Scenes: All


      .. note::

         The game renders no frame, takes no input and runs no physics while the tick is
         held. Keep a hold as short as possible, and release it even when the code between
         the two calls fails.

         A tick is held once, so this waits for the next tick when the current one has
         already been released. One client holds the tick at a time, and only a client
         making a call can hold it: a stream or an event cannot.

         A hold ends by itself when it times out, when the client disconnects, and when the
         client calls a procedure that takes more than one tick to finish, such as staging
         or warping. Stream updates are sent once the tick is released, so waiting for a
         stream or an event inside a hold waits out the timeout.

   .. function:: void NextTick()

      Let the game move on from the tick being held and hold the one after it, so that a
      program acts on consecutive physics ticks. Holds the soonest tick it can if no tick
      is currently being held.


      :Game Scenes: All


      .. note::

         A release and a hold made as two separate calls let the game advance between them.
         This takes the next tick without that gap. Call it at the top of a loop that has to
         act on every tick, work, and come back to it.

         The game advances one tick per call, so it runs at the rate of the loop for as long
         as the loop runs. End the loop with :meth:`KRPC.ReleaseTick <KRPC.KRPC.ReleaseTick>`.

         Every limit of :meth:`KRPC.HoldTick <KRPC.KRPC.HoldTick>` applies, the timeout included. A hold lost to
         the timeout is not reported, and the next call takes whatever tick it lands on.

   .. property:: bool Paused { get; set; }

      Whether the game is paused.

      :Game Scenes: All

   .. function:: void ReleaseTick()

      Let the game move on from the tick that :meth:`KRPC.HoldTick <KRPC.KRPC.HoldTick>` held it on. Does
      nothing when no tick is held, which is the case once a hold has timed out.


      :Game Scenes: All



.. class:: Expression

   A server side expression.

   .. note::

      Numeric operands of differing types are converted to a common type.

   .. function:: static Expression Add(IConnection connection, Expression arg0, Expression arg1)

      Numerical addition.

      :parameters:

       * **arg0** -- The first operand.
       * **arg1** -- The second operand.

      :Game Scenes: All

   .. function:: static Expression Aggregate(IConnection connection, Expression arg, Expression func)

      The result of applying an accumulator function over a collection.

      :parameters:

       * **arg** -- The collection.
       * **func** -- The accumulator function.

      :returns: The accumulated value.
      :Game Scenes: All


      .. note::

         Throws an exception if the collection is empty.

   .. function:: static Expression AggregateWithSeed(IConnection connection, Expression arg, Expression seed, Expression func)

      The result of applying an accumulator function over a collection, starting from a seed.

      :parameters:

       * **arg** -- The collection.
       * **seed** -- The seed value.
       * **func** -- The accumulator function.

      :returns: The accumulated value.
      :Game Scenes: All


      .. note::

         The seed widens to the type of the accumulated value, the function's result.

   .. function:: static Expression All(IConnection connection, Expression arg, Expression predicate)

      Whether a predicate function returns true for every value of a collection.

      :parameters:

       * **arg** -- The collection.
       * **predicate** -- The predicate function.

      :returns: Whether all items satisfy the predicate.
      :Game Scenes: All

   .. function:: static Expression And(IConnection connection, Expression arg0, Expression arg1)

      Logical or bitwise and, which evaluates both operands.

      :parameters:

       * **arg0** -- The first operand.
       * **arg1** -- The second operand.

      :Game Scenes: All


      .. note::

         Also takes two values of a flags enumeration, and gives the values they share.

   .. function:: static Expression Any(IConnection connection, Expression arg, Expression predicate)

      Whether a predicate function returns true for any value of a collection.

      :parameters:

       * **arg** -- The collection.
       * **predicate** -- The predicate function.

      :returns: Whether any item satisfies the predicate.
      :Game Scenes: All

   .. function:: static Expression Append(IConnection connection, Expression collection, Expression value)

      A statement adding a value to a list or a set.

      :parameters:

       * **collection** -- The list or set to add to.
       * **value** -- The value to add.

      :Game Scenes: All


      .. note::

         The value goes on the end of a list. A set holding the value is left unchanged.

   .. function:: static Expression Assign(IConnection connection, Expression variable, Expression value)

      An assignment to a local variable, state variable or function parameter.

      :parameters:

       * **variable** -- The variable to assign to.
       * **value** -- The value to assign.

      :Game Scenes: All


      .. note::

         The value's type must be assignable to the variable's type. A numeric value of a
         different type is widened, and narrowing it requires a cast.

   .. function:: static Expression Average(IConnection connection, Expression arg)

      The average of the values of a collection.

      :parameters:

       * **arg** -- The list or set.

      :returns: The average of the elements in the collection.
      :Game Scenes: All


      .. note::

         Throws an exception if the collection is empty and its values are not nullable.

   .. function:: static Expression Block(IConnection connection, System.Collections.Generic.IList<Expression> statements)

      A block of statements, whose value is that of its last statement.

      :parameters:

       * **statements** -- The statements.

      :Game Scenes: All

   .. function:: static Expression BlockWithVariables(IConnection connection, System.Collections.Generic.IList<Expression> variables, System.Collections.Generic.IList<Expression> statements)

      A block of statements with local variables.

      :parameters:

       * **variables** -- The local variables of the block.
       * **statements** -- The statements.

      :Game Scenes: All


      .. note::

         The value of the block is that of its last statement. The variables are in scope
         for the statements of the block, including within nested functions.

   .. function:: static Expression Break(IConnection connection)

      A break statement, which ends the enclosing loop or :meth:`Expression.Switch`.


      :Game Scenes: All

   .. function:: static Expression BuildDictionary(IConnection connection, Expression arg, Expression keyFunc, Expression valueFunc)

      A dictionary built from a collection, with a key and a value computed from each
      element.

      :parameters:

       * **arg** -- The list or set.
       * **keyFunc** -- The function computing an element's key.
       * **valueFunc** -- The function computing an element's value.

      :returns: The dictionary.
      :Game Scenes: All


      .. note::

         A repeated key takes the last of its values.

   .. function:: static Expression Call(IConnection connection, KRPC.Schema.KRPC.ProcedureCall call)

      An RPC call.

      :parameters:

       * **call** -- The procedure call.

      :Game Scenes: All


      .. note::

         The instance the call is made on, and the values of its arguments, are fixed
         when the expression is created. A call to a procedure with no return value is
         evaluated for its effects, as a statement in a :meth:`Expression.Block`. A collection the
         procedure returns is copied, as a client would receive it.

   .. function:: static Expression CallWithArguments(IConnection connection, KRPC.Schema.KRPC.ProcedureCall call, System.Collections.Generic.IDictionary<int,Expression> args)

      An RPC call, where some or all of the arguments are computed by expressions.

      :parameters:

       * **call** -- The RPC to call.
       * **args** -- Expressions computing the call's arguments, by position.

      :Game Scenes: All


      .. note::

         Position 0 is the instance of a class method or property. A position with no
         expression takes the argument encoded in the call, or the parameter's default. A
         collection the procedure returns is copied, as a client would receive it.

   .. function:: static Expression Cast(IConnection connection, Expression arg, Type type)

      A value cast to the given type.

      :parameters:

       * **arg** -- The value to cast.
       * **type** -- Type to cast the argument to.

      :Game Scenes: All


      .. note::

         A tuple or collection is cast element by element.

   .. function:: static Expression Clear(IConnection connection, Expression collection)

      A statement removing every value from a list or a set, or every entry from a dictionary.

      :parameters:

       * **collection** -- The list, set or dictionary.

      :Game Scenes: All

   .. function:: static Expression Concat(IConnection connection, Expression arg1, Expression arg2)

      The values of one collection followed by those of another.

      :parameters:

       * **arg1** -- The first collection.
       * **arg2** -- The second collection.

      :returns: The first collection followed by the second.
      :Game Scenes: All


      .. note::

         The result is lazily evaluated. :meth:`Expression.ToList` makes it concrete.

   .. function:: static Expression Conditional(IConnection connection, Expression condition, Expression ifTrue, Expression ifFalse)

      The second argument if the condition is true, and the third argument otherwise.

      :parameters:

       * **condition** -- The boolean condition.
       * **ifTrue** -- The value when the condition is true.
       * **ifFalse** -- The value when the condition is false.

      :Game Scenes: All


      .. note::

         Throws an exception if the two values have no common type.

   .. function:: static Expression ConditionalAnd(IConnection connection, Expression arg0, Expression arg1)

      Logical and, which evaluates the second operand only when the first is true.

      :parameters:

       * **arg0** -- The first operand.
       * **arg1** -- The second operand.

      :Game Scenes: All

   .. function:: static Expression ConditionalOr(IConnection connection, Expression arg0, Expression arg1)

      Logical or, which evaluates the second operand only when the first is false.

      :parameters:

       * **arg0** -- The first operand.
       * **arg1** -- The second operand.

      :Game Scenes: All

   .. function:: static Expression ConstantBool(IConnection connection, bool value)

      A constant value of boolean type.

      :parameters:

       * **value** -- The value.

      :Game Scenes: All

   .. function:: static Expression ConstantBytes(IConnection connection, byte[] value)

      A constant value of bytes type.

      :parameters:

       * **value** -- The value.

      :Game Scenes: All

   .. function:: static Expression ConstantDouble(IConnection connection, double value)

      A constant value of double precision floating point type.

      :parameters:

       * **value** -- The value.

      :Game Scenes: All

   .. function:: static Expression ConstantEnum(IConnection connection, string service, string name, int value)

      A constant value of an enumeration a service defines.

      :parameters:

       * **service** -- The name of the service the enumeration is defined in.
       * **name** -- The name of the enumeration.
       * **value** -- The value of the member of the enumeration.

      :Game Scenes: All

   .. function:: static Expression ConstantEnumById(IConnection connection, uint serviceId, uint enumerationId, int value)

      A constant value of an enumeration a service defines, named by ids.

      :parameters:

       * **serviceId** -- The id of the service the enumeration is defined in.
       * **enumerationId** -- The id of the enumeration.
       * **value** -- The value of the member of the enumeration.

      :Game Scenes: All

   .. function:: static Expression ConstantFloat(IConnection connection, float value)

      A constant value of single precision floating point type.

      :parameters:

       * **value** -- The value.

      :Game Scenes: All

   .. function:: static Expression ConstantInt(IConnection connection, int value)

      A constant value of integer type.

      :parameters:

       * **value** -- The value.

      :Game Scenes: All

   .. function:: static Expression ConstantLong(IConnection connection, long value)

      A constant value of long integer type.

      :parameters:

       * **value** -- The value.

      :Game Scenes: All

   .. function:: static Expression ConstantNull(IConnection connection, Type type)

      A null constant of the given type.

      :parameters:

       * **type** -- The type of the value.

      :Game Scenes: All


      .. note::

         The type is made nullable, as by :meth:`Type.NullableType`.

   .. function:: static Expression ConstantObject(IConnection connection, ulong value)

      A constant object, an instance of a class a service defines.

      :parameters:

       * **value** -- The object identifier of the object.

      :Game Scenes: All


      .. note::

         The object is given by its object identifier.

   .. function:: static Expression ConstantString(IConnection connection, string value)

      A constant value of string type.

      :parameters:

       * **value** -- The value.

      :Game Scenes: All

   .. function:: static Expression ConstantUInt(IConnection connection, uint value)

      A constant value of unsigned integer type.

      :parameters:

       * **value** -- The value.

      :Game Scenes: All

   .. function:: static Expression ConstantULong(IConnection connection, ulong value)

      A constant value of unsigned long integer type.

      :parameters:

       * **value** -- The value.

      :Game Scenes: All

   .. function:: static Expression Contains(IConnection connection, Expression arg, Expression value)

      Whether a collection contains a value.

      :parameters:

       * **arg** -- The collection.
       * **value** -- The value to look for.

      :returns: Whether the collection contains the value.
      :Game Scenes: All

   .. function:: static Expression ContainsKey(IConnection connection, Expression dictionary, Expression key)

      Whether a dictionary contains a key.

      :parameters:

       * **dictionary** -- The dictionary.
       * **key** -- The key to look for.

      :returns: Whether the dictionary contains the key.
      :Game Scenes: All

   .. function:: static Expression Continue(IConnection connection)

      A continue statement, which skips to the next iteration of the enclosing loop.


      :Game Scenes: All

   .. function:: static Expression ConvertToString(IConnection connection, Expression arg)

      The string representation of a value.

      :parameters:

       * **arg** -- The value to convert.

      :returns: The string representation of the value.
      :Game Scenes: All


      .. note::

         A value other than a number, boolean, string or enumeration converts as .NET
         formats it, which for a collection is the name of its type. Throws an exception if
         the value is null.

   .. function:: static Expression Count(IConnection connection, Expression arg)

      The number of values in a collection.

      :parameters:

       * **arg** -- The list, set or dictionary.

      :returns: The number of elements in the collection.
      :Game Scenes: All

   .. function:: static Expression CreateDictionary(IConnection connection, System.Collections.Generic.IList<Expression> keys, System.Collections.Generic.IList<Expression> values)

      A dictionary of the given keys and corresponding values, with numbers widened to
      their common type. Throws an exception if there are no keys, the counts differ, the
      keys are not of a valid key type, or a key is null.

      :parameters:

       * **keys** -- The keys.
       * **values** -- The values.

      :returns: The dictionary.
      :Game Scenes: All


      .. note::

         A repeated key takes the last of its values.

   .. function:: static Expression CreateEmptyDictionary(IConnection connection, Type keyType, Type valueType)

      An empty dictionary of the given key and value types. Throws an exception if the
      key type is nullable or not a valid key type.

      :parameters:

       * **keyType** -- The type of the dictionary's keys.
       * **valueType** -- The type of the dictionary's values.

      :returns: The empty dictionary.
      :Game Scenes: All

   .. function:: static Expression CreateEmptyList(IConnection connection, Type valueType)

      An empty list of the given value type.

      :parameters:

       * **valueType** -- The type of the values the list holds.

      :returns: The empty list.
      :Game Scenes: All

   .. function:: static Expression CreateEmptySet(IConnection connection, Type valueType)

      An empty set of the given value type. Throws an exception if the value type is
      nullable.

      :parameters:

       * **valueType** -- The type of the values the set holds.

      :returns: The empty set.
      :Game Scenes: All

   .. function:: static Expression CreateList(IConnection connection, System.Collections.Generic.IList<Expression> values)

      A list of the given values, with numbers widened to their common type. Throws an
      exception if there are no values.

      :parameters:

       * **values** -- The values.

      :returns: The list.
      :Game Scenes: All

   .. function:: static Expression CreateSet(IConnection connection, System.Collections.Generic.ISet<Expression> values)

      A set of the given values, with numbers widened to their common type. Throws an
      exception if there are no values, or a value is null.

      :parameters:

       * **values** -- The values.

      :returns: The set.
      :Game Scenes: All


      .. note::

         The values are evaluated in an unspecified order.

   .. function:: static Expression CreateStruct(IConnection connection, Type type, System.Collections.Generic.IList<Expression> fieldValues)

      A structure with the given field values. Throws an exception if the field values do
      not match its fields.

      :parameters:

       * **type** -- The type of the structure.
       * **fieldValues** -- The values of the structure's fields, in the order the structure declares them.

      :returns: The structure.
      :Game Scenes: All

   .. function:: static Expression CreateTuple(IConnection connection, System.Collections.Generic.IList<Expression> elements)

      A tuple of the given elements. Throws an exception if there are no elements or more
      than seven.

      :parameters:

       * **elements** -- The elements.

      :returns: The tuple.
      :Game Scenes: All

   .. function:: static Expression DictionaryKeys(IConnection connection, Expression dictionary)

      The keys of a dictionary, as a list.

      :parameters:

       * **dictionary** -- The dictionary.

      :returns: The keys of the dictionary.
      :Game Scenes: All


      .. note::

         The collection operations take the keys of a dictionary from here, and its values
         from :meth:`Expression.DictionaryValues`.

   .. function:: static Expression DictionaryValues(IConnection connection, Expression dictionary)

      The values of a dictionary, as a list.

      :parameters:

       * **dictionary** -- The dictionary.

      :returns: The values of the dictionary.
      :Game Scenes: All

   .. function:: static Expression Distinct(IConnection connection, Expression arg)

      The values of a collection with duplicates removed, keeping the first
      occurrence of each.

      :parameters:

       * **arg** -- The collection.

      :returns: The distinct values of the collection.
      :Game Scenes: All


      .. note::

         The result is lazily evaluated. :meth:`Expression.ToList` makes it concrete.

   .. function:: static Expression Divide(IConnection connection, Expression arg0, Expression arg1)

      Numerical division.

      :parameters:

       * **arg0** -- The first operand.
       * **arg1** -- The second operand.

      :Game Scenes: All

   .. function:: static Expression ElementAt(IConnection connection, Expression arg, Expression index)

      The value at the given position in a collection.

      :parameters:

       * **arg** -- The collection.
       * **index** -- The position of the value, counting from zero.

      :returns: The value at the given position.
      :Game Scenes: All


      .. note::

         Throws an exception if the index is out of range.

   .. function:: static Expression Equal(IConnection connection, Expression arg0, Expression arg1)

      Equality comparison.

      :parameters:

       * **arg0** -- The first operand.
       * **arg1** -- The second operand.

      :Game Scenes: All


      .. note::

         Collections, tuples, bytes and structures compare by their contents. Throws an
         exception if the operands are of unrelated types.

   .. function:: static Expression Except(IConnection connection, Expression arg1, Expression arg2)

      The values of the first collection that are not in the second, with
      duplicates removed.

      :parameters:

       * **arg1** -- The first collection.
       * **arg2** -- The second collection.

      :returns: The difference between the two collections.
      :Game Scenes: All


      .. note::

         The result is lazily evaluated. :meth:`Expression.ToList` makes it concrete.

   .. function:: static Expression ExclusiveOr(IConnection connection, Expression arg0, Expression arg1)

      Logical or bitwise exclusive or.

      :parameters:

       * **arg0** -- The first operand.
       * **arg1** -- The second operand.

      :Game Scenes: All


      .. note::

         Also takes two values of a flags enumeration, and gives the values in only one of them.

   .. function:: static Expression First(IConnection connection, Expression arg)

      The first value of a collection.

      :parameters:

       * **arg** -- The collection.

      :returns: The first value.
      :Game Scenes: All


      .. note::

         Throws an exception if the collection is empty.

   .. function:: static Expression ForEach(IConnection connection, Expression variable, Expression collection, Expression body)

      A loop that evaluates the body once for each value of a collection.

      :parameters:

       * **variable** -- The loop variable.
       * **collection** -- The collection to iterate over.
       * **body** -- The statement evaluated on each iteration.

      :Game Scenes: All


      .. note::

         The loop variable is a local variable declared by an enclosing
         :meth:`Expression.BlockWithVariables`, or a parameter of an enclosing :meth:`Expression.Lambda`.
         A :meth:`Expression.Break` or :meth:`Expression.Continue` in the body applies to this loop.

   .. function:: static Expression Get(IConnection connection, Expression arg, Expression index)

      An element of a tuple, list or dictionary.

      :parameters:

       * **arg** -- The tuple, list or dictionary.
       * **index** -- The index or key of the element.

      :returns: The element.
      :Game Scenes: All


      .. note::

         A tuple takes a constant integer index, a list a zero indexed integer and a
         dictionary a key. Throws an exception if the index is out of range or the key is
         missing, and for a tuple when the expression is created.

   .. function:: static Expression GetField(IConnection connection, Expression arg, string name)

      The value of a field of a structure. Throws an exception if the value is not a
      structure, or has no field of the given name.

      :parameters:

       * **arg** -- The structure.
       * **name** -- The name of the field to access.

      :returns: The value of the field.
      :Game Scenes: All

   .. function:: static Expression GetFieldByIndex(IConnection connection, Expression arg, int index)

      The value of a field of a structure, named by its position. Throws an exception if
      the value is not a structure, or the index is out of range.

      :parameters:

       * **arg** -- The structure.
       * **index** -- The position of the field to access.

      :returns: The value of the field.
      :Game Scenes: All


      .. note::

         The fields are numbered from 0, in the order the structure defines them.

   .. function:: static Expression GreaterThan(IConnection connection, Expression arg0, Expression arg1)

      Greater than numerical comparison.

      :parameters:

       * **arg0** -- The first operand.
       * **arg1** -- The second operand.

      :Game Scenes: All

   .. function:: static Expression GreaterThanOrEqual(IConnection connection, Expression arg0, Expression arg1)

      Greater than or equal numerical comparison.

      :parameters:

       * **arg0** -- The first operand.
       * **arg1** -- The second operand.

      :Game Scenes: All

   .. function:: static Expression GroupBy(IConnection connection, Expression arg, Expression key)

      The values of a collection grouped by the key a function produces for each of them.

      :parameters:

       * **arg** -- The collection.
       * **key** -- A function producing the key to group values by.

      :returns: A dictionary of each key to the values that produced it.
      :Game Scenes: All

   .. property:: bool HasReturnType { get; }

      Whether the expression evaluates to a value.

      :Game Scenes: All


      .. note::

         An expression with no value has no :prop:`Expression.ReturnType`. An empty
         collection and no value both encode to an empty sequence of bytes.

   .. function:: static Expression IfThen(IConnection connection, Expression condition, Expression body)

      An if statement, with no value.

      :parameters:

       * **condition** -- The boolean condition.
       * **body** -- The statement evaluated when the condition is true.

      :Game Scenes: All


      .. note::

         Use :meth:`Expression.Conditional` for an if-then-else that produces a value.

   .. function:: static Expression IfThenElse(IConnection connection, Expression condition, Expression body, Expression elseBody)

      An if-else statement, with no value.

      :parameters:

       * **condition** -- The boolean condition.
       * **body** -- The statement evaluated when the condition is true.
       * **elseBody** -- The statement evaluated when the condition is false.

      :Game Scenes: All


      .. note::

         Use :meth:`Expression.Conditional` for an if-then-else that produces a value.

   .. function:: static Expression Intersect(IConnection connection, Expression arg1, Expression arg2)

      The values in both of two collections, with duplicates removed.

      :parameters:

       * **arg1** -- The first collection.
       * **arg2** -- The second collection.

      :returns: The intersection of the two collections.
      :Game Scenes: All


      .. note::

         The result is lazily evaluated. :meth:`Expression.ToList` makes it concrete.

   .. function:: static Expression Invoke(IConnection connection, Expression function, System.Collections.Generic.IDictionary<string,Expression> args)

      A function call.

      :parameters:

       * **function** -- The function to call.
       * **args** -- The arguments to call the function with.

      :returns: A function call.
      :Game Scenes: All

   .. function:: static Expression IsNull(IConnection connection, Expression value)

      Whether a value is null. Throws an exception if the type of the value cannot be null.

      :parameters:

       * **value** -- The value.

      :Game Scenes: All

   .. function:: static Expression Lambda(IConnection connection, System.Collections.Generic.IList<Expression> parameters, Expression body)

      A function with the given parameters and body.

      :parameters:

       * **parameters** -- The parameters of the function.
       * **body** -- The body of the function.

      :returns: A function.
      :Game Scenes: All


      .. note::

         The body is an expression or a block of statements, and a body with no value is
         evaluated for its effects. A :meth:`Expression.Return` or :meth:`Expression.ReturnNothing`
         statement ends the evaluation.

   .. function:: static Expression Last(IConnection connection, Expression arg)

      The last value of a collection.

      :parameters:

       * **arg** -- The collection.

      :returns: The last value.
      :Game Scenes: All


      .. note::

         Throws an exception if the collection is empty.

   .. function:: static Expression LeftShift(IConnection connection, Expression arg0, Expression arg1)

      Bitwise left shift.

      :parameters:

       * **arg0** -- The first operand.
       * **arg1** -- The second operand.

      :Game Scenes: All


      .. note::

         Only the low 5 bits of the count are used for a 32-bit value, and the low 6 bits
         for a 64-bit value.

   .. function:: static Expression LessThan(IConnection connection, Expression arg0, Expression arg1)

      Less than numerical comparison.

      :parameters:

       * **arg0** -- The first operand.
       * **arg1** -- The second operand.

      :Game Scenes: All

   .. function:: static Expression LessThanOrEqual(IConnection connection, Expression arg0, Expression arg1)

      Less than or equal numerical comparison.

      :parameters:

       * **arg0** -- The first operand.
       * **arg1** -- The second operand.

      :Game Scenes: All

   .. function:: static Expression Match(IConnection connection, Expression value, System.Collections.Generic.IList<System.Collections.Generic.IList<Expression>> cases, System.Collections.Generic.IList<Expression> bodies)

      A match statement, which evaluates the first matching case.

      :parameters:

       * **value** -- The value to match.
       * **cases** -- The test values of each case.
       * **bodies** -- The statement of each case.

      :Game Scenes: All


      .. note::

         Test values compare as for :meth:`Expression.Equal`, and a case with none is the default.
         A :meth:`Expression.Break` in a body ends the enclosing loop.

   .. function:: static Expression Max(IConnection connection, Expression arg)

      The largest value of a collection.

      :parameters:

       * **arg** -- The list or set.

      :returns: The largest value in the collection.
      :Game Scenes: All


      .. note::

         Throws an exception if the collection is empty and its values are not nullable.

   .. function:: static Expression MaxBy(IConnection connection, Expression arg, Expression key)

      The value of a collection with the largest key.

      :parameters:

       * **arg** -- The collection.
       * **key** -- A function producing the key to compare values by.

      :returns: The value with the largest key.
      :Game Scenes: All


      .. note::

         Throws an exception if the collection is empty.

   .. function:: static Expression Min(IConnection connection, Expression arg)

      The smallest value of a collection.

      :parameters:

       * **arg** -- The list or set.

      :returns: The smallest value in the collection.
      :Game Scenes: All


      .. note::

         Throws an exception if the collection is empty and its values are not nullable.

   .. function:: static Expression MinBy(IConnection connection, Expression arg, Expression key)

      The value of a collection with the smallest key.

      :parameters:

       * **arg** -- The collection.
       * **key** -- A function producing the key to compare values by.

      :returns: The value with the smallest key.
      :Game Scenes: All


      .. note::

         Throws an exception if the collection is empty.

   .. function:: static Expression Modulo(IConnection connection, Expression arg0, Expression arg1)

      Numerical modulo operator.

      :parameters:

       * **arg0** -- The first operand.
       * **arg1** -- The second operand.

      :returns: The remainder of arg0 divided by arg1
      :Game Scenes: All

   .. function:: static Expression Multiply(IConnection connection, Expression arg0, Expression arg1)

      Numerical multiplication.

      :parameters:

       * **arg0** -- The first operand.
       * **arg1** -- The second operand.

      :Game Scenes: All

   .. function:: static Expression Negate(IConnection connection, Expression arg)

      Numerical negation, which negates a uint as a long.

      :parameters:

       * **arg** -- The operand.

      :Game Scenes: All

   .. function:: static Expression Not(IConnection connection, Expression arg)

      Logical or bitwise negation.

      :parameters:

       * **arg** -- The operand.

      :Game Scenes: All


      .. note::

         Also takes a value of a flags enumeration, and gives the declared values it does not hold.

   .. function:: static Expression NotEqual(IConnection connection, Expression arg0, Expression arg1)

      Inequality comparison.

      :parameters:

       * **arg0** -- The first operand.
       * **arg1** -- The second operand.

      :Game Scenes: All


      .. note::

         Collections, tuples, bytes and structures compare by their contents. Throws an
         exception if the operands are of unrelated types.

   .. function:: static Expression Or(IConnection connection, Expression arg0, Expression arg1)

      Logical or bitwise or, which evaluates both operands.

      :parameters:

       * **arg0** -- The first operand.
       * **arg1** -- The second operand.

      :Game Scenes: All


      .. note::

         Also takes two values of a flags enumeration, and gives their combination.

   .. function:: static Expression OrderBy(IConnection connection, Expression arg, Expression key)

      The values of a collection ordered by the key a function produces for each of them.

      :parameters:

       * **arg** -- The collection to order.
       * **key** -- A function producing the key to order values by.

      :returns: The ordered collection.
      :Game Scenes: All


      .. note::

         The result is lazily evaluated. :meth:`Expression.ToList` makes it concrete.

   .. function:: static Expression Parameter(IConnection connection, string name, Type type)

      A named parameter of a function.

      :parameters:

       * **name** -- The name of the parameter.
       * **type** -- The type of the parameter.

      :returns: A named parameter.
      :Game Scenes: All


      .. note::

         A value is bound to it by name with :meth:`Expression.Invoke`, or by its position in
         the parameters of a :meth:`Expression.Lambda`.

   .. property:: System.Collections.Generic.IList<Type> ParameterTypes { get; }

      The types of the function's parameters.

      :Game Scenes: All


      .. note::

         Empty for a function that is not created with :meth:`Expression.Lambda`.

   .. function:: static Expression Power(IConnection connection, Expression arg0, Expression arg1)

      Numerical power operator.

      :parameters:

       * **arg0** -- The first operand.
       * **arg1** -- The second operand.

      :returns: arg0 raised to the power of arg1, at the common type of the operands
      :Game Scenes: All

   .. function:: static Expression Remove(IConnection connection, Expression collection, Expression value)

      A removal of a value from a list or a set, or of an entry from a dictionary.

      :parameters:

       * **collection** -- The list, set or dictionary.
       * **value** -- The value to remove, or the key of the entry to remove.

      :returns: Whether the collection held the value.
      :Game Scenes: All


      .. note::

         The first occurrence is removed from a list.

   .. function:: static Expression RemoveAt(IConnection connection, Expression list, Expression index)

      A statement removing the element at an index of a list.

      :parameters:

       * **list** -- The list.
       * **index** -- The index of the element, counting from zero.

      :Game Scenes: All


      .. note::

         Throws an exception if the index is out of range.

   .. function:: static Expression Return(IConnection connection, Expression value)

      A return statement, which ends the enclosing function with a value.

      :parameters:

       * **value** -- The value to return.

      :Game Scenes: All

   .. function:: static Expression ReturnNothing(IConnection connection)

      A return statement, which ends the enclosing function with no value.


      :Game Scenes: All

   .. property:: Type ReturnType { get; }

      The type of the value the expression evaluates to. Throws an exception if the
      value cannot be sent to a client.

      :Game Scenes: All


      .. note::

         A function created with :meth:`Expression.Lambda` evaluates to the value it returns. A
         lazily evaluated collection, such as from :meth:`Expression.Select`, is sent once
         converted by :meth:`Expression.ToList` or :meth:`Expression.ToSet`.

   .. function:: static Expression Reverse(IConnection connection, Expression arg)

      The values of a collection in the opposite order.

      :parameters:

       * **arg** -- The collection.

      :returns: The reversed collection.
      :Game Scenes: All


      .. note::

         The result is lazily evaluated. :meth:`Expression.ToList` makes it concrete.

   .. function:: static Expression RightShift(IConnection connection, Expression arg0, Expression arg1)

      Bitwise right shift.

      :parameters:

       * **arg0** -- The first operand.
       * **arg1** -- The second operand.

      :Game Scenes: All


      .. note::

         Only the low 5 bits of the count are used for a 32-bit value, and the low 6 bits
         for a 64-bit value.

   .. function:: static Expression Select(IConnection connection, Expression arg, Expression func)

      The result of a function for each value of a collection.

      :parameters:

       * **arg** -- The list or set.
       * **func** -- The function, taking an element of the collection.

      :returns: The collection of function results.
      :Game Scenes: All


      .. note::

         The result is lazily evaluated. :meth:`Expression.ToList` makes it concrete.

   .. function:: static Expression SelectMany(IConnection connection, Expression arg, Expression func)

      The values of the collections a function returns for each value of a collection,
      flattened into one.

      :parameters:

       * **arg** -- The list or set.
       * **func** -- The function, taking an element of the collection and returning a collection.

      :returns: The flattened collection of function results.
      :Game Scenes: All


      .. note::

         The result is lazily evaluated. :meth:`Expression.ToList` makes it concrete.

   .. function:: static Expression Set(IConnection connection, Expression collection, Expression index, Expression value)

      A statement setting an element of a list, or an entry of a dictionary.

      :parameters:

       * **collection** -- The list or dictionary.
       * **index** -- The index of the element, counting from zero, or the key of the entry.
       * **value** -- The value to set.

      :Game Scenes: All


      .. note::

         A key the dictionary does not hold is added. Throws an exception if the index
         is out of range for the list.

   .. function:: static Expression Skip(IConnection connection, Expression arg, Expression count)

      A collection without its first count values.

      :parameters:

       * **arg** -- The collection.
       * **count** -- The number of values to skip.

      :returns: The collection without its first count values.
      :Game Scenes: All


      .. note::

         The result is lazily evaluated. :meth:`Expression.ToList` makes it concrete.

   .. function:: static Expression StateVariable(IConnection connection, string name, Expression initialValue)

      A variable that keeps its value between evaluations of a function.

      :parameters:

       * **name** -- The name of the variable.
       * **initialValue** -- The initial value of the variable.

      :returns: A state variable.
      :Game Scenes: All


      .. note::

         The initial value is evaluated once, when the variable is created, and gives the
         variable its type. The value belongs to the variable, so every function that
         contains it shares it.

   .. function:: static Expression StringConcat(IConnection connection, System.Collections.Generic.IList<Expression> args)

      The concatenation of strings.

      :parameters:

       * **args** -- The strings to concatenate, in order.

      :returns: The concatenated string.
      :Game Scenes: All


      .. note::

         Use :meth:`Expression.ConvertToString` to convert other values to strings.

   .. function:: static Expression StringContains(IConnection connection, Expression arg, Expression value)

      Whether one string occurs within another.

      :parameters:

       * **arg** -- The string to search.
       * **value** -- The string to search for.

      :returns: Whether the value occurs in the string.
      :Game Scenes: All

   .. function:: static Expression StringEndsWith(IConnection connection, Expression arg, Expression value)

      Whether a string ends with another.

      :parameters:

       * **arg** -- The string to test.
       * **value** -- The string to test for.

      :returns: Whether the string ends with the value.
      :Game Scenes: All

   .. function:: static Expression StringGet(IConnection connection, Expression arg, Expression index)

      The character at the given position in a string, as a string of length one.

      :parameters:

       * **arg** -- The string.
       * **index** -- The position of the character, counting from zero.

      :returns: The character at the given position.
      :Game Scenes: All


      .. note::

         Throws an exception if the index is out of range.

   .. function:: static Expression StringIndexOf(IConnection connection, Expression arg, Expression value)

      The position of the first occurrence of one string within another.

      :parameters:

       * **arg** -- The string to search.
       * **value** -- The string to search for.

      :returns: The position of the value, counting from zero, or -1 if it does not occur.
      :Game Scenes: All

   .. function:: static Expression StringJoin(IConnection connection, Expression separator, Expression values)

      Strings joined together, separated by another string.

      :parameters:

       * **separator** -- The string to put between the values.
       * **values** -- The strings to join.

      :returns: The joined string.
      :Game Scenes: All

   .. function:: static Expression StringLength(IConnection connection, Expression arg)

      The number of characters in a string.

      :parameters:

       * **arg** -- The string.

      :returns: The length of the string.
      :Game Scenes: All

   .. function:: static Expression StringReplace(IConnection connection, Expression arg, Expression oldValue, Expression newValue)

      A string with every occurrence of one string replaced by another.

      :parameters:

       * **arg** -- The string to search.
       * **oldValue** -- The string to replace.
       * **newValue** -- The string to replace it with.

      :returns: The string with the replacements made.
      :Game Scenes: All


      .. note::

         Throws an exception if the string to replace is empty.

   .. function:: static Expression StringSplit(IConnection connection, Expression arg, Expression separator)

      The parts of a string separated by another string.

      :parameters:

       * **arg** -- The string to split.
       * **separator** -- The string that separates the parts.

      :returns: The parts of the string.
      :Game Scenes: All

   .. function:: static Expression StringStartsWith(IConnection connection, Expression arg, Expression value)

      Whether a string starts with another.

      :parameters:

       * **arg** -- The string to test.
       * **value** -- The string to test for.

      :returns: Whether the string starts with the value.
      :Game Scenes: All

   .. function:: static Expression StringSubstring(IConnection connection, Expression arg, Expression start, Expression length)

      Part of a string.

      :parameters:

       * **arg** -- The string.
       * **start** -- The position to start at, counting from zero.
       * **length** -- The number of characters to take.

      :returns: The substring.
      :Game Scenes: All


      .. note::

         Throws an exception if the start or the length is out of range.

   .. function:: static Expression StringToLower(IConnection connection, Expression arg)

      A string in lower case, independently of the game's language.

      :parameters:

       * **arg** -- The string.

      :returns: The string in lower case.
      :Game Scenes: All

   .. function:: static Expression StringToUpper(IConnection connection, Expression arg)

      A string in upper case, independently of the game's language.

      :parameters:

       * **arg** -- The string.

      :returns: The string in upper case.
      :Game Scenes: All

   .. function:: static Expression StringTrim(IConnection connection, Expression arg)

      A string with white space removed from both ends.

      :parameters:

       * **arg** -- The string.

      :returns: The trimmed string.
      :Game Scenes: All

   .. function:: static Expression StringTrimEnd(IConnection connection, Expression arg)

      A string with white space removed from its end.

      :parameters:

       * **arg** -- The string.

      :returns: The trimmed string.
      :Game Scenes: All

   .. function:: static Expression StringTrimStart(IConnection connection, Expression arg)

      A string with white space removed from its start.

      :parameters:

       * **arg** -- The string.

      :returns: The trimmed string.
      :Game Scenes: All

   .. function:: static Expression Subtract(IConnection connection, Expression arg0, Expression arg1)

      Numerical subtraction.

      :parameters:

       * **arg0** -- The first operand.
       * **arg1** -- The second operand.

      :Game Scenes: All

   .. function:: static Expression Sum(IConnection connection, Expression arg)

      The sum of the values of a collection.

      :parameters:

       * **arg** -- The list or set.

      :returns: The sum of the elements in the collection.
      :Game Scenes: All

   .. function:: static Expression Switch(IConnection connection, Expression value, System.Collections.Generic.IList<System.Collections.Generic.IList<Expression>> cases, System.Collections.Generic.IList<Expression> bodies)

      A switch statement, which evaluates from the first matching case on.

      :parameters:

       * **value** -- The value to match.
       * **cases** -- The test values of each case.
       * **bodies** -- The statement of each case.

      :Game Scenes: All


      .. note::

         Test values compare as for :meth:`Expression.Equal`, and a case with none is the default.
         A :meth:`Expression.Break` in a body ends the switch.

   .. function:: static Expression Take(IConnection connection, Expression arg, Expression count)

      The first count values of a collection.

      :parameters:

       * **arg** -- The collection.
       * **count** -- The number of values to take.

      :returns: The first count values of the collection.
      :Game Scenes: All


      .. note::

         The result is lazily evaluated. :meth:`Expression.ToList` makes it concrete.

   .. function:: static Expression Throw(IConnection connection, string service, string name, Expression message)

      A throw statement, which raises the named exception.

      :parameters:

       * **service** -- The name of the service the exception is defined in.
       * **name** -- The name of the exception.
       * **message** -- The message of the exception.

      :Game Scenes: All


      .. note::

         The client receives the exception as an error from the function, or from the stream
         or event evaluating it. For example, the service ``KRPC`` and the name
         ``Error`` raise :type:`Error`. Throws an exception if the service does not
         declare it.

   .. function:: static Expression ThrowById(IConnection connection, uint serviceId, uint exceptionId, Expression message)

      A throw statement, which raises the exception with the given ids.

      :parameters:

       * **serviceId** -- The id of the service the exception is defined in.
       * **exceptionId** -- The id of the exception.
       * **message** -- The message of the exception.

      :Game Scenes: All


      .. note::

         Raises the exception as :meth:`Expression.Throw` does. Throws an exception if the
         service does not declare it.

   .. function:: static Expression ToList(IConnection connection, Expression arg)

      A collection as a list.

      :parameters:

       * **arg** -- The collection.

      :returns: The collection as a list.
      :Game Scenes: All

   .. function:: static Expression ToSet(IConnection connection, Expression arg)

      A collection as a set.

      :parameters:

       * **arg** -- The collection.

      :returns: The collection as a set.
      :Game Scenes: All


      .. note::

         Throws an exception if a value is null.

   .. function:: static Expression TryCatch(IConnection connection, Expression body, string service, string name, Expression message, Expression handler)

      A try-catch statement that handles the named exception.

      :parameters:

       * **body** -- The statement to evaluate.
       * **service** -- The name of the service the exception is defined in.
       * **name** -- The name of the exception.
       * **message** -- A string variable assigned the exception's message, or null.
       * **handler** -- The statement evaluated when the exception is caught.

      :Game Scenes: All


      .. note::

         Only an exception a service declares can be named, and :meth:`Expression.TryCatchAll`
         handles any exception. Throws an exception if the service does not declare it.

   .. function:: static Expression TryCatchAll(IConnection connection, Expression body, Expression message, Expression handler)

      A try-catch statement that handles any exception.

      :parameters:

       * **body** -- The statement to evaluate.
       * **message** -- A string variable assigned the exception's message, or null.
       * **handler** -- The statement evaluated when an exception is caught.

      :Game Scenes: All

   .. function:: static Expression TryCatchById(IConnection connection, Expression body, uint serviceId, uint exceptionId, Expression message, Expression handler)

      A try-catch statement that handles the exception with the given ids.

      :parameters:

       * **body** -- The statement to evaluate.
       * **serviceId** -- The id of the service the exception is defined in.
       * **exceptionId** -- The id of the exception.
       * **message** -- A string variable assigned the exception's message, or null.
       * **handler** -- The statement evaluated when the exception is caught.

      :Game Scenes: All


      .. note::

         Handles the exception as :meth:`Expression.TryCatch` does. Throws an exception if the
         service does not declare it.

   .. function:: static Expression TryFinally(IConnection connection, Expression body, Expression finalizer)

      A try-finally statement.

      :parameters:

       * **body** -- The statement to evaluate.
       * **finalizer** -- The statement evaluated after the body.

      :Game Scenes: All


      .. note::

         The finalizer is evaluated after the body, whether or not the body raises an
         exception. A break, continue or return cannot leave the finalizer.

   .. function:: static Expression Union(IConnection connection, Expression arg1, Expression arg2)

      The values in either of two collections, with duplicates removed.

      :parameters:

       * **arg1** -- The first collection.
       * **arg2** -- The second collection.

      :returns: The union of the two collections.
      :Game Scenes: All


      .. note::

         The result is lazily evaluated. :meth:`Expression.ToList` makes it concrete.

   .. function:: static Expression Variable(IConnection connection, string name, Type type)

      A local variable, declared by :meth:`Expression.BlockWithVariables`.

      :parameters:

       * **name** -- The name of the variable.
       * **type** -- The type of the variable.

      :returns: A local variable.
      :Game Scenes: All

   .. function:: static Expression Where(IConnection connection, Expression arg, Expression func)

      The values of a collection for which a predicate function returns true.

      :parameters:

       * **arg** -- The list or set.
       * **func** -- The predicate function, taking an element of the collection.

      :returns: The filtered collection.
      :Game Scenes: All


      .. note::

         The result is lazily evaluated. :meth:`Expression.ToList` makes it concrete.

   .. function:: static Expression While(IConnection connection, Expression condition, Expression body)

      A while loop, which evaluates the body while the condition is true.

      :parameters:

       * **condition** -- The boolean condition.
       * **body** -- The statement evaluated on each iteration.

      :Game Scenes: All


      .. note::

         A :meth:`Expression.Break` or :meth:`Expression.Continue` in the body applies to this loop.

   .. function:: static Expression Zip(IConnection connection, Expression arg1, Expression arg2, Expression func)

      The results of a function applied to the values of two collections in pairs.

      :parameters:

       * **arg1** -- The first collection.
       * **arg2** -- The second collection.
       * **func** -- A function combining a value from each collection.

      :returns: The combined collection.
      :Game Scenes: All


      .. note::

         The result is as long as the shorter of the two collections. It is lazily
         evaluated, and :meth:`Expression.ToList` makes it concrete.



.. class:: Type

   The type of a value a procedure takes or returns.

   .. function:: static Type Bool(IConnection connection)

      A boolean type.


      :Game Scenes: All

   .. function:: static Type Bytes(IConnection connection)

      A byte sequence type.


      :Game Scenes: All

   .. function:: static Type ClassType(IConnection connection, string service, string name)

      The type of a class defined by a service.

      :parameters:

       * **service** -- The name of the service the class is defined in.
       * **name** -- The name of the class.

      :Game Scenes: All

   .. function:: static Type ClassTypeById(IConnection connection, uint serviceId, uint classId)

      The type of a class defined by a service, named by ids.

      :parameters:

       * **serviceId** -- The id of the service the class is defined in.
       * **classId** -- The id of the class.

      :Game Scenes: All

   .. property:: TypeCode Code { get; }

      The kind of the type.

      :Game Scenes: All

   .. function:: static Type DictionaryType(IConnection connection, Type keyType, Type valueType)

      A dictionary type. Throws an exception if the key type is nullable or not a valid
      key type.

      :parameters:

       * **keyType** -- The type of the keys of the dictionary.
       * **valueType** -- The type of the values of the dictionary.

      :Game Scenes: All

   .. function:: static Type Double(IConnection connection)

      A double precision floating point number type.


      :Game Scenes: All

   .. function:: static Type EnumerationType(IConnection connection, string service, string name)

      The type of an enumeration defined by a service.

      :parameters:

       * **service** -- The name of the service the enumeration is defined in.
       * **name** -- The name of the enumeration.

      :Game Scenes: All

   .. function:: static Type EnumerationTypeById(IConnection connection, uint serviceId, uint enumerationId)

      The type of an enumeration defined by a service, named by ids.

      :parameters:

       * **serviceId** -- The id of the service the enumeration is defined in.
       * **enumerationId** -- The id of the enumeration.

      :Game Scenes: All

   .. function:: static Type Float(IConnection connection)

      A single precision floating point number type.


      :Game Scenes: All

   .. function:: static Type Int(IConnection connection)

      A signed 32-bit integer type.


      :Game Scenes: All

   .. function:: static Type ListType(IConnection connection, Type valueType)

      A list type.

      :parameters:

       * **valueType** -- The type of the elements of the list.

      :Game Scenes: All

   .. function:: static Type Long(IConnection connection)

      A signed 64-bit integer type.


      :Game Scenes: All

   .. property:: string Name { get; }

      The name of a class, enumeration or structure type, or an empty string.

      :Game Scenes: All

   .. property:: bool Nullable { get; }

      Whether a value of the type may be null.

      :Game Scenes: All


      .. note::

         Any type can be nullable. A string or class value of a type that is not nullable
         is never null.

   .. function:: static Type NullableType(IConnection connection, Type valueType)

      A nullable type, holding a value of the given type or null.

      :parameters:

       * **valueType** -- The type of the value.

      :Game Scenes: All

   .. property:: string Service { get; }

      The service a class, enumeration or structure type is defined in, or an empty string.

      :Game Scenes: All

   .. function:: static Type SetType(IConnection connection, Type valueType)

      A set type. Throws an exception if the element type is nullable.

      :parameters:

       * **valueType** -- The type of the elements of the set.

      :Game Scenes: All

   .. function:: static Type String(IConnection connection)

      A string type.


      :Game Scenes: All

   .. function:: static Type StructType(IConnection connection, string service, string name)

      The type of a structure defined by a service.

      :parameters:

       * **service** -- The name of the service the structure is defined in.
       * **name** -- The name of the structure.

      :Game Scenes: All

   .. function:: static Type StructTypeById(IConnection connection, uint serviceId, uint structId)

      The type of a structure defined by a service, named by ids.

      :parameters:

       * **serviceId** -- The id of the service the structure is defined in.
       * **structId** -- The id of the structure.

      :Game Scenes: All

   .. function:: static Type TupleType(IConnection connection, System.Collections.Generic.IList<Type> valueTypes)

      A tuple type. Throws an exception if there are no element types or more than seven.

      :parameters:

       * **valueTypes** -- The types of the elements of the tuple.

      :Game Scenes: All

   .. property:: System.Collections.Generic.IList<Type> Types { get; }

      The types of the elements of a tuple, list, set or dictionary type.

      :Game Scenes: All


      .. note::

         The value types of a tuple, the value type of a list or set, or the key and value
         types of a dictionary. An empty list for any other type.

   .. function:: static Type UInt(IConnection connection)

      An unsigned 32-bit integer type.


      :Game Scenes: All

   .. function:: static Type ULong(IConnection connection)

      An unsigned 64-bit integer type.


      :Game Scenes: All



.. enum:: GameScene

   The game scene. See :prop:`KRPC.GameScene <KRPC.KRPC.GameScene>`.


   .. enumerator:: Administration

      The administration facility. This is a pseudo-scene, shown when the
      administration facility is open within the space center scene.


   .. enumerator:: AstronautComplex

      The astronaut complex. This is a pseudo-scene, shown when the
      astronaut complex facility is open within the space center scene.


   .. enumerator:: EditorSPH

      The Space Plane Hangar.


   .. enumerator:: EditorVAB

      The Vehicle Assembly Building.


   .. enumerator:: Flight

      The game scene showing a vessel in flight (or on the launchpad/runway).


   .. enumerator:: MissionBuilder

      The mission builder.


   .. enumerator:: MissionControl

      Mission control. This is a pseudo-scene, shown when the
      mission control facility is open within the space center scene.


   .. enumerator:: ResearchAndDevelopment

      Research and development. This is a pseudo-scene, shown when the
      research and development facility is open within the space center scene.


   .. enumerator:: SpaceCenter

      The game scene showing the Kerbal Space Center buildings.


   .. enumerator:: TrackingStation

      The tracking station.



.. enum:: TypeCode

   The kind of a type, with the values of the communication protocol's type codes.


   .. enumerator:: Bool

      A boolean.


   .. enumerator:: Bytes

      A sequence of bytes.


   .. enumerator:: Class

      A class defined by a service.


   .. enumerator:: Dictionary

      A dictionary, with the types of its keys and values in :prop:`Type.Types`.


   .. enumerator:: Double

      A double precision floating point number.


   .. enumerator:: Enumeration

      An enumeration defined by a service.


   .. enumerator:: Float

      A single precision floating point number.


   .. enumerator:: List

      A list, with the type of its values in :prop:`Type.Types`.


   .. enumerator:: SInt32

      A signed 32-bit integer.


   .. enumerator:: SInt64

      A signed 64-bit integer.


   .. enumerator:: Set

      A set, with the type of its values in :prop:`Type.Types`.


   .. enumerator:: String

      A string.


   .. enumerator:: Struct

      A structure defined by a service.


   .. enumerator:: Tuple

      A tuple, with the types of its values in :prop:`Type.Types`.


   .. enumerator:: UInt32

      An unsigned 32-bit integer.


   .. enumerator:: UInt64

      An unsigned 64-bit integer.



.. struct:: ClientInfo

   A client connected to the server.

   .. note::

      See :prop:`KRPC.Clients <KRPC.KRPC.Clients>`.

   .. function:: ClientInfo(byte[] id, string name, string address, uint protocolVersion)

      Construct a ClientInfo from the values of its fields.


   .. property:: byte[] Id { get; }

      The identifier of the client.

   .. property:: string Name { get; }

      The name of the client, or an empty string if it has no name.

   .. property:: string Address { get; }

      The address of the client.

   .. property:: uint ProtocolVersion { get; }

      The protocol version of the client's connection.


.. struct:: ServerSettings

   The settings that control how the server executes calls.

   .. note::

      See :prop:`ServerStatus.Settings`.

   .. function:: ServerSettings(bool oneRpcPerUpdate, uint maxTimePerUpdate, bool adaptiveRateControl, bool blockingRecv, uint recvTimeout, uint tickHoldTimeout)

      Construct a ServerSettings from the values of its fields.


   .. property:: bool OneRpcPerUpdate { get; }

      Whether the server executes one call per update.

   .. property:: uint MaxTimePerUpdate { get; }

      The current maximum time spent executing calls per update, in microseconds.

   .. property:: bool AdaptiveRateControl { get; }

      Whether the server adjusts :prop:`ServerSettings.MaxTimePerUpdate` to hold the frame rate.

   .. property:: bool BlockingRecv { get; }

      Whether the server waits for the next call in an update.

   .. property:: uint RecvTimeout { get; }

      The time the server waits for the next call, in microseconds.

   .. property:: uint TickHoldTimeout { get; }

      The longest time a client may hold the game on one physics tick, in microseconds.


.. struct:: ServerStatistics

   The performance statistics of the server.

   .. note::

      See :prop:`ServerStatus.Statistics`.

   .. function:: ServerStatistics(ulong bytesRead, ulong bytesWritten, float bytesReadRate, float bytesWrittenRate, ulong rpcsExecuted, float rpcRate, float timePerRpcUpdate, float pollTimePerRpcUpdate, float execTimePerRpcUpdate, uint deferredCalls, uint streams, ulong streamEvaluations, float streamEvaluationRate, ulong streamUpdatesSent, float timePerStreamUpdate)

      Construct a ServerStatistics from the values of its fields.


   .. property:: ulong BytesRead { get; }

      The number of bytes received from clients.

   .. property:: ulong BytesWritten { get; }

      The number of bytes sent to clients.

   .. property:: float BytesReadRate { get; }

      The rate bytes are received from clients, in bytes per second.

   .. property:: float BytesWrittenRate { get; }

      The rate bytes are sent to clients, in bytes per second.

   .. property:: ulong RpcsExecuted { get; }

      The number of calls executed.

   .. property:: float RpcRate { get; }

      The number of calls executed per second.

   .. property:: float TimePerRpcUpdate { get; }

      The time spent receiving and executing calls per update, in seconds.

   .. property:: float PollTimePerRpcUpdate { get; }

      The time spent receiving calls per update, in seconds.

   .. property:: float ExecTimePerRpcUpdate { get; }

      The time spent executing calls per update, in seconds.

   .. property:: uint DeferredCalls { get; }

      The number of calls left for the next update, because the update ran out of time
      or the call yielded.

   .. property:: uint Streams { get; }

      The number of open streams, events included.

   .. property:: ulong StreamEvaluations { get; }

      The number of stream evaluations, one per stream per update.

   .. property:: float StreamEvaluationRate { get; }

      The number of stream evaluations per second.

   .. property:: ulong StreamUpdatesSent { get; }

      The number of stream updates sent to clients.

   .. property:: float TimePerStreamUpdate { get; }

      The time spent updating streams per update, in seconds.


.. struct:: ServerStatus

   The status of the server.

   .. note::

      See :meth:`KRPC.GetStatus <KRPC.KRPC.GetStatus>`.

   .. function:: ServerStatus(string version, uint protocolVersion, ServerSettings settings, ServerStatistics statistics)

      Construct a ServerStatus from the values of its fields.


   .. property:: string Version { get; }

      The version of the server, or ``unknown``.

   .. property:: uint ProtocolVersion { get; }

      The protocol version of the connection.

   .. property:: ServerSettings Settings { get; }

      The settings of the server.

   .. property:: ServerStatistics Statistics { get; }

      The performance statistics of the server.


.. class:: ArgumentException

   A method was invoked where at least one of the passed arguments does not
   meet the parameter specification of the method.

   The client throws :type:`System.ArgumentException` for this exception, and has no class for it.



.. class:: ArgumentNullException

   A null reference was passed to a method that does not accept it as a valid argument.

   The client throws :type:`System.ArgumentNullException` for this exception, and has no class for it.



.. class:: ArgumentOutOfRangeException

   The value of an argument is outside the allowable range of values as defined by the invoked method.

   The client throws :type:`System.ArgumentOutOfRangeException` for this exception, and has no class for it.



.. class:: DivideByZeroException

   An integer was divided by zero.

   The client throws :type:`System.DivideByZeroException` for this exception, and has no class for it.



.. class:: Error

   A general error, described by its message.

   A subclass of :type:`KRPC.Client.ServerException`.



.. class:: IndexOutOfRangeException

   An index was outside the bounds of a collection, string or array.

   The client throws :type:`System.IndexOutOfRangeException` for this exception, and has no class for it.



.. class:: InvalidOperationException

   A method call was made to a method that is invalid
   given the current state of the object.

   The client throws :type:`System.InvalidOperationException` for this exception, and has no class for it.



.. class:: KeyNotFoundException

   A key was not found in a dictionary.

   The client throws :type:`System.Collections.Generic.KeyNotFoundException` for this exception, and has no class for it.



.. class:: NotSupportedException

   A method was invoked that is not supported.

   The client throws :type:`System.NotSupportedException` for this exception, and has no class for it.



.. class:: NullReferenceException

   A value was null where its type does not allow one.

   The client throws :type:`System.NullReferenceException` for this exception, and has no class for it.



.. class:: ObjectDestroyedException

   The game object that a service object refers to no longer exists,
   so the object cannot be used. For example, the part it represents
   was destroyed, or the vessel it belongs to was recovered.

   A subclass of :type:`KRPC.Client.ServerException`.

   .. note::

      See :type:`ObjectNotLoadedException` for a game object that exists but is not
      loaded.

.. class:: ObjectNotLoadedException

   The game object that a service object refers to exists, but is not loaded.
   For example, the game is loading, or the vessel carrying the part is unloaded.

   A subclass of :type:`KRPC.Client.ServerException`.

   .. note::

      See :type:`ObjectDestroyedException` for a game object that no longer exists.

.. class:: OverflowException

   An arithmetic or conversion operation overflowed.

   The client throws :type:`System.OverflowException` for this exception, and has no class for it.



.. class:: StreamRemovedException

   A stream was removed by the server, e.g. an event that has finished.

   The client throws :type:`KRPC.Client.StreamException` for this exception, and has no class for it.



.. class:: TimeoutException

   An operation did not complete in the time allotted to it.

   The client throws :type:`System.TimeoutException` for this exception, and has no class for it.



.. class:: UnsupportedSceneException

   The procedure is not available in the current game scene.

   A subclass of :type:`KRPC.Client.ServerException`.
