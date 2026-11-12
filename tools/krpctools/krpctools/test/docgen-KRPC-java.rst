.. default-domain:: java
.. highlight:: java

.. package:: krpc.client.services.KRPC


.. type:: public class KRPC

   Main kRPC service, used by clients to interact with basic server functionality.

   .. method:: java.util.List<ClientInfo> getClients()


      The clients that are currently connected to the server.

   

   .. method:: GameScene getGameScene()

   .. method:: void setGameScene(GameScene value)

      The current game scene. Setting this switches the game to the given
      scene, or opens/closes the corresponding facility for the pseudo-scenes.
      Scene changes happen asynchronously: setting this property returns
      immediately, and clients should poll it until it reports the requested
      scene. Setting it to :field:`GameScene.FLIGHT` resumes the save's
      active vessel, and fails if there is none. Moving between
      :field:`GameScene.EDITOR_VAB` and :field:`GameScene.EDITOR_SPH`
      keeps the vessel that is being constructed, as the game's own switch
      editor button does. Leaving flight saves the game first, unless the game
      cannot be saved, e.g. while under acceleration.

   

   .. method:: int getGameStateLoads()


      The number of game states that have finished loading.

   

      .. note::

         Increases once a game load, quickload, revert or scene change has finished.

   .. method:: byte[] getClientID()

      Returns the identifier for the current client.

   

   .. method:: String getClientName()

      Returns the name of the current client.
      This is an empty string if the client has no name.

   

   .. method:: krpc.schema.KRPC.Services getServices()

      The services the server provides, with their procedures, classes, enumerations,
      structures and exceptions.

   

   .. method:: ServerStatus getStatus()

      Returns the status of the server, with its version, settings and performance statistics.

   

   .. method:: void holdTick()

      Hold the game on its current physics tick until :meth:`KRPC.releaseTick()` is called
      or the hold times out. Every call made in between is executed before the game
      advances, so a read, compute and write all happen in the state of one tick.

   

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

   .. method:: void nextTick()

      Let the game move on from the tick being held and hold the one after it, so that a
      program acts on consecutive physics ticks. Holds the soonest tick it can if no tick
      is currently being held.

   

      .. note::

         A release and a hold made as two separate calls let the game advance between them.
         This takes the next tick without that gap. Call it at the top of a loop that has to
         act on every tick, work, and come back to it.

         The game advances one tick per call, so it runs at the rate of the loop for as long
         as the loop runs. End the loop with :meth:`KRPC.releaseTick()`.

         Every limit of :meth:`KRPC.holdTick()` applies, the timeout included. A hold lost to
         the timeout is not reported, and the next call takes whatever tick it lands on.

   .. method:: boolean getPaused()

   .. method:: void setPaused(boolean value)

      Whether the game is paused.

   

   .. method:: void releaseTick()

      Let the game move on from the tick that :meth:`KRPC.holdTick()` held it on. Does
      nothing when no tick is held, which is the case once a hold has timed out.

   



.. type:: public class Expression

   A server side expression.

   .. note::

      Numeric operands of differing types are converted to a common type.

   .. method:: static Expression add(Connection connection, Expression arg0, Expression arg1)

      Numerical addition.

      :param Expression arg0: The first operand.
      :param Expression arg1: The second operand.
   

   .. method:: static Expression aggregate(Connection connection, Expression arg, Expression func)

      The result of applying an accumulator function over a collection.

      :param Expression arg: The collection.
      :param Expression func: The accumulator function.
      :returns: The accumulated value.
   

      .. note::

         Throws an exception if the collection is empty.

   .. method:: static Expression aggregateWithSeed(Connection connection, Expression arg, Expression seed, Expression func)

      The result of applying an accumulator function over a collection, starting from a seed.

      :param Expression arg: The collection.
      :param Expression seed: The seed value.
      :param Expression func: The accumulator function.
      :returns: The accumulated value.
   

      .. note::

         The seed widens to the type of the accumulated value, the function's result.

   .. method:: static Expression all(Connection connection, Expression arg, Expression predicate)

      Whether a predicate function returns true for every value of a collection.

      :param Expression arg: The collection.
      :param Expression predicate: The predicate function.
      :returns: Whether all items satisfy the predicate.
   

   .. method:: static Expression and(Connection connection, Expression arg0, Expression arg1)

      Logical or bitwise and, which evaluates both operands.

      :param Expression arg0: The first operand.
      :param Expression arg1: The second operand.
   

      .. note::

         Also takes two values of a flags enumeration, and gives the values they share.

   .. method:: static Expression any(Connection connection, Expression arg, Expression predicate)

      Whether a predicate function returns true for any value of a collection.

      :param Expression arg: The collection.
      :param Expression predicate: The predicate function.
      :returns: Whether any item satisfies the predicate.
   

   .. method:: static Expression append(Connection connection, Expression collection, Expression value)

      A statement adding a value to a list or a set.

      :param Expression collection: The list or set to add to.
      :param Expression value: The value to add.
   

      .. note::

         The value goes on the end of a list. A set holding the value is left unchanged.

   .. method:: static Expression assign(Connection connection, Expression variable, Expression value)

      An assignment to a local variable, state variable or function parameter.

      :param Expression variable: The variable to assign to.
      :param Expression value: The value to assign.
   

      .. note::

         The value's type must be assignable to the variable's type. A numeric value of a
         different type is widened, and narrowing it requires a cast.

   .. method:: static Expression average(Connection connection, Expression arg)

      The average of the values of a collection.

      :param Expression arg: The list or set.
      :returns: The average of the elements in the collection.
   

      .. note::

         Throws an exception if the collection is empty and its values are not nullable.

   .. method:: static Expression block(Connection connection, java.util.List<Expression> statements)

      A block of statements, whose value is that of its last statement.

      :param java.util.List<Expression> statements: The statements.
   

   .. method:: static Expression blockWithVariables(Connection connection, java.util.List<Expression> variables, java.util.List<Expression> statements)

      A block of statements with local variables.

      :param java.util.List<Expression> variables: The local variables of the block.
      :param java.util.List<Expression> statements: The statements.
   

      .. note::

         The value of the block is that of its last statement. The variables are in scope
         for the statements of the block, including within nested functions.

   .. method:: static Expression break_(Connection connection)

      A break statement, which ends the enclosing loop.

   

   .. method:: static Expression buildDictionary(Connection connection, Expression arg, Expression keyFunc, Expression valueFunc)

      A dictionary built from a collection, with a key and a value computed from each
      element.

      :param Expression arg: The list or set.
      :param Expression keyFunc: The function computing an element's key.
      :param Expression valueFunc: The function computing an element's value.
      :returns: The dictionary.
   

      .. note::

         A repeated key takes the last of its values.

   .. method:: static Expression call(Connection connection, krpc.schema.KRPC.ProcedureCall call)

      An RPC call.

      :param krpc.schema.KRPC.ProcedureCall call: The procedure call.
   

      .. note::

         The instance the call is made on, and the values of its arguments, are fixed
         when the expression is created. A call to a procedure with no return value is
         evaluated for its effects, as a statement in a :meth:`Expression.block(java.util.List<Expression>)`. A collection the
         procedure returns is copied, as a client would receive it.

   .. method:: static Expression callWithArguments(Connection connection, krpc.schema.KRPC.ProcedureCall call, java.util.Map<Integer,Expression> args)

      An RPC call, where some or all of the arguments are computed by expressions.

      :param krpc.schema.KRPC.ProcedureCall call: The RPC to call.
      :param java.util.Map<Integer,Expression> args: Expressions computing the call's arguments, by position.
   

      .. note::

         Position 0 is the instance of a class method or property. A position with no
         expression takes the argument encoded in the call, or the parameter's default. A
         collection the procedure returns is copied, as a client would receive it.

   .. method:: static Expression cast(Connection connection, Expression arg, Type type)

      A value cast to the given type.

      :param Expression arg: The value to cast.
      :param Type type: Type to cast the argument to.
   

      .. note::

         A tuple or collection is cast element by element.

   .. method:: static Expression clear(Connection connection, Expression collection)

      A statement removing every value from a list or a set, or every entry from a dictionary.

      :param Expression collection: The list, set or dictionary.
   

   .. method:: static Expression concat(Connection connection, Expression arg1, Expression arg2)

      The values of one collection followed by those of another.

      :param Expression arg1: The first collection.
      :param Expression arg2: The second collection.
      :returns: The first collection followed by the second.
   

      .. note::

         The result is lazily evaluated. :meth:`Expression.toList(Expression)` makes it concrete.

   .. method:: static Expression conditional(Connection connection, Expression condition, Expression ifTrue, Expression ifFalse)

      The second argument if the condition is true, and the third argument otherwise.

      :param Expression condition: The boolean condition.
      :param Expression ifTrue: The value when the condition is true.
      :param Expression ifFalse: The value when the condition is false.
   

      .. note::

         Throws an exception if the two values have no common type.

   .. method:: static Expression conditionalAnd(Connection connection, Expression arg0, Expression arg1)

      Logical and, which evaluates the second operand only when the first is true.

      :param Expression arg0: The first operand.
      :param Expression arg1: The second operand.
   

   .. method:: static Expression conditionalOr(Connection connection, Expression arg0, Expression arg1)

      Logical or, which evaluates the second operand only when the first is false.

      :param Expression arg0: The first operand.
      :param Expression arg1: The second operand.
   

   .. method:: static Expression constantBool(Connection connection, boolean value)

      A constant value of boolean type.

      :param boolean value: The value.
   

   .. method:: static Expression constantBytes(Connection connection, byte[] value)

      A constant value of bytes type.

      :param byte[] value: The value.
   

   .. method:: static Expression constantDouble(Connection connection, double value)

      A constant value of double precision floating point type.

      :param double value: The value.
   

   .. method:: static Expression constantEnum(Connection connection, String service, String name, int value)

      A constant value of an enumeration a service defines.

      :param String service: The name of the service the enumeration is defined in.
      :param String name: The name of the enumeration.
      :param int value: The value of the member of the enumeration.
   

   .. method:: static Expression constantFloat(Connection connection, float value)

      A constant value of single precision floating point type.

      :param float value: The value.
   

   .. method:: static Expression constantInt(Connection connection, int value)

      A constant value of integer type.

      :param int value: The value.
   

   .. method:: static Expression constantLong(Connection connection, long value)

      A constant value of long integer type.

      :param long value: The value.
   

   .. method:: static Expression constantNull(Connection connection, Type type)

      A null constant of the given type.

      :param Type type: The type of the value.
   

      .. note::

         The type is made nullable, as by :meth:`Type.nullableType(Type)`.

   .. method:: static Expression constantObject(Connection connection, long value)

      A constant object, an instance of a class a service defines.

      :param long value: The object identifier of the object.
   

      .. note::

         The object is given by its object identifier.

   .. method:: static Expression constantString(Connection connection, String value)

      A constant value of string type.

      :param String value: The value.
   

   .. method:: static Expression constantUInt(Connection connection, int value)

      A constant value of unsigned integer type.

      :param int value: The value.
   

   .. method:: static Expression constantULong(Connection connection, long value)

      A constant value of unsigned long integer type.

      :param long value: The value.
   

   .. method:: static Expression contains(Connection connection, Expression arg, Expression value)

      Whether a collection contains a value.

      :param Expression arg: The collection.
      :param Expression value: The value to look for.
      :returns: Whether the collection contains the value.
   

   .. method:: static Expression containsKey(Connection connection, Expression dictionary, Expression key)

      Whether a dictionary contains a key.

      :param Expression dictionary: The dictionary.
      :param Expression key: The key to look for.
      :returns: Whether the dictionary contains the key.
   

   .. method:: static Expression continue_(Connection connection)

      A continue statement, which skips to the next iteration of the enclosing loop.

   

   .. method:: static Expression convertToString(Connection connection, Expression arg)

      The string representation of a value.

      :param Expression arg: The value to convert.
      :returns: The string representation of the value.
   

      .. note::

         A value other than a number, boolean, string or enumeration converts as .NET
         formats it, which for a collection is the name of its type. Throws an exception if
         the value is null.

   .. method:: static Expression count(Connection connection, Expression arg)

      The number of values in a collection.

      :param Expression arg: The list, set or dictionary.
      :returns: The number of elements in the collection.
   

   .. method:: static Expression createDictionary(Connection connection, java.util.List<Expression> keys, java.util.List<Expression> values)

      A dictionary of the given keys and corresponding values, with numbers widened to
      their common type. Throws an exception if there are no keys, the counts differ, the
      keys are not of a valid key type, or a key is null.

      :param java.util.List<Expression> keys: The keys.
      :param java.util.List<Expression> values: The values.
      :returns: The dictionary.
   

      .. note::

         A repeated key takes the last of its values.

   .. method:: static Expression createEmptyDictionary(Connection connection, Type keyType, Type valueType)

      An empty dictionary of the given key and value types. Throws an exception if the
      key type is nullable or not a valid key type.

      :param Type keyType: The type of the dictionary's keys.
      :param Type valueType: The type of the dictionary's values.
      :returns: The empty dictionary.
   

   .. method:: static Expression createEmptyList(Connection connection, Type valueType)

      An empty list of the given value type.

      :param Type valueType: The type of the values the list holds.
      :returns: The empty list.
   

   .. method:: static Expression createEmptySet(Connection connection, Type valueType)

      An empty set of the given value type. Throws an exception if the value type is
      nullable.

      :param Type valueType: The type of the values the set holds.
      :returns: The empty set.
   

   .. method:: static Expression createList(Connection connection, java.util.List<Expression> values)

      A list of the given values, with numbers widened to their common type. Throws an
      exception if there are no values.

      :param java.util.List<Expression> values: The values.
      :returns: The list.
   

   .. method:: static Expression createSet(Connection connection, java.util.Set<Expression> values)

      A set of the given values, with numbers widened to their common type. Throws an
      exception if there are no values, or a value is null.

      :param java.util.Set<Expression> values: The values.
      :returns: The set.
   

      .. note::

         The values are evaluated in an unspecified order.

   .. method:: static Expression createStruct(Connection connection, Type type, java.util.List<Expression> fieldValues)

      A structure with the given field values. Throws an exception if the field values do
      not match its fields.

      :param Type type: The type of the structure.
      :param java.util.List<Expression> fieldValues: The values of the structure's fields, in the order the structure declares them.
      :returns: The structure.
   

   .. method:: static Expression createTuple(Connection connection, java.util.List<Expression> elements)

      A tuple of the given elements. Throws an exception if there are no elements or more
      than seven.

      :param java.util.List<Expression> elements: The elements.
      :returns: The tuple.
   

   .. method:: static Expression dictionaryKeys(Connection connection, Expression dictionary)

      The keys of a dictionary, as a list.

      :param Expression dictionary: The dictionary.
      :returns: The keys of the dictionary.
   

      .. note::

         The collection operations take the keys of a dictionary from here, and its values
         from :meth:`Expression.dictionaryValues(Expression)`.

   .. method:: static Expression dictionaryValues(Connection connection, Expression dictionary)

      The values of a dictionary, as a list.

      :param Expression dictionary: The dictionary.
      :returns: The values of the dictionary.
   

   .. method:: static Expression distinct(Connection connection, Expression arg)

      The values of a collection with duplicates removed, keeping the first
      occurrence of each.

      :param Expression arg: The collection.
      :returns: The distinct values of the collection.
   

      .. note::

         The result is lazily evaluated. :meth:`Expression.toList(Expression)` makes it concrete.

   .. method:: static Expression divide(Connection connection, Expression arg0, Expression arg1)

      Numerical division.

      :param Expression arg0: The first operand.
      :param Expression arg1: The second operand.
   

   .. method:: static Expression elementAt(Connection connection, Expression arg, Expression index)

      The value at the given position in a collection.

      :param Expression arg: The collection.
      :param Expression index: The position of the value, counting from zero.
      :returns: The value at the given position.
   

      .. note::

         Throws an exception if the index is out of range.

   .. method:: static Expression equal(Connection connection, Expression arg0, Expression arg1)

      Equality comparison.

      :param Expression arg0: The first operand.
      :param Expression arg1: The second operand.
   

      .. note::

         Collections, tuples, bytes and structures compare by their contents. Throws an
         exception if the operands are of unrelated types.

   .. method:: static Expression except(Connection connection, Expression arg1, Expression arg2)

      The values of the first collection that are not in the second, with
      duplicates removed.

      :param Expression arg1: The first collection.
      :param Expression arg2: The second collection.
      :returns: The difference between the two collections.
   

      .. note::

         The result is lazily evaluated. :meth:`Expression.toList(Expression)` makes it concrete.

   .. method:: static Expression exclusiveOr(Connection connection, Expression arg0, Expression arg1)

      Logical or bitwise exclusive or.

      :param Expression arg0: The first operand.
      :param Expression arg1: The second operand.
   

      .. note::

         Also takes two values of a flags enumeration, and gives the values in only one of them.

   .. method:: static Expression first(Connection connection, Expression arg)

      The first value of a collection.

      :param Expression arg: The collection.
      :returns: The first value.
   

      .. note::

         Throws an exception if the collection is empty.

   .. method:: static Expression forEach(Connection connection, Expression variable, Expression collection, Expression body)

      A loop that evaluates the body once for each value of a collection.

      :param Expression variable: The loop variable.
      :param Expression collection: The collection to iterate over.
      :param Expression body: The statement evaluated on each iteration.
   

      .. note::

         The loop variable is a local variable declared by an enclosing
         :meth:`Expression.blockWithVariables(java.util.List<Expression>, java.util.List<Expression>)`, or a parameter of an enclosing :meth:`Expression.lambda(java.util.List<Expression>, Expression)`.
         A :meth:`Expression.break_()` or :meth:`Expression.continue_()` in the body applies to this loop.

   .. method:: static Expression get(Connection connection, Expression arg, Expression index)

      An element of a tuple, list or dictionary.

      :param Expression arg: The tuple, list or dictionary.
      :param Expression index: The index or key of the element.
      :returns: The element.
   

      .. note::

         A tuple takes a constant integer index, a list a zero indexed integer and a
         dictionary a key. Throws an exception if the index is out of range or the key is
         missing, and for a tuple when the expression is created.

   .. method:: static Expression getField(Connection connection, Expression arg, String name)

      The value of a field of a structure. Throws an exception if the value is not a
      structure, or has no field of the given name.

      :param Expression arg: The structure.
      :param String name: The name of the field to access.
      :returns: The value of the field.
   

   .. method:: static Expression greaterThan(Connection connection, Expression arg0, Expression arg1)

      Greater than numerical comparison.

      :param Expression arg0: The first operand.
      :param Expression arg1: The second operand.
   

   .. method:: static Expression greaterThanOrEqual(Connection connection, Expression arg0, Expression arg1)

      Greater than or equal numerical comparison.

      :param Expression arg0: The first operand.
      :param Expression arg1: The second operand.
   

   .. method:: static Expression groupBy(Connection connection, Expression arg, Expression key)

      The values of a collection grouped by the key a function produces for each of them.

      :param Expression arg: The collection.
      :param Expression key: A function producing the key to group values by.
      :returns: A dictionary of each key to the values that produced it.
   

   .. method:: boolean getHasReturnType()


      Whether the expression evaluates to a value.

   

      .. note::

         An expression with no value has no :meth:`Expression.getReturnType()`. An empty
         collection and no value both encode to an empty sequence of bytes.

   .. method:: static Expression ifThen(Connection connection, Expression condition, Expression body)

      An if statement, with no value.

      :param Expression condition: The boolean condition.
      :param Expression body: The statement evaluated when the condition is true.
   

      .. note::

         Use :meth:`Expression.conditional(Expression, Expression, Expression)` for an if-then-else that produces a value.

   .. method:: static Expression ifThenElse(Connection connection, Expression condition, Expression body, Expression elseBody)

      An if-else statement, with no value.

      :param Expression condition: The boolean condition.
      :param Expression body: The statement evaluated when the condition is true.
      :param Expression elseBody: The statement evaluated when the condition is false.
   

      .. note::

         Use :meth:`Expression.conditional(Expression, Expression, Expression)` for an if-then-else that produces a value.

   .. method:: static Expression intersect(Connection connection, Expression arg1, Expression arg2)

      The values in both of two collections, with duplicates removed.

      :param Expression arg1: The first collection.
      :param Expression arg2: The second collection.
      :returns: The intersection of the two collections.
   

      .. note::

         The result is lazily evaluated. :meth:`Expression.toList(Expression)` makes it concrete.

   .. method:: static Expression invoke(Connection connection, Expression function, java.util.Map<String,Expression> args)

      A function call.

      :param Expression function: The function to call.
      :param java.util.Map<String,Expression> args: The arguments to call the function with.
      :returns: A function call.
   

   .. method:: static Expression isNull(Connection connection, Expression value)

      Whether a value is null. Throws an exception if the type of the value cannot be null.

      :param Expression value: The value.
   

   .. method:: static Expression lambda(Connection connection, java.util.List<Expression> parameters, Expression body)

      A function with the given parameters and body.

      :param java.util.List<Expression> parameters: The parameters of the function.
      :param Expression body: The body of the function.
      :returns: A function.
   

      .. note::

         The body is an expression or a block of statements, and a body with no value is
         evaluated for its effects. A :meth:`Expression.return_(Expression)` or :meth:`Expression.returnNothing()`
         statement ends the evaluation.

   .. method:: static Expression last(Connection connection, Expression arg)

      The last value of a collection.

      :param Expression arg: The collection.
      :returns: The last value.
   

      .. note::

         Throws an exception if the collection is empty.

   .. method:: static Expression leftShift(Connection connection, Expression arg0, Expression arg1)

      Bitwise left shift.

      :param Expression arg0: The first operand.
      :param Expression arg1: The second operand.
   

      .. note::

         Only the low 5 bits of the count are used for a 32-bit value, and the low 6 bits
         for a 64-bit value.

   .. method:: static Expression lessThan(Connection connection, Expression arg0, Expression arg1)

      Less than numerical comparison.

      :param Expression arg0: The first operand.
      :param Expression arg1: The second operand.
   

   .. method:: static Expression lessThanOrEqual(Connection connection, Expression arg0, Expression arg1)

      Less than or equal numerical comparison.

      :param Expression arg0: The first operand.
      :param Expression arg1: The second operand.
   

   .. method:: static Expression max(Connection connection, Expression arg)

      The largest value of a collection.

      :param Expression arg: The list or set.
      :returns: The largest value in the collection.
   

      .. note::

         Throws an exception if the collection is empty and its values are not nullable.

   .. method:: static Expression maxBy(Connection connection, Expression arg, Expression key)

      The value of a collection with the largest key.

      :param Expression arg: The collection.
      :param Expression key: A function producing the key to compare values by.
      :returns: The value with the largest key.
   

      .. note::

         Throws an exception if the collection is empty.

   .. method:: static Expression min(Connection connection, Expression arg)

      The smallest value of a collection.

      :param Expression arg: The list or set.
      :returns: The smallest value in the collection.
   

      .. note::

         Throws an exception if the collection is empty and its values are not nullable.

   .. method:: static Expression minBy(Connection connection, Expression arg, Expression key)

      The value of a collection with the smallest key.

      :param Expression arg: The collection.
      :param Expression key: A function producing the key to compare values by.
      :returns: The value with the smallest key.
   

      .. note::

         Throws an exception if the collection is empty.

   .. method:: static Expression modulo(Connection connection, Expression arg0, Expression arg1)

      Numerical modulo operator.

      :param Expression arg0: The first operand.
      :param Expression arg1: The second operand.
      :returns: The remainder of arg0 divided by arg1
   

   .. method:: static Expression multiply(Connection connection, Expression arg0, Expression arg1)

      Numerical multiplication.

      :param Expression arg0: The first operand.
      :param Expression arg1: The second operand.
   

   .. method:: static Expression negate(Connection connection, Expression arg)

      Numerical negation, which negates a uint as a long.

      :param Expression arg: The operand.
   

   .. method:: static Expression not(Connection connection, Expression arg)

      Logical or bitwise negation.

      :param Expression arg: The operand.
   

      .. note::

         Also takes a value of a flags enumeration, and gives the declared values it does not hold.

   .. method:: static Expression notEqual(Connection connection, Expression arg0, Expression arg1)

      Inequality comparison.

      :param Expression arg0: The first operand.
      :param Expression arg1: The second operand.
   

      .. note::

         Collections, tuples, bytes and structures compare by their contents. Throws an
         exception if the operands are of unrelated types.

   .. method:: static Expression or(Connection connection, Expression arg0, Expression arg1)

      Logical or bitwise or, which evaluates both operands.

      :param Expression arg0: The first operand.
      :param Expression arg1: The second operand.
   

      .. note::

         Also takes two values of a flags enumeration, and gives their combination.

   .. method:: static Expression orderBy(Connection connection, Expression arg, Expression key)

      The values of a collection ordered by the key a function produces for each of them.

      :param Expression arg: The collection to order.
      :param Expression key: A function producing the key to order values by.
      :returns: The ordered collection.
   

      .. note::

         The result is lazily evaluated. :meth:`Expression.toList(Expression)` makes it concrete.

   .. method:: static Expression parameter(Connection connection, String name, Type type)

      A named parameter of a function.

      :param String name: The name of the parameter.
      :param Type type: The type of the parameter.
      :returns: A named parameter.
   

      .. note::

         A value is bound to it by name with :meth:`Expression.invoke(Expression, java.util.Map<String,Expression>)`, or by its position in
         the parameters of a :meth:`Expression.lambda(java.util.List<Expression>, Expression)`.

   .. method:: java.util.List<Type> getParameterTypes()


      The types of the function's parameters.

   

      .. note::

         Empty for a function that is not created with :meth:`Expression.lambda(java.util.List<Expression>, Expression)`.

   .. method:: static Expression power(Connection connection, Expression arg0, Expression arg1)

      Numerical power operator.

      :param Expression arg0: The first operand.
      :param Expression arg1: The second operand.
      :returns: arg0 raised to the power of arg1, at the common type of the operands
   

   .. method:: static Expression remove(Connection connection, Expression collection, Expression value)

      A removal of a value from a list or a set, or of an entry from a dictionary.

      :param Expression collection: The list, set or dictionary.
      :param Expression value: The value to remove, or the key of the entry to remove.
      :returns: Whether the collection held the value.
   

      .. note::

         The first occurrence is removed from a list.

   .. method:: static Expression removeAt(Connection connection, Expression list, Expression index)

      A statement removing the element at an index of a list.

      :param Expression list: The list.
      :param Expression index: The index of the element, counting from zero.
   

      .. note::

         Throws an exception if the index is out of range.

   .. method:: static Expression return_(Connection connection, Expression value)

      A return statement, which ends the enclosing function with a value.

      :param Expression value: The value to return.
   

   .. method:: static Expression returnNothing(Connection connection)

      A return statement, which ends the enclosing function with no value.

   

   .. method:: Type getReturnType()


      The type of the value the expression evaluates to. Throws an exception if the
      value cannot be sent to a client.

   

      .. note::

         A function created with :meth:`Expression.lambda(java.util.List<Expression>, Expression)` evaluates to the value it returns. A
         lazily evaluated collection, such as from :meth:`Expression.select(Expression, Expression)`, is sent once
         converted by :meth:`Expression.toList(Expression)` or :meth:`Expression.toSet(Expression)`.

   .. method:: static Expression reverse(Connection connection, Expression arg)

      The values of a collection in the opposite order.

      :param Expression arg: The collection.
      :returns: The reversed collection.
   

      .. note::

         The result is lazily evaluated. :meth:`Expression.toList(Expression)` makes it concrete.

   .. method:: static Expression rightShift(Connection connection, Expression arg0, Expression arg1)

      Bitwise right shift.

      :param Expression arg0: The first operand.
      :param Expression arg1: The second operand.
   

      .. note::

         Only the low 5 bits of the count are used for a 32-bit value, and the low 6 bits
         for a 64-bit value.

   .. method:: static Expression select(Connection connection, Expression arg, Expression func)

      The result of a function for each value of a collection.

      :param Expression arg: The list or set.
      :param Expression func: The function, taking an element of the collection.
      :returns: The collection of function results.
   

      .. note::

         The result is lazily evaluated. :meth:`Expression.toList(Expression)` makes it concrete.

   .. method:: static Expression selectMany(Connection connection, Expression arg, Expression func)

      The values of the collections a function returns for each value of a collection,
      flattened into one.

      :param Expression arg: The list or set.
      :param Expression func: The function, taking an element of the collection and returning a collection.
      :returns: The flattened collection of function results.
   

      .. note::

         The result is lazily evaluated. :meth:`Expression.toList(Expression)` makes it concrete.

   .. method:: static Expression set(Connection connection, Expression collection, Expression index, Expression value)

      A statement setting an element of a list, or an entry of a dictionary.

      :param Expression collection: The list or dictionary.
      :param Expression index: The index of the element, counting from zero, or the key of the entry.
      :param Expression value: The value to set.
   

      .. note::

         A key the dictionary does not hold is added. Throws an exception if the index
         is out of range for the list.

   .. method:: static Expression skip(Connection connection, Expression arg, Expression count)

      A collection without its first count values.

      :param Expression arg: The collection.
      :param Expression count: The number of values to skip.
      :returns: The collection without its first count values.
   

      .. note::

         The result is lazily evaluated. :meth:`Expression.toList(Expression)` makes it concrete.

   .. method:: static Expression stateVariable(Connection connection, String name, Expression initialValue)

      A variable that keeps its value between evaluations of a function.

      :param String name: The name of the variable.
      :param Expression initialValue: The initial value of the variable.
      :returns: A state variable.
   

      .. note::

         The initial value is evaluated once, when the variable is created, and gives the
         variable its type. The value belongs to the variable, so every function that
         contains it shares it.

   .. method:: static Expression stringConcat(Connection connection, java.util.List<Expression> args)

      The concatenation of strings.

      :param java.util.List<Expression> args: The strings to concatenate, in order.
      :returns: The concatenated string.
   

      .. note::

         Use :meth:`Expression.convertToString(Expression)` to convert other values to strings.

   .. method:: static Expression stringContains(Connection connection, Expression arg, Expression value)

      Whether one string occurs within another.

      :param Expression arg: The string to search.
      :param Expression value: The string to search for.
      :returns: Whether the value occurs in the string.
   

   .. method:: static Expression stringEndsWith(Connection connection, Expression arg, Expression value)

      Whether a string ends with another.

      :param Expression arg: The string to test.
      :param Expression value: The string to test for.
      :returns: Whether the string ends with the value.
   

   .. method:: static Expression stringGet(Connection connection, Expression arg, Expression index)

      The character at the given position in a string, as a string of length one.

      :param Expression arg: The string.
      :param Expression index: The position of the character, counting from zero.
      :returns: The character at the given position.
   

      .. note::

         Throws an exception if the index is out of range.

   .. method:: static Expression stringIndexOf(Connection connection, Expression arg, Expression value)

      The position of the first occurrence of one string within another.

      :param Expression arg: The string to search.
      :param Expression value: The string to search for.
      :returns: The position of the value, counting from zero, or -1 if it does not occur.
   

   .. method:: static Expression stringJoin(Connection connection, Expression separator, Expression values)

      Strings joined together, separated by another string.

      :param Expression separator: The string to put between the values.
      :param Expression values: The strings to join.
      :returns: The joined string.
   

   .. method:: static Expression stringLength(Connection connection, Expression arg)

      The number of characters in a string.

      :param Expression arg: The string.
      :returns: The length of the string.
   

   .. method:: static Expression stringReplace(Connection connection, Expression arg, Expression oldValue, Expression newValue)

      A string with every occurrence of one string replaced by another.

      :param Expression arg: The string to search.
      :param Expression oldValue: The string to replace.
      :param Expression newValue: The string to replace it with.
      :returns: The string with the replacements made.
   

      .. note::

         Throws an exception if the string to replace is empty.

   .. method:: static Expression stringSplit(Connection connection, Expression arg, Expression separator)

      The parts of a string separated by another string.

      :param Expression arg: The string to split.
      :param Expression separator: The string that separates the parts.
      :returns: The parts of the string.
   

   .. method:: static Expression stringStartsWith(Connection connection, Expression arg, Expression value)

      Whether a string starts with another.

      :param Expression arg: The string to test.
      :param Expression value: The string to test for.
      :returns: Whether the string starts with the value.
   

   .. method:: static Expression stringSubstring(Connection connection, Expression arg, Expression start, Expression length)

      Part of a string.

      :param Expression arg: The string.
      :param Expression start: The position to start at, counting from zero.
      :param Expression length: The number of characters to take.
      :returns: The substring.
   

      .. note::

         Throws an exception if the start or the length is out of range.

   .. method:: static Expression stringToLower(Connection connection, Expression arg)

      A string in lower case, independently of the game's language.

      :param Expression arg: The string.
      :returns: The string in lower case.
   

   .. method:: static Expression stringToUpper(Connection connection, Expression arg)

      A string in upper case, independently of the game's language.

      :param Expression arg: The string.
      :returns: The string in upper case.
   

   .. method:: static Expression stringTrim(Connection connection, Expression arg)

      A string with white space removed from both ends.

      :param Expression arg: The string.
      :returns: The trimmed string.
   

   .. method:: static Expression stringTrimEnd(Connection connection, Expression arg)

      A string with white space removed from its end.

      :param Expression arg: The string.
      :returns: The trimmed string.
   

   .. method:: static Expression stringTrimStart(Connection connection, Expression arg)

      A string with white space removed from its start.

      :param Expression arg: The string.
      :returns: The trimmed string.
   

   .. method:: static Expression subtract(Connection connection, Expression arg0, Expression arg1)

      Numerical subtraction.

      :param Expression arg0: The first operand.
      :param Expression arg1: The second operand.
   

   .. method:: static Expression sum(Connection connection, Expression arg)

      The sum of the values of a collection.

      :param Expression arg: The list or set.
      :returns: The sum of the elements in the collection.
   

   .. method:: static Expression take(Connection connection, Expression arg, Expression count)

      The first count values of a collection.

      :param Expression arg: The collection.
      :param Expression count: The number of values to take.
      :returns: The first count values of the collection.
   

      .. note::

         The result is lazily evaluated. :meth:`Expression.toList(Expression)` makes it concrete.

   .. method:: static Expression throw_(Connection connection, String service, String name, Expression message)

      A throw statement, which raises the named exception.

      :param String service: The name of the service the exception is defined in.
      :param String name: The name of the exception.
      :param Expression message: The message of the exception.
   

      .. note::

         The client receives the exception as an error from the function, or from the stream
         or event evaluating it. For example, the service ``KRPC`` and the name
         ``Error`` raise :type:`Error`. Throws an exception if the service does not
         declare it.

   .. method:: static Expression toList(Connection connection, Expression arg)

      A collection as a list.

      :param Expression arg: The collection.
      :returns: The collection as a list.
   

   .. method:: static Expression toSet(Connection connection, Expression arg)

      A collection as a set.

      :param Expression arg: The collection.
      :returns: The collection as a set.
   

      .. note::

         Throws an exception if a value is null.

   .. method:: static Expression tryCatch(Connection connection, Expression body, String service, String name, Expression message, Expression handler)

      A try-catch statement that handles the named exception.

      :param Expression body: The statement to evaluate.
      :param String service: The name of the service the exception is defined in.
      :param String name: The name of the exception.
      :param Expression message: A string variable assigned the exception's message, or null.
      :param Expression handler: The statement evaluated when the exception is caught.
   

      .. note::

         Only an exception a service declares can be named, and :meth:`Expression.tryCatchAll(Expression, Expression, Expression)`
         handles any exception. Throws an exception if the service does not declare it.

   .. method:: static Expression tryCatchAll(Connection connection, Expression body, Expression message, Expression handler)

      A try-catch statement that handles any exception.

      :param Expression body: The statement to evaluate.
      :param Expression message: A string variable assigned the exception's message, or null.
      :param Expression handler: The statement evaluated when an exception is caught.
   

   .. method:: static Expression tryFinally(Connection connection, Expression body, Expression finalizer)

      A try-finally statement.

      :param Expression body: The statement to evaluate.
      :param Expression finalizer: The statement evaluated after the body.
   

      .. note::

         The finalizer is evaluated after the body, whether or not the body raises an
         exception. A break, continue or return cannot leave the finalizer.

   .. method:: static Expression union(Connection connection, Expression arg1, Expression arg2)

      The values in either of two collections, with duplicates removed.

      :param Expression arg1: The first collection.
      :param Expression arg2: The second collection.
      :returns: The union of the two collections.
   

      .. note::

         The result is lazily evaluated. :meth:`Expression.toList(Expression)` makes it concrete.

   .. method:: static Expression variable(Connection connection, String name, Type type)

      A local variable, declared by :meth:`Expression.blockWithVariables(java.util.List<Expression>, java.util.List<Expression>)`.

      :param String name: The name of the variable.
      :param Type type: The type of the variable.
      :returns: A local variable.
   

   .. method:: static Expression where(Connection connection, Expression arg, Expression func)

      The values of a collection for which a predicate function returns true.

      :param Expression arg: The list or set.
      :param Expression func: The predicate function, taking an element of the collection.
      :returns: The filtered collection.
   

      .. note::

         The result is lazily evaluated. :meth:`Expression.toList(Expression)` makes it concrete.

   .. method:: static Expression while_(Connection connection, Expression condition, Expression body)

      A while loop, which evaluates the body while the condition is true.

      :param Expression condition: The boolean condition.
      :param Expression body: The statement evaluated on each iteration.
   

      .. note::

         A :meth:`Expression.break_()` or :meth:`Expression.continue_()` in the body applies to this loop.

   .. method:: static Expression zip(Connection connection, Expression arg1, Expression arg2, Expression func)

      The results of a function applied to the values of two collections in pairs.

      :param Expression arg1: The first collection.
      :param Expression arg2: The second collection.
      :param Expression func: A function combining a value from each collection.
      :returns: The combined collection.
   

      .. note::

         The result is as long as the shorter of the two collections. It is lazily
         evaluated, and :meth:`Expression.toList(Expression)` makes it concrete.



.. type:: public class Type

   The type of a value a procedure takes or returns.

   .. method:: static Type bool(Connection connection)

      A boolean type.

   

   .. method:: static Type bytes(Connection connection)

      A byte sequence type.

   

   .. method:: static Type classType(Connection connection, String service, String name)

      The type of a class defined by a service.

      :param String service: The name of the service the class is defined in.
      :param String name: The name of the class.
   

   .. method:: TypeCode getCode()


      The kind of the type.

   

   .. method:: static Type dictionaryType(Connection connection, Type keyType, Type valueType)

      A dictionary type. Throws an exception if the key type is nullable or not a valid
      key type.

      :param Type keyType: The type of the keys of the dictionary.
      :param Type valueType: The type of the values of the dictionary.
   

   .. method:: static Type double_(Connection connection)

      A double precision floating point number type.

   

   .. method:: static Type enumerationType(Connection connection, String service, String name)

      The type of an enumeration defined by a service.

      :param String service: The name of the service the enumeration is defined in.
      :param String name: The name of the enumeration.
   

   .. method:: static Type float_(Connection connection)

      A single precision floating point number type.

   

   .. method:: static Type int_(Connection connection)

      A signed 32-bit integer type.

   

   .. method:: static Type listType(Connection connection, Type valueType)

      A list type.

      :param Type valueType: The type of the elements of the list.
   

   .. method:: static Type long_(Connection connection)

      A signed 64-bit integer type.

   

   .. method:: String getName()


      The name of a class, enumeration or structure type, or an empty string.

   

   .. method:: boolean getNullable()


      Whether a value of the type may be null.

   

      .. note::

         Any type can be nullable. A string or class value of a type that is not nullable
         is never null.

   .. method:: static Type nullableType(Connection connection, Type valueType)

      A nullable type, holding a value of the given type or null.

      :param Type valueType: The type of the value.
   

   .. method:: String getService()


      The service a class, enumeration or structure type is defined in, or an empty string.

   

   .. method:: static Type setType(Connection connection, Type valueType)

      A set type. Throws an exception if the element type is nullable.

      :param Type valueType: The type of the elements of the set.
   

   .. method:: static Type string(Connection connection)

      A string type.

   

   .. method:: static Type structType(Connection connection, String service, String name)

      The type of a structure defined by a service.

      :param String service: The name of the service the structure is defined in.
      :param String name: The name of the structure.
   

   .. method:: static Type tupleType(Connection connection, java.util.List<Type> valueTypes)

      A tuple type. Throws an exception if there are no element types or more than seven.

      :param java.util.List<Type> valueTypes: The types of the elements of the tuple.
   

   .. method:: java.util.List<Type> getTypes()


      The types of the elements of a tuple, list, set or dictionary type.

   

      .. note::

         The value types of a tuple, the value type of a list or set, or the key and value
         types of a dictionary. An empty list for any other type.

   .. method:: static Type uInt(Connection connection)

      An unsigned 32-bit integer type.

   

   .. method:: static Type uLong(Connection connection)

      An unsigned 64-bit integer type.

   



.. type:: public enum GameScene

   The game scene. See :meth:`KRPC.getGameScene()`.


   .. field:: public GameScene ADMINISTRATION

      The administration facility. This is a pseudo-scene, shown when the
      administration facility is open within the space center scene.


   .. field:: public GameScene ASTRONAUT_COMPLEX

      The astronaut complex. This is a pseudo-scene, shown when the
      astronaut complex facility is open within the space center scene.


   .. field:: public GameScene EDITOR_SPH

      The Space Plane Hangar.


   .. field:: public GameScene EDITOR_VAB

      The Vehicle Assembly Building.


   .. field:: public GameScene FLIGHT

      The game scene showing a vessel in flight (or on the launchpad/runway).


   .. field:: public GameScene MISSION_BUILDER

      The mission builder.


   .. field:: public GameScene MISSION_CONTROL

      Mission control. This is a pseudo-scene, shown when the
      mission control facility is open within the space center scene.


   .. field:: public GameScene RESEARCH_AND_DEVELOPMENT

      Research and development. This is a pseudo-scene, shown when the
      research and development facility is open within the space center scene.


   .. field:: public GameScene SPACE_CENTER

      The game scene showing the Kerbal Space Center buildings.


   .. field:: public GameScene TRACKING_STATION

      The tracking station.



.. type:: public enum TypeCode

   The kind of a type, with the values of the communication protocol's type codes.


   .. field:: public TypeCode BOOL

      A boolean.


   .. field:: public TypeCode BYTES

      A sequence of bytes.


   .. field:: public TypeCode CLASS

      A class defined by a service.


   .. field:: public TypeCode DICTIONARY

      A dictionary, with the types of its keys and values in :meth:`Type.getTypes()`.


   .. field:: public TypeCode DOUBLE

      A double precision floating point number.


   .. field:: public TypeCode ENUMERATION

      An enumeration defined by a service.


   .. field:: public TypeCode FLOAT

      A single precision floating point number.


   .. field:: public TypeCode LIST

      A list, with the type of its values in :meth:`Type.getTypes()`.


   .. field:: public TypeCode S_INT32

      A signed 32-bit integer.


   .. field:: public TypeCode S_INT64

      A signed 64-bit integer.


   .. field:: public TypeCode SET

      A set, with the type of its values in :meth:`Type.getTypes()`.


   .. field:: public TypeCode STRING

      A string.


   .. field:: public TypeCode STRUCT

      A structure defined by a service.


   .. field:: public TypeCode TUPLE

      A tuple, with the types of its values in :meth:`Type.getTypes()`.


   .. field:: public TypeCode U_INT32

      An unsigned 32-bit integer.


   .. field:: public TypeCode U_INT64

      An unsigned 64-bit integer.



.. type:: public class ClientInfo

   A client connected to the server.

   .. note::

      See :meth:`KRPC.getClients()`.

   .. constructor:: public ClientInfo(byte[] id, String name, String address, int protocolVersion)

      Construct a ClientInfo from the values of its fields.


   .. method:: byte[] getId()

      The identifier of the client.

   .. method:: String getName()

      The name of the client, or an empty string if it has no name.

   .. method:: String getAddress()

      The address of the client.

   .. method:: int getProtocolVersion()

      The protocol version of the client's connection.


.. type:: public class ServerSettings

   The settings that control how the server executes calls.

   .. note::

      See :meth:`ServerStatus.getSettings()`.

   .. constructor:: public ServerSettings(boolean oneRpcPerUpdate, int maxTimePerUpdate, boolean adaptiveRateControl, boolean blockingRecv, int recvTimeout, int tickHoldTimeout)

      Construct a ServerSettings from the values of its fields.


   .. method:: boolean getOneRpcPerUpdate()

      Whether the server executes one call per update.

   .. method:: int getMaxTimePerUpdate()

      The current maximum time spent executing calls per update, in microseconds.

   .. method:: boolean getAdaptiveRateControl()

      Whether the server adjusts :meth:`ServerSettings.getMaxTimePerUpdate()` to hold the frame rate.

   .. method:: boolean getBlockingRecv()

      Whether the server waits for the next call in an update.

   .. method:: int getRecvTimeout()

      The time the server waits for the next call, in microseconds.

   .. method:: int getTickHoldTimeout()

      The longest time a client may hold the game on one physics tick, in microseconds.


.. type:: public class ServerStatistics

   The performance statistics of the server.

   .. note::

      See :meth:`ServerStatus.getStatistics()`.

   .. constructor:: public ServerStatistics(long bytesRead, long bytesWritten, float bytesReadRate, float bytesWrittenRate, long rpcsExecuted, float rpcRate, float timePerRpcUpdate, float pollTimePerRpcUpdate, float execTimePerRpcUpdate, int deferredCalls, int streams, long streamEvaluations, float streamEvaluationRate, long streamUpdatesSent, float timePerStreamUpdate)

      Construct a ServerStatistics from the values of its fields.


   .. method:: long getBytesRead()

      The number of bytes received from clients.

   .. method:: long getBytesWritten()

      The number of bytes sent to clients.

   .. method:: float getBytesReadRate()

      The rate bytes are received from clients, in bytes per second.

   .. method:: float getBytesWrittenRate()

      The rate bytes are sent to clients, in bytes per second.

   .. method:: long getRpcsExecuted()

      The number of calls executed.

   .. method:: float getRpcRate()

      The number of calls executed per second.

   .. method:: float getTimePerRpcUpdate()

      The time spent receiving and executing calls per update, in seconds.

   .. method:: float getPollTimePerRpcUpdate()

      The time spent receiving calls per update, in seconds.

   .. method:: float getExecTimePerRpcUpdate()

      The time spent executing calls per update, in seconds.

   .. method:: int getDeferredCalls()

      The number of calls left for the next update, because the update ran out of time
      or the call yielded.

   .. method:: int getStreams()

      The number of open streams, events included.

   .. method:: long getStreamEvaluations()

      The number of stream evaluations, one per stream per update.

   .. method:: float getStreamEvaluationRate()

      The number of stream evaluations per second.

   .. method:: long getStreamUpdatesSent()

      The number of stream updates sent to clients.

   .. method:: float getTimePerStreamUpdate()

      The time spent updating streams per update, in seconds.


.. type:: public class ServerStatus

   The status of the server.

   .. note::

      See :meth:`KRPC.getStatus()`.

   .. constructor:: public ServerStatus(String version, int protocolVersion, ServerSettings settings, ServerStatistics statistics)

      Construct a ServerStatus from the values of its fields.


   .. method:: String getVersion()

      The version of the server, or ``unknown``.

   .. method:: int getProtocolVersion()

      The protocol version of the connection.

   .. method:: ServerSettings getSettings()

      The settings of the server.

   .. method:: ServerStatistics getStatistics()

      The performance statistics of the server.


.. type:: public class ArgumentException

   A method was invoked where at least one of the passed arguments does not
   meet the parameter specification of the method.

   The client throws :type:`java.lang.IllegalArgumentException` for this exception, and has no class for it.



.. type:: public class ArgumentNullException

   A null reference was passed to a method that does not accept it as a valid argument.

   The client throws :type:`java.lang.IllegalArgumentException` for this exception, and has no class for it.



.. type:: public class ArgumentOutOfRangeException

   The value of an argument is outside the allowable range of values as defined by the invoked method.

   The client throws :type:`java.lang.IllegalArgumentException` for this exception, and has no class for it.



.. type:: public class DivideByZeroException

   An integer was divided by zero.

   The client throws :type:`java.lang.ArithmeticException` for this exception, and has no class for it.



.. type:: public class Error

   A general error, described by its message.

   A subclass of :type:`krpc.client.ServerException`.



.. type:: public class IndexOutOfRangeException

   An index was outside the bounds of a collection, string or array.

   The client throws :type:`java.lang.IndexOutOfBoundsException` for this exception, and has no class for it.



.. type:: public class InvalidOperationException

   A method call was made to a method that is invalid
   given the current state of the object.

   The client throws :type:`java.lang.IllegalStateException` for this exception, and has no class for it.



.. type:: public class KeyNotFoundException

   A key was not found in a dictionary.

   The client throws :type:`java.util.NoSuchElementException` for this exception, and has no class for it.



.. type:: public class NotSupportedException

   A method was invoked that is not supported.

   The client throws :type:`java.lang.UnsupportedOperationException` for this exception, and has no class for it.



.. type:: public class NullReferenceException

   A value was null where its type does not allow one.

   The client throws :type:`java.lang.NullPointerException` for this exception, and has no class for it.



.. type:: public class ObjectDestroyedException

   The game object that a service object refers to no longer exists,
   so the object cannot be used. For example, the part it represents
   was destroyed, or the vessel it belongs to was recovered.

   A subclass of :type:`krpc.client.ServerException`.

   .. note::

      See :type:`ObjectNotLoadedException` for a game object that exists but is not
      loaded.

.. type:: public class ObjectNotLoadedException

   The game object that a service object refers to exists, but is not loaded.
   For example, the game is loading, or the vessel carrying the part is unloaded.

   A subclass of :type:`krpc.client.ServerException`.

   .. note::

      See :type:`ObjectDestroyedException` for a game object that no longer exists.

.. type:: public class OverflowException

   An arithmetic or conversion operation overflowed.

   The client throws :type:`java.lang.ArithmeticException` for this exception, and has no class for it.



.. type:: public class StreamRemovedException

   A stream was removed by the server, e.g. an event that has finished.

   The client throws :type:`krpc.client.StreamException` for this exception, and has no class for it.



.. type:: public class TimeoutException

   An operation did not complete in the time allotted to it.

   A subclass of :type:`krpc.client.ServerException`.



.. type:: public class UnsupportedSceneException

   The procedure is not available in the current game scene.

   A subclass of :type:`krpc.client.ServerException`.
