.. default-domain:: lua
.. highlight:: lua

.. currentmodule:: KRPC


.. module:: KRPC

Main kRPC service, used by clients to interact with basic server functionality.


.. attribute:: clients: List

   The clients that are currently connected to the server.

   :Attribute: Read-only, cannot be set




.. attribute:: game_scene: KRPC.GameScene

   The current game scene. Setting this switches the game to the given
   scene, or opens/closes the corresponding facility for the pseudo-scenes.
   Scene changes happen asynchronously: setting this property returns
   immediately, and clients should poll it until it reports the requested
   scene. Setting it to :data:`KRPC.GameScene.flight` resumes the save's
   active vessel, and fails if there is none. Moving between
   :data:`KRPC.GameScene.editor_vab` and :data:`KRPC.GameScene.editor_sph`
   keeps the vessel that is being constructed, as the game's own switch
   editor button does. Leaving flight saves the game first, unless the game
   cannot be saved, e.g. while under acceleration.

   :Attribute: Can be read or written




.. attribute:: game_state_loads: number

   The number of game states that have finished loading.

   :Attribute: Read-only, cannot be set

   .. note::

      Increases once a game load, quickload, revert or scene change has finished.




.. staticmethod:: get_client_id(): string

   Returns the identifier for the current client.





.. staticmethod:: get_client_name(): string

   Returns the name of the current client.
   This is an empty string if the client has no name.





.. staticmethod:: get_services(): krpc.schema.Services

   The services the server provides, with their procedures, classes, enumerations,
   structures and exceptions.





.. staticmethod:: get_status(): KRPC.ServerStatus

   Returns the status of the server, with its version, settings and performance statistics.





.. staticmethod:: hold_tick()

   Hold the game on its current physics tick until :meth:`KRPC.release_tick` is called
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




.. staticmethod:: next_tick()

   Let the game move on from the tick being held and hold the one after it, so that a
   program acts on consecutive physics ticks. Holds the soonest tick it can if no tick
   is currently being held.


   .. note::

      A release and a hold made as two separate calls let the game advance between them.
      This takes the next tick without that gap. Call it at the top of a loop that has to
      act on every tick, work, and come back to it.

      The game advances one tick per call, so it runs at the rate of the loop for as long
      as the loop runs. End the loop with :meth:`KRPC.release_tick`.

      Every limit of :meth:`KRPC.hold_tick` applies, the timeout included. A hold lost to
      the timeout is not reported, and the next call takes whatever tick it lands on.




.. attribute:: paused: boolean

   Whether the game is paused.

   :Attribute: Can be read or written




.. staticmethod:: release_tick()

   Let the game move on from the tick that :meth:`KRPC.hold_tick` held it on. Does
   nothing when no tick is held, which is the case once a hold has timed out.






.. class:: Expression

   A server side expression.

   .. note::

      Numeric operands of differing types are converted to a common type.

   .. staticmethod:: add(arg0: KRPC.Expression, arg1: KRPC.Expression): KRPC.Expression

      Numerical addition.

      :param arg0: The first operand.
      :param arg1: The second operand.

   .. staticmethod:: aggregate(arg: KRPC.Expression, func: KRPC.Expression): KRPC.Expression

      The result of applying an accumulator function over a collection.

      :param arg: The collection.
      :param func: The accumulator function.
      :returns: The accumulated value.

      .. note::

         Throws an exception if the collection is empty.

   .. staticmethod:: aggregate_with_seed(arg: KRPC.Expression, seed: KRPC.Expression, func: KRPC.Expression): KRPC.Expression

      The result of applying an accumulator function over a collection, starting from a seed.

      :param arg: The collection.
      :param seed: The seed value.
      :param func: The accumulator function.
      :returns: The accumulated value.

      .. note::

         The seed widens to the type of the accumulated value, the function's result.

   .. staticmethod:: all(arg: KRPC.Expression, predicate: KRPC.Expression): KRPC.Expression

      Whether a predicate function returns true for every value of a collection.

      :param arg: The collection.
      :param predicate: The predicate function.
      :returns: Whether all items satisfy the predicate.

   .. staticmethod:: and(arg0: KRPC.Expression, arg1: KRPC.Expression): KRPC.Expression

      Logical or bitwise and, which evaluates both operands.

      :param arg0: The first operand.
      :param arg1: The second operand.

      .. note::

         Also takes two values of a flags enumeration, and gives the values they share.

   .. staticmethod:: any(arg: KRPC.Expression, predicate: KRPC.Expression): KRPC.Expression

      Whether a predicate function returns true for any value of a collection.

      :param arg: The collection.
      :param predicate: The predicate function.
      :returns: Whether any item satisfies the predicate.

   .. staticmethod:: append(collection: KRPC.Expression, value: KRPC.Expression): KRPC.Expression

      A statement adding a value to a list or a set.

      :param collection: The list or set to add to.
      :param value: The value to add.

      .. note::

         The value goes on the end of a list. A set holding the value is left unchanged.

   .. staticmethod:: assign(variable: KRPC.Expression, value: KRPC.Expression): KRPC.Expression

      An assignment to a local variable, state variable or function parameter.

      :param variable: The variable to assign to.
      :param value: The value to assign.

      .. note::

         The value's type must be assignable to the variable's type. A numeric value of a
         different type is widened, and narrowing it requires a cast.

   .. staticmethod:: average(arg: KRPC.Expression): KRPC.Expression

      The average of the values of a collection.

      :param arg: The list or set.
      :returns: The average of the elements in the collection.

      .. note::

         Throws an exception if the collection is empty and its values are not nullable.

   .. staticmethod:: block(statements: List): KRPC.Expression

      A block of statements, whose value is that of its last statement.

      :param statements: The statements.

   .. staticmethod:: block_with_variables(variables: List, statements: List): KRPC.Expression

      A block of statements with local variables.

      :param variables: The local variables of the block.
      :param statements: The statements.

      .. note::

         The value of the block is that of its last statement. The variables are in scope
         for the statements of the block, including within nested functions.

   .. staticmethod:: break(): KRPC.Expression

      A break statement, which ends the enclosing loop or :meth:`KRPC.Expression.switch`.

   .. staticmethod:: build_dictionary(arg: KRPC.Expression, key_func: KRPC.Expression, value_func: KRPC.Expression): KRPC.Expression

      A dictionary built from a collection, with a key and a value computed from each
      element.

      :param arg: The list or set.
      :param key_func: The function computing an element's key.
      :param value_func: The function computing an element's value.
      :returns: The dictionary.

      .. note::

         A repeated key takes the last of its values.

   .. staticmethod:: call(call: krpc.schema.ProcedureCall): KRPC.Expression

      An RPC call.

      :param call: The procedure call.

      .. note::

         The instance the call is made on, and the values of its arguments, are fixed
         when the expression is created. A call to a procedure with no return value is
         evaluated for its effects, as a statement in a :meth:`KRPC.Expression.block`. A collection the
         procedure returns is copied, as a client would receive it.

   .. staticmethod:: call_with_arguments(call: krpc.schema.ProcedureCall, args: Map): KRPC.Expression

      An RPC call, where some or all of the arguments are computed by expressions.

      :param call: The RPC to call.
      :param args: Expressions computing the call's arguments, by position.

      .. note::

         Position 0 is the instance of a class method or property. A position with no
         expression takes the argument encoded in the call, or the parameter's default. A
         collection the procedure returns is copied, as a client would receive it.

   .. staticmethod:: cast(arg: KRPC.Expression, type: KRPC.Type): KRPC.Expression

      A value cast to the given type.

      :param arg: The value to cast.
      :param type: Type to cast the argument to.

      .. note::

         A tuple or collection is cast element by element.

   .. staticmethod:: clear(collection: KRPC.Expression): KRPC.Expression

      A statement removing every value from a list or a set, or every entry from a dictionary.

      :param collection: The list, set or dictionary.

   .. staticmethod:: concat(arg1: KRPC.Expression, arg2: KRPC.Expression): KRPC.Expression

      The values of one collection followed by those of another.

      :param arg1: The first collection.
      :param arg2: The second collection.
      :returns: The first collection followed by the second.

      .. note::

         The result is lazily evaluated. :meth:`KRPC.Expression.to_list` makes it concrete.

   .. staticmethod:: conditional(condition: KRPC.Expression, if_true: KRPC.Expression, if_false: KRPC.Expression): KRPC.Expression

      The second argument if the condition is true, and the third argument otherwise.

      :param condition: The boolean condition.
      :param if_true: The value when the condition is true.
      :param if_false: The value when the condition is false.

      .. note::

         Throws an exception if the two values have no common type.

   .. staticmethod:: conditional_and(arg0: KRPC.Expression, arg1: KRPC.Expression): KRPC.Expression

      Logical and, which evaluates the second operand only when the first is true.

      :param arg0: The first operand.
      :param arg1: The second operand.

   .. staticmethod:: conditional_or(arg0: KRPC.Expression, arg1: KRPC.Expression): KRPC.Expression

      Logical or, which evaluates the second operand only when the first is false.

      :param arg0: The first operand.
      :param arg1: The second operand.

   .. staticmethod:: constant_bool(value: boolean): KRPC.Expression

      A constant value of boolean type.

      :param value: The value.

   .. staticmethod:: constant_bytes(value: string): KRPC.Expression

      A constant value of bytes type.

      :param value: The value.

   .. staticmethod:: constant_double(value: number): KRPC.Expression

      A constant value of double precision floating point type.

      :param value: The value.

   .. staticmethod:: constant_enum(service: string, name: string, value: number): KRPC.Expression

      A constant value of an enumeration a service defines.

      :param service: The name of the service the enumeration is defined in.
      :param name: The name of the enumeration.
      :param value: The value of the member of the enumeration.

   .. staticmethod:: constant_enum_by_id(service_id: number, enumeration_id: number, value: number): KRPC.Expression

      A constant value of an enumeration a service defines, named by ids.

      :param service_id: The id of the service the enumeration is defined in.
      :param enumeration_id: The id of the enumeration.
      :param value: The value of the member of the enumeration.

   .. staticmethod:: constant_float(value: number): KRPC.Expression

      A constant value of single precision floating point type.

      :param value: The value.

   .. staticmethod:: constant_int(value: number): KRPC.Expression

      A constant value of integer type.

      :param value: The value.

   .. staticmethod:: constant_long(value: number): KRPC.Expression

      A constant value of long integer type.

      :param value: The value.

   .. staticmethod:: constant_null(type: KRPC.Type): KRPC.Expression

      A null constant of the given type.

      :param type: The type of the value.

      .. note::

         The type is made nullable, as by :meth:`KRPC.Type.nullable_type`.

   .. staticmethod:: constant_object(value: number): KRPC.Expression

      A constant object, an instance of a class a service defines.

      :param value: The object identifier of the object.

      .. note::

         The object is given by its object identifier.

   .. staticmethod:: constant_string(value: string): KRPC.Expression

      A constant value of string type.

      :param value: The value.

   .. staticmethod:: constant_u_int(value: number): KRPC.Expression

      A constant value of unsigned integer type.

      :param value: The value.

   .. staticmethod:: constant_u_long(value: number): KRPC.Expression

      A constant value of unsigned long integer type.

      :param value: The value.

   .. staticmethod:: contains(arg: KRPC.Expression, value: KRPC.Expression): KRPC.Expression

      Whether a collection contains a value.

      :param arg: The collection.
      :param value: The value to look for.
      :returns: Whether the collection contains the value.

   .. staticmethod:: contains_key(dictionary: KRPC.Expression, key: KRPC.Expression): KRPC.Expression

      Whether a dictionary contains a key.

      :param dictionary: The dictionary.
      :param key: The key to look for.
      :returns: Whether the dictionary contains the key.

   .. staticmethod:: continue(): KRPC.Expression

      A continue statement, which skips to the next iteration of the enclosing loop.

   .. staticmethod:: convert_to_string(arg: KRPC.Expression): KRPC.Expression

      The string representation of a value.

      :param arg: The value to convert.
      :returns: The string representation of the value.

      .. note::

         A value other than a number, boolean, string or enumeration converts as .NET
         formats it, which for a collection is the name of its type. Throws an exception if
         the value is null.

   .. staticmethod:: count(arg: KRPC.Expression): KRPC.Expression

      The number of values in a collection.

      :param arg: The list, set or dictionary.
      :returns: The number of elements in the collection.

   .. staticmethod:: create_dictionary(keys: List, values: List): KRPC.Expression

      A dictionary of the given keys and corresponding values, with numbers widened to
      their common type. Throws an exception if there are no keys, the counts differ, the
      keys are not of a valid key type, or a key is null.

      :param keys: The keys.
      :param values: The values.
      :returns: The dictionary.

      .. note::

         A repeated key takes the last of its values.

   .. staticmethod:: create_empty_dictionary(key_type: KRPC.Type, value_type: KRPC.Type): KRPC.Expression

      An empty dictionary of the given key and value types. Throws an exception if the
      key type is nullable or not a valid key type.

      :param key_type: The type of the dictionary's keys.
      :param value_type: The type of the dictionary's values.
      :returns: The empty dictionary.

   .. staticmethod:: create_empty_list(value_type: KRPC.Type): KRPC.Expression

      An empty list of the given value type.

      :param value_type: The type of the values the list holds.
      :returns: The empty list.

   .. staticmethod:: create_empty_set(value_type: KRPC.Type): KRPC.Expression

      An empty set of the given value type. Throws an exception if the value type is
      nullable.

      :param value_type: The type of the values the set holds.
      :returns: The empty set.

   .. staticmethod:: create_list(values: List): KRPC.Expression

      A list of the given values, with numbers widened to their common type. Throws an
      exception if there are no values.

      :param values: The values.
      :returns: The list.

   .. staticmethod:: create_set(values: Set): KRPC.Expression

      A set of the given values, with numbers widened to their common type. Throws an
      exception if there are no values, or a value is null.

      :param values: The values.
      :returns: The set.

      .. note::

         The values are evaluated in an unspecified order.

   .. staticmethod:: create_struct(type: KRPC.Type, field_values: List): KRPC.Expression

      A structure with the given field values. Throws an exception if the field values do
      not match its fields.

      :param type: The type of the structure.
      :param field_values: The values of the structure's fields, in the order the structure declares them.
      :returns: The structure.

   .. staticmethod:: create_tuple(elements: List): KRPC.Expression

      A tuple of the given elements. Throws an exception if there are no elements or more
      than seven.

      :param elements: The elements.
      :returns: The tuple.

   .. staticmethod:: dictionary_keys(dictionary: KRPC.Expression): KRPC.Expression

      The keys of a dictionary, as a list.

      :param dictionary: The dictionary.
      :returns: The keys of the dictionary.

      .. note::

         The collection operations take the keys of a dictionary from here, and its values
         from :meth:`KRPC.Expression.dictionary_values`.

   .. staticmethod:: dictionary_values(dictionary: KRPC.Expression): KRPC.Expression

      The values of a dictionary, as a list.

      :param dictionary: The dictionary.
      :returns: The values of the dictionary.

   .. staticmethod:: distinct(arg: KRPC.Expression): KRPC.Expression

      The values of a collection with duplicates removed, keeping the first
      occurrence of each.

      :param arg: The collection.
      :returns: The distinct values of the collection.

      .. note::

         The result is lazily evaluated. :meth:`KRPC.Expression.to_list` makes it concrete.

   .. staticmethod:: divide(arg0: KRPC.Expression, arg1: KRPC.Expression): KRPC.Expression

      Numerical division.

      :param arg0: The first operand.
      :param arg1: The second operand.

   .. staticmethod:: element_at(arg: KRPC.Expression, index: KRPC.Expression): KRPC.Expression

      The value at the given position in a collection.

      :param arg: The collection.
      :param index: The position of the value, counting from zero.
      :returns: The value at the given position.

      .. note::

         Throws an exception if the index is out of range.

   .. staticmethod:: equal(arg0: KRPC.Expression, arg1: KRPC.Expression): KRPC.Expression

      Equality comparison.

      :param arg0: The first operand.
      :param arg1: The second operand.

      .. note::

         Collections, tuples, bytes and structures compare by their contents. Throws an
         exception if the operands are of unrelated types.

   .. staticmethod:: except(arg1: KRPC.Expression, arg2: KRPC.Expression): KRPC.Expression

      The values of the first collection that are not in the second, with
      duplicates removed.

      :param arg1: The first collection.
      :param arg2: The second collection.
      :returns: The difference between the two collections.

      .. note::

         The result is lazily evaluated. :meth:`KRPC.Expression.to_list` makes it concrete.

   .. staticmethod:: exclusive_or(arg0: KRPC.Expression, arg1: KRPC.Expression): KRPC.Expression

      Logical or bitwise exclusive or.

      :param arg0: The first operand.
      :param arg1: The second operand.

      .. note::

         Also takes two values of a flags enumeration, and gives the values in only one of them.

   .. staticmethod:: first(arg: KRPC.Expression): KRPC.Expression

      The first value of a collection.

      :param arg: The collection.
      :returns: The first value.

      .. note::

         Throws an exception if the collection is empty.

   .. staticmethod:: for_each(variable: KRPC.Expression, collection: KRPC.Expression, body: KRPC.Expression): KRPC.Expression

      A loop that evaluates the body once for each value of a collection.

      :param variable: The loop variable.
      :param collection: The collection to iterate over.
      :param body: The statement evaluated on each iteration.

      .. note::

         The loop variable is a local variable declared by an enclosing
         :meth:`KRPC.Expression.block_with_variables`, or a parameter of an enclosing :meth:`KRPC.Expression.lambda`.
         A :meth:`KRPC.Expression.break` or :meth:`KRPC.Expression.continue` in the body applies to this loop.

   .. staticmethod:: get(arg: KRPC.Expression, index: KRPC.Expression): KRPC.Expression

      An element of a tuple, list or dictionary.

      :param arg: The tuple, list or dictionary.
      :param index: The index or key of the element.
      :returns: The element.

      .. note::

         A tuple takes a constant integer index, a list a zero indexed integer and a
         dictionary a key. Throws an exception if the index is out of range or the key is
         missing, and for a tuple when the expression is created.

   .. staticmethod:: get_field(arg: KRPC.Expression, name: string): KRPC.Expression

      The value of a field of a structure. Throws an exception if the value is not a
      structure, or has no field of the given name.

      :param arg: The structure.
      :param name: The name of the field to access.
      :returns: The value of the field.

   .. staticmethod:: get_field_by_index(arg: KRPC.Expression, index: number): KRPC.Expression

      The value of a field of a structure, named by its position. Throws an exception if
      the value is not a structure, or the index is out of range.

      :param arg: The structure.
      :param index: The position of the field to access.
      :returns: The value of the field.

      .. note::

         The fields are numbered from 0, in the order the structure defines them.

   .. staticmethod:: greater_than(arg0: KRPC.Expression, arg1: KRPC.Expression): KRPC.Expression

      Greater than numerical comparison.

      :param arg0: The first operand.
      :param arg1: The second operand.

   .. staticmethod:: greater_than_or_equal(arg0: KRPC.Expression, arg1: KRPC.Expression): KRPC.Expression

      Greater than or equal numerical comparison.

      :param arg0: The first operand.
      :param arg1: The second operand.

   .. staticmethod:: group_by(arg: KRPC.Expression, key: KRPC.Expression): KRPC.Expression

      The values of a collection grouped by the key a function produces for each of them.

      :param arg: The collection.
      :param key: A function producing the key to group values by.
      :returns: A dictionary of each key to the values that produced it.

   .. attribute:: has_return_type: boolean

      Whether the expression evaluates to a value.

      :Attribute: Read-only, cannot be set

      .. note::

         An expression with no value has no :attr:`KRPC.Expression.return_type`. An empty
         collection and no value both encode to an empty sequence of bytes.

   .. staticmethod:: if_then(condition: KRPC.Expression, body: KRPC.Expression): KRPC.Expression

      An if statement, with no value.

      :param condition: The boolean condition.
      :param body: The statement evaluated when the condition is true.

      .. note::

         Use :meth:`KRPC.Expression.conditional` for an if-then-else that produces a value.

   .. staticmethod:: if_then_else(condition: KRPC.Expression, body: KRPC.Expression, else_body: KRPC.Expression): KRPC.Expression

      An if-else statement, with no value.

      :param condition: The boolean condition.
      :param body: The statement evaluated when the condition is true.
      :param else_body: The statement evaluated when the condition is false.

      .. note::

         Use :meth:`KRPC.Expression.conditional` for an if-then-else that produces a value.

   .. staticmethod:: intersect(arg1: KRPC.Expression, arg2: KRPC.Expression): KRPC.Expression

      The values in both of two collections, with duplicates removed.

      :param arg1: The first collection.
      :param arg2: The second collection.
      :returns: The intersection of the two collections.

      .. note::

         The result is lazily evaluated. :meth:`KRPC.Expression.to_list` makes it concrete.

   .. staticmethod:: invoke(function: KRPC.Expression, args: Map): KRPC.Expression

      A function call.

      :param function: The function to call.
      :param args: The arguments to call the function with.
      :returns: A function call.

   .. staticmethod:: is_null(value: KRPC.Expression): KRPC.Expression

      Whether a value is null. Throws an exception if the type of the value cannot be null.

      :param value: The value.

   .. staticmethod:: lambda(parameters: List, body: KRPC.Expression): KRPC.Expression

      A function with the given parameters and body.

      :param parameters: The parameters of the function.
      :param body: The body of the function.
      :returns: A function.

      .. note::

         The body is an expression or a block of statements, and a body with no value is
         evaluated for its effects. A :meth:`KRPC.Expression.return` or :meth:`KRPC.Expression.return_nothing`
         statement ends the evaluation.

   .. staticmethod:: last(arg: KRPC.Expression): KRPC.Expression

      The last value of a collection.

      :param arg: The collection.
      :returns: The last value.

      .. note::

         Throws an exception if the collection is empty.

   .. staticmethod:: left_shift(arg0: KRPC.Expression, arg1: KRPC.Expression): KRPC.Expression

      Bitwise left shift.

      :param arg0: The first operand.
      :param arg1: The second operand.

      .. note::

         Only the low 5 bits of the count are used for a 32-bit value, and the low 6 bits
         for a 64-bit value.

   .. staticmethod:: less_than(arg0: KRPC.Expression, arg1: KRPC.Expression): KRPC.Expression

      Less than numerical comparison.

      :param arg0: The first operand.
      :param arg1: The second operand.

   .. staticmethod:: less_than_or_equal(arg0: KRPC.Expression, arg1: KRPC.Expression): KRPC.Expression

      Less than or equal numerical comparison.

      :param arg0: The first operand.
      :param arg1: The second operand.

   .. staticmethod:: match(value: KRPC.Expression, cases: List, bodies: List): KRPC.Expression

      A match statement, which evaluates the first matching case.

      :param value: The value to match.
      :param cases: The test values of each case.
      :param bodies: The statement of each case.

      .. note::

         Test values compare as for :meth:`KRPC.Expression.equal`, and a case with none is the default.
         A :meth:`KRPC.Expression.break` in a body ends the enclosing loop.

   .. staticmethod:: max(arg: KRPC.Expression): KRPC.Expression

      The largest value of a collection.

      :param arg: The list or set.
      :returns: The largest value in the collection.

      .. note::

         Throws an exception if the collection is empty and its values are not nullable.

   .. staticmethod:: max_by(arg: KRPC.Expression, key: KRPC.Expression): KRPC.Expression

      The value of a collection with the largest key.

      :param arg: The collection.
      :param key: A function producing the key to compare values by.
      :returns: The value with the largest key.

      .. note::

         Throws an exception if the collection is empty.

   .. staticmethod:: min(arg: KRPC.Expression): KRPC.Expression

      The smallest value of a collection.

      :param arg: The list or set.
      :returns: The smallest value in the collection.

      .. note::

         Throws an exception if the collection is empty and its values are not nullable.

   .. staticmethod:: min_by(arg: KRPC.Expression, key: KRPC.Expression): KRPC.Expression

      The value of a collection with the smallest key.

      :param arg: The collection.
      :param key: A function producing the key to compare values by.
      :returns: The value with the smallest key.

      .. note::

         Throws an exception if the collection is empty.

   .. staticmethod:: modulo(arg0: KRPC.Expression, arg1: KRPC.Expression): KRPC.Expression

      Numerical modulo operator.

      :param arg0: The first operand.
      :param arg1: The second operand.
      :returns: The remainder of arg0 divided by arg1

   .. staticmethod:: multiply(arg0: KRPC.Expression, arg1: KRPC.Expression): KRPC.Expression

      Numerical multiplication.

      :param arg0: The first operand.
      :param arg1: The second operand.

   .. staticmethod:: negate(arg: KRPC.Expression): KRPC.Expression

      Numerical negation, which negates a uint as a long.

      :param arg: The operand.

   .. staticmethod:: not(arg: KRPC.Expression): KRPC.Expression

      Logical or bitwise negation.

      :param arg: The operand.

      .. note::

         Also takes a value of a flags enumeration, and gives the declared values it does not hold.

   .. staticmethod:: not_equal(arg0: KRPC.Expression, arg1: KRPC.Expression): KRPC.Expression

      Inequality comparison.

      :param arg0: The first operand.
      :param arg1: The second operand.

      .. note::

         Collections, tuples, bytes and structures compare by their contents. Throws an
         exception if the operands are of unrelated types.

   .. staticmethod:: or(arg0: KRPC.Expression, arg1: KRPC.Expression): KRPC.Expression

      Logical or bitwise or, which evaluates both operands.

      :param arg0: The first operand.
      :param arg1: The second operand.

      .. note::

         Also takes two values of a flags enumeration, and gives their combination.

   .. staticmethod:: order_by(arg: KRPC.Expression, key: KRPC.Expression): KRPC.Expression

      The values of a collection ordered by the key a function produces for each of them.

      :param arg: The collection to order.
      :param key: A function producing the key to order values by.
      :returns: The ordered collection.

      .. note::

         The result is lazily evaluated. :meth:`KRPC.Expression.to_list` makes it concrete.

   .. staticmethod:: parameter(name: string, type: KRPC.Type): KRPC.Expression

      A named parameter of a function.

      :param name: The name of the parameter.
      :param type: The type of the parameter.
      :returns: A named parameter.

      .. note::

         A value is bound to it by name with :meth:`KRPC.Expression.invoke`, or by its position in
         the parameters of a :meth:`KRPC.Expression.lambda`.

   .. attribute:: parameter_types: List

      The types of the function's parameters.

      :Attribute: Read-only, cannot be set

      .. note::

         Empty for a function that is not created with :meth:`KRPC.Expression.lambda`.

   .. staticmethod:: power(arg0: KRPC.Expression, arg1: KRPC.Expression): KRPC.Expression

      Numerical power operator.

      :param arg0: The first operand.
      :param arg1: The second operand.
      :returns: arg0 raised to the power of arg1, at the common type of the operands

   .. staticmethod:: remove(collection: KRPC.Expression, value: KRPC.Expression): KRPC.Expression

      A removal of a value from a list or a set, or of an entry from a dictionary.

      :param collection: The list, set or dictionary.
      :param value: The value to remove, or the key of the entry to remove.
      :returns: Whether the collection held the value.

      .. note::

         The first occurrence is removed from a list.

   .. staticmethod:: remove_at(list: KRPC.Expression, index: KRPC.Expression): KRPC.Expression

      A statement removing the element at an index of a list.

      :param list: The list.
      :param index: The index of the element, counting from zero.

      .. note::

         Throws an exception if the index is out of range.

   .. staticmethod:: return(value: KRPC.Expression): KRPC.Expression

      A return statement, which ends the enclosing function with a value.

      :param value: The value to return.

   .. staticmethod:: return_nothing(): KRPC.Expression

      A return statement, which ends the enclosing function with no value.

   .. attribute:: return_type: KRPC.Type

      The type of the value the expression evaluates to. Throws an exception if the
      value cannot be sent to a client.

      :Attribute: Read-only, cannot be set

      .. note::

         A function created with :meth:`KRPC.Expression.lambda` evaluates to the value it returns. A
         lazily evaluated collection, such as from :meth:`KRPC.Expression.select`, is sent once
         converted by :meth:`KRPC.Expression.to_list` or :meth:`KRPC.Expression.to_set`.

   .. staticmethod:: reverse(arg: KRPC.Expression): KRPC.Expression

      The values of a collection in the opposite order.

      :param arg: The collection.
      :returns: The reversed collection.

      .. note::

         The result is lazily evaluated. :meth:`KRPC.Expression.to_list` makes it concrete.

   .. staticmethod:: right_shift(arg0: KRPC.Expression, arg1: KRPC.Expression): KRPC.Expression

      Bitwise right shift.

      :param arg0: The first operand.
      :param arg1: The second operand.

      .. note::

         Only the low 5 bits of the count are used for a 32-bit value, and the low 6 bits
         for a 64-bit value.

   .. staticmethod:: select(arg: KRPC.Expression, func: KRPC.Expression): KRPC.Expression

      The result of a function for each value of a collection.

      :param arg: The list or set.
      :param func: The function, taking an element of the collection.
      :returns: The collection of function results.

      .. note::

         The result is lazily evaluated. :meth:`KRPC.Expression.to_list` makes it concrete.

   .. staticmethod:: select_many(arg: KRPC.Expression, func: KRPC.Expression): KRPC.Expression

      The values of the collections a function returns for each value of a collection,
      flattened into one.

      :param arg: The list or set.
      :param func: The function, taking an element of the collection and returning a collection.
      :returns: The flattened collection of function results.

      .. note::

         The result is lazily evaluated. :meth:`KRPC.Expression.to_list` makes it concrete.

   .. staticmethod:: set(collection: KRPC.Expression, index: KRPC.Expression, value: KRPC.Expression): KRPC.Expression

      A statement setting an element of a list, or an entry of a dictionary.

      :param collection: The list or dictionary.
      :param index: The index of the element, counting from zero, or the key of the entry.
      :param value: The value to set.

      .. note::

         A key the dictionary does not hold is added. Throws an exception if the index
         is out of range for the list.

   .. staticmethod:: skip(arg: KRPC.Expression, count: KRPC.Expression): KRPC.Expression

      A collection without its first count values.

      :param arg: The collection.
      :param count: The number of values to skip.
      :returns: The collection without its first count values.

      .. note::

         The result is lazily evaluated. :meth:`KRPC.Expression.to_list` makes it concrete.

   .. staticmethod:: state_variable(name: string, initial_value: KRPC.Expression): KRPC.Expression

      A variable that keeps its value between evaluations of a function.

      :param name: The name of the variable.
      :param initial_value: The initial value of the variable.
      :returns: A state variable.

      .. note::

         The initial value is evaluated once, when the variable is created, and gives the
         variable its type. The value belongs to the variable, so every function that
         contains it shares it.

   .. staticmethod:: string_concat(args: List): KRPC.Expression

      The concatenation of strings.

      :param args: The strings to concatenate, in order.
      :returns: The concatenated string.

      .. note::

         Use :meth:`KRPC.Expression.convert_to_string` to convert other values to strings.

   .. staticmethod:: string_contains(arg: KRPC.Expression, value: KRPC.Expression): KRPC.Expression

      Whether one string occurs within another.

      :param arg: The string to search.
      :param value: The string to search for.
      :returns: Whether the value occurs in the string.

   .. staticmethod:: string_ends_with(arg: KRPC.Expression, value: KRPC.Expression): KRPC.Expression

      Whether a string ends with another.

      :param arg: The string to test.
      :param value: The string to test for.
      :returns: Whether the string ends with the value.

   .. staticmethod:: string_get(arg: KRPC.Expression, index: KRPC.Expression): KRPC.Expression

      The character at the given position in a string, as a string of length one.

      :param arg: The string.
      :param index: The position of the character, counting from zero.
      :returns: The character at the given position.

      .. note::

         Throws an exception if the index is out of range.

   .. staticmethod:: string_index_of(arg: KRPC.Expression, value: KRPC.Expression): KRPC.Expression

      The position of the first occurrence of one string within another.

      :param arg: The string to search.
      :param value: The string to search for.
      :returns: The position of the value, counting from zero, or -1 if it does not occur.

   .. staticmethod:: string_join(separator: KRPC.Expression, values: KRPC.Expression): KRPC.Expression

      Strings joined together, separated by another string.

      :param separator: The string to put between the values.
      :param values: The strings to join.
      :returns: The joined string.

   .. staticmethod:: string_length(arg: KRPC.Expression): KRPC.Expression

      The number of characters in a string.

      :param arg: The string.
      :returns: The length of the string.

   .. staticmethod:: string_replace(arg: KRPC.Expression, old_value: KRPC.Expression, new_value: KRPC.Expression): KRPC.Expression

      A string with every occurrence of one string replaced by another.

      :param arg: The string to search.
      :param old_value: The string to replace.
      :param new_value: The string to replace it with.
      :returns: The string with the replacements made.

      .. note::

         Throws an exception if the string to replace is empty.

   .. staticmethod:: string_split(arg: KRPC.Expression, separator: KRPC.Expression): KRPC.Expression

      The parts of a string separated by another string.

      :param arg: The string to split.
      :param separator: The string that separates the parts.
      :returns: The parts of the string.

   .. staticmethod:: string_starts_with(arg: KRPC.Expression, value: KRPC.Expression): KRPC.Expression

      Whether a string starts with another.

      :param arg: The string to test.
      :param value: The string to test for.
      :returns: Whether the string starts with the value.

   .. staticmethod:: string_substring(arg: KRPC.Expression, start: KRPC.Expression, length: KRPC.Expression): KRPC.Expression

      Part of a string.

      :param arg: The string.
      :param start: The position to start at, counting from zero.
      :param length: The number of characters to take.
      :returns: The substring.

      .. note::

         Throws an exception if the start or the length is out of range.

   .. staticmethod:: string_to_lower(arg: KRPC.Expression): KRPC.Expression

      A string in lower case, independently of the game's language.

      :param arg: The string.
      :returns: The string in lower case.

   .. staticmethod:: string_to_upper(arg: KRPC.Expression): KRPC.Expression

      A string in upper case, independently of the game's language.

      :param arg: The string.
      :returns: The string in upper case.

   .. staticmethod:: string_trim(arg: KRPC.Expression): KRPC.Expression

      A string with white space removed from both ends.

      :param arg: The string.
      :returns: The trimmed string.

   .. staticmethod:: string_trim_end(arg: KRPC.Expression): KRPC.Expression

      A string with white space removed from its end.

      :param arg: The string.
      :returns: The trimmed string.

   .. staticmethod:: string_trim_start(arg: KRPC.Expression): KRPC.Expression

      A string with white space removed from its start.

      :param arg: The string.
      :returns: The trimmed string.

   .. staticmethod:: subtract(arg0: KRPC.Expression, arg1: KRPC.Expression): KRPC.Expression

      Numerical subtraction.

      :param arg0: The first operand.
      :param arg1: The second operand.

   .. staticmethod:: sum(arg: KRPC.Expression): KRPC.Expression

      The sum of the values of a collection.

      :param arg: The list or set.
      :returns: The sum of the elements in the collection.

   .. staticmethod:: switch(value: KRPC.Expression, cases: List, bodies: List): KRPC.Expression

      A switch statement, which evaluates from the first matching case on.

      :param value: The value to match.
      :param cases: The test values of each case.
      :param bodies: The statement of each case.

      .. note::

         Test values compare as for :meth:`KRPC.Expression.equal`, and a case with none is the default.
         A :meth:`KRPC.Expression.break` in a body ends the switch.

   .. staticmethod:: take(arg: KRPC.Expression, count: KRPC.Expression): KRPC.Expression

      The first count values of a collection.

      :param arg: The collection.
      :param count: The number of values to take.
      :returns: The first count values of the collection.

      .. note::

         The result is lazily evaluated. :meth:`KRPC.Expression.to_list` makes it concrete.

   .. staticmethod:: throw(service: string, name: string, message: KRPC.Expression): KRPC.Expression

      A throw statement, which raises the named exception.

      :param service: The name of the service the exception is defined in.
      :param name: The name of the exception.
      :param message: The message of the exception.

      .. note::

         The client receives the exception as an error from the function, or from the stream
         or event evaluating it. For example, the service ``KRPC`` and the name
         ``Error`` raise :class:`KRPC.Error`. Throws an exception if the service does not
         declare it.

   .. staticmethod:: throw_by_id(service_id: number, exception_id: number, message: KRPC.Expression): KRPC.Expression

      A throw statement, which raises the exception with the given ids.

      :param service_id: The id of the service the exception is defined in.
      :param exception_id: The id of the exception.
      :param message: The message of the exception.

      .. note::

         Raises the exception as :meth:`KRPC.Expression.throw` does. Throws an exception if the
         service does not declare it.

   .. staticmethod:: to_list(arg: KRPC.Expression): KRPC.Expression

      A collection as a list.

      :param arg: The collection.
      :returns: The collection as a list.

   .. staticmethod:: to_set(arg: KRPC.Expression): KRPC.Expression

      A collection as a set.

      :param arg: The collection.
      :returns: The collection as a set.

      .. note::

         Throws an exception if a value is null.

   .. staticmethod:: try_catch(body: KRPC.Expression, service: string, name: string, message: KRPC.Expression | Types.none, handler: KRPC.Expression): KRPC.Expression

      A try-catch statement that handles the named exception.

      :param body: The statement to evaluate.
      :param service: The name of the service the exception is defined in.
      :param name: The name of the exception.
      :param message: A string variable assigned the exception's message, or null.
      :param handler: The statement evaluated when the exception is caught.

      .. note::

         Only an exception a service declares can be named, and :meth:`KRPC.Expression.try_catch_all`
         handles any exception. Throws an exception if the service does not declare it.

   .. staticmethod:: try_catch_all(body: KRPC.Expression, message: KRPC.Expression | Types.none, handler: KRPC.Expression): KRPC.Expression

      A try-catch statement that handles any exception.

      :param body: The statement to evaluate.
      :param message: A string variable assigned the exception's message, or null.
      :param handler: The statement evaluated when an exception is caught.

   .. staticmethod:: try_catch_by_id(body: KRPC.Expression, service_id: number, exception_id: number, message: KRPC.Expression | Types.none, handler: KRPC.Expression): KRPC.Expression

      A try-catch statement that handles the exception with the given ids.

      :param body: The statement to evaluate.
      :param service_id: The id of the service the exception is defined in.
      :param exception_id: The id of the exception.
      :param message: A string variable assigned the exception's message, or null.
      :param handler: The statement evaluated when the exception is caught.

      .. note::

         Handles the exception as :meth:`KRPC.Expression.try_catch` does. Throws an exception if the
         service does not declare it.

   .. staticmethod:: try_finally(body: KRPC.Expression, finalizer: KRPC.Expression): KRPC.Expression

      A try-finally statement.

      :param body: The statement to evaluate.
      :param finalizer: The statement evaluated after the body.

      .. note::

         The finalizer is evaluated after the body, whether or not the body raises an
         exception. A break, continue or return cannot leave the finalizer.

   .. staticmethod:: union(arg1: KRPC.Expression, arg2: KRPC.Expression): KRPC.Expression

      The values in either of two collections, with duplicates removed.

      :param arg1: The first collection.
      :param arg2: The second collection.
      :returns: The union of the two collections.

      .. note::

         The result is lazily evaluated. :meth:`KRPC.Expression.to_list` makes it concrete.

   .. staticmethod:: variable(name: string, type: KRPC.Type): KRPC.Expression

      A local variable, declared by :meth:`KRPC.Expression.block_with_variables`.

      :param name: The name of the variable.
      :param type: The type of the variable.
      :returns: A local variable.

   .. staticmethod:: where(arg: KRPC.Expression, func: KRPC.Expression): KRPC.Expression

      The values of a collection for which a predicate function returns true.

      :param arg: The list or set.
      :param func: The predicate function, taking an element of the collection.
      :returns: The filtered collection.

      .. note::

         The result is lazily evaluated. :meth:`KRPC.Expression.to_list` makes it concrete.

   .. staticmethod:: while(condition: KRPC.Expression, body: KRPC.Expression): KRPC.Expression

      A while loop, which evaluates the body while the condition is true.

      :param condition: The boolean condition.
      :param body: The statement evaluated on each iteration.

      .. note::

         A :meth:`KRPC.Expression.break` or :meth:`KRPC.Expression.continue` in the body applies to this loop.

   .. staticmethod:: zip(arg1: KRPC.Expression, arg2: KRPC.Expression, func: KRPC.Expression): KRPC.Expression

      The results of a function applied to the values of two collections in pairs.

      :param arg1: The first collection.
      :param arg2: The second collection.
      :param func: A function combining a value from each collection.
      :returns: The combined collection.

      .. note::

         The result is as long as the shorter of the two collections. It is lazily
         evaluated, and :meth:`KRPC.Expression.to_list` makes it concrete.



.. class:: Type

   The type of a value a procedure takes or returns.

   .. staticmethod:: bool(): KRPC.Type

      A boolean type.

   .. staticmethod:: bytes(): KRPC.Type

      A byte sequence type.

   .. staticmethod:: class_type(service: string, name: string): KRPC.Type

      The type of a class defined by a service.

      :param service: The name of the service the class is defined in.
      :param name: The name of the class.

   .. staticmethod:: class_type_by_id(service_id: number, class_id: number): KRPC.Type

      The type of a class defined by a service, named by ids.

      :param service_id: The id of the service the class is defined in.
      :param class_id: The id of the class.

   .. attribute:: code: KRPC.TypeCode

      The kind of the type.

      :Attribute: Read-only, cannot be set

   .. staticmethod:: dictionary_type(key_type: KRPC.Type, value_type: KRPC.Type): KRPC.Type

      A dictionary type. Throws an exception if the key type is nullable or not a valid
      key type.

      :param key_type: The type of the keys of the dictionary.
      :param value_type: The type of the values of the dictionary.

   .. staticmethod:: double(): KRPC.Type

      A double precision floating point number type.

   .. staticmethod:: enumeration_type(service: string, name: string): KRPC.Type

      The type of an enumeration defined by a service.

      :param service: The name of the service the enumeration is defined in.
      :param name: The name of the enumeration.

   .. staticmethod:: enumeration_type_by_id(service_id: number, enumeration_id: number): KRPC.Type

      The type of an enumeration defined by a service, named by ids.

      :param service_id: The id of the service the enumeration is defined in.
      :param enumeration_id: The id of the enumeration.

   .. staticmethod:: float(): KRPC.Type

      A single precision floating point number type.

   .. staticmethod:: int(): KRPC.Type

      A signed 32-bit integer type.

   .. staticmethod:: list_type(value_type: KRPC.Type): KRPC.Type

      A list type.

      :param value_type: The type of the elements of the list.

   .. staticmethod:: long(): KRPC.Type

      A signed 64-bit integer type.

   .. attribute:: name: string

      The name of a class, enumeration or structure type, or an empty string.

      :Attribute: Read-only, cannot be set

   .. attribute:: nullable: boolean

      Whether a value of the type may be null.

      :Attribute: Read-only, cannot be set

      .. note::

         Any type can be nullable. A string or class value of a type that is not nullable
         is never null.

   .. staticmethod:: nullable_type(value_type: KRPC.Type): KRPC.Type

      A nullable type, holding a value of the given type or null.

      :param value_type: The type of the value.

   .. attribute:: service: string

      The service a class, enumeration or structure type is defined in, or an empty string.

      :Attribute: Read-only, cannot be set

   .. staticmethod:: set_type(value_type: KRPC.Type): KRPC.Type

      A set type. Throws an exception if the element type is nullable.

      :param value_type: The type of the elements of the set.

   .. staticmethod:: string(): KRPC.Type

      A string type.

   .. staticmethod:: struct_type(service: string, name: string): KRPC.Type

      The type of a structure defined by a service.

      :param service: The name of the service the structure is defined in.
      :param name: The name of the structure.

   .. staticmethod:: struct_type_by_id(service_id: number, struct_id: number): KRPC.Type

      The type of a structure defined by a service, named by ids.

      :param service_id: The id of the service the structure is defined in.
      :param struct_id: The id of the structure.

   .. staticmethod:: tuple_type(value_types: List): KRPC.Type

      A tuple type. Throws an exception if there are no element types or more than seven.

      :param value_types: The types of the elements of the tuple.

   .. attribute:: types: List

      The types of the elements of a tuple, list, set or dictionary type.

      :Attribute: Read-only, cannot be set

      .. note::

         The value types of a tuple, the value type of a list or set, or the key and value
         types of a dictionary. An empty list for any other type.

   .. staticmethod:: u_int(): KRPC.Type

      An unsigned 32-bit integer type.

   .. staticmethod:: u_long(): KRPC.Type

      An unsigned 64-bit integer type.



.. class:: GameScene

   The game scene. See :attr:`KRPC.game_scene`.


   .. data:: administration

      The administration facility. This is a pseudo-scene, shown when the
      administration facility is open within the space center scene.


   .. data:: astronaut_complex

      The astronaut complex. This is a pseudo-scene, shown when the
      astronaut complex facility is open within the space center scene.


   .. data:: editor_sph

      The Space Plane Hangar.


   .. data:: editor_vab

      The Vehicle Assembly Building.


   .. data:: flight

      The game scene showing a vessel in flight (or on the launchpad/runway).


   .. data:: mission_builder

      The mission builder.


   .. data:: mission_control

      Mission control. This is a pseudo-scene, shown when the
      mission control facility is open within the space center scene.


   .. data:: research_and_development

      Research and development. This is a pseudo-scene, shown when the
      research and development facility is open within the space center scene.


   .. data:: space_center

      The game scene showing the Kerbal Space Center buildings.


   .. data:: tracking_station

      The tracking station.



.. class:: TypeCode

   The kind of a type, with the values of the communication protocol's type codes.


   .. data:: bool

      A boolean.


   .. data:: bytes

      A sequence of bytes.


   .. data:: class

      A class defined by a service.


   .. data:: dictionary

      A dictionary, with the types of its keys and values in :attr:`KRPC.Type.types`.


   .. data:: double

      A double precision floating point number.


   .. data:: enumeration

      An enumeration defined by a service.


   .. data:: float

      A single precision floating point number.


   .. data:: list

      A list, with the type of its values in :attr:`KRPC.Type.types`.


   .. data:: s_int32

      A signed 32-bit integer.


   .. data:: s_int64

      A signed 64-bit integer.


   .. data:: set

      A set, with the type of its values in :attr:`KRPC.Type.types`.


   .. data:: string

      A string.


   .. data:: struct

      A structure defined by a service.


   .. data:: tuple

      A tuple, with the types of its values in :attr:`KRPC.Type.types`.


   .. data:: u_int32

      An unsigned 32-bit integer.


   .. data:: u_int64

      An unsigned 64-bit integer.



.. class:: ClientInfo

   A client connected to the server.

   .. note::

      See :attr:`KRPC.clients`.
   .. attribute:: id: string

      The identifier of the client.


   .. attribute:: name: string

      The name of the client, or an empty string if it has no name.


   .. attribute:: address: string

      The address of the client.


   .. attribute:: protocol_version: number

      The protocol version of the client's connection.



.. class:: ServerSettings

   The settings that control how the server executes calls.

   .. note::

      See :attr:`KRPC.ServerStatus.settings`.
   .. attribute:: one_rpc_per_update: boolean

      Whether the server executes one call per update.


   .. attribute:: max_time_per_update: number

      The current maximum time spent executing calls per update, in microseconds.


   .. attribute:: adaptive_rate_control: boolean

      Whether the server adjusts :attr:`KRPC.ServerSettings.max_time_per_update` to hold the frame rate.


   .. attribute:: blocking_recv: boolean

      Whether the server waits for the next call in an update.


   .. attribute:: recv_timeout: number

      The time the server waits for the next call, in microseconds.


   .. attribute:: tick_hold_timeout: number

      The longest time a client may hold the game on one physics tick, in microseconds.



.. class:: ServerStatistics

   The performance statistics of the server.

   .. note::

      See :attr:`KRPC.ServerStatus.statistics`.
   .. attribute:: bytes_read: number

      The number of bytes received from clients.


   .. attribute:: bytes_written: number

      The number of bytes sent to clients.


   .. attribute:: bytes_read_rate: number

      The rate bytes are received from clients, in bytes per second.


   .. attribute:: bytes_written_rate: number

      The rate bytes are sent to clients, in bytes per second.


   .. attribute:: rpcs_executed: number

      The number of calls executed.


   .. attribute:: rpc_rate: number

      The number of calls executed per second.


   .. attribute:: time_per_rpc_update: number

      The time spent receiving and executing calls per update, in seconds.


   .. attribute:: poll_time_per_rpc_update: number

      The time spent receiving calls per update, in seconds.


   .. attribute:: exec_time_per_rpc_update: number

      The time spent executing calls per update, in seconds.


   .. attribute:: deferred_calls: number

      The number of calls left for the next update, because the update ran out of time
      or the call yielded.


   .. attribute:: streams: number

      The number of open streams, events included.


   .. attribute:: stream_evaluations: number

      The number of stream evaluations, one per stream per update.


   .. attribute:: stream_evaluation_rate: number

      The number of stream evaluations per second.


   .. attribute:: stream_updates_sent: number

      The number of stream updates sent to clients.


   .. attribute:: time_per_stream_update: number

      The time spent updating streams per update, in seconds.



.. class:: ServerStatus

   The status of the server.

   .. note::

      See :meth:`KRPC.get_status`.
   .. attribute:: version: string

      The version of the server, or ``unknown``.


   .. attribute:: protocol_version: number

      The protocol version of the connection.


   .. attribute:: settings: KRPC.ServerSettings

      The settings of the server.


   .. attribute:: statistics: KRPC.ServerStatistics

      The performance statistics of the server.



.. class:: ArgumentException

   A method was invoked where at least one of the passed arguments does not
   meet the parameter specification of the method.

   Raised as a :class:`krpc.ServerError` whose ``service`` and ``name`` are ``'KRPC'`` and ``'ArgumentException'``.



.. class:: ArgumentNullException

   A null reference was passed to a method that does not accept it as a valid argument.

   Raised as a :class:`krpc.ServerError` whose ``service`` and ``name`` are ``'KRPC'`` and ``'ArgumentNullException'``.



.. class:: ArgumentOutOfRangeException

   The value of an argument is outside the allowable range of values as defined by the invoked method.

   Raised as a :class:`krpc.ServerError` whose ``service`` and ``name`` are ``'KRPC'`` and ``'ArgumentOutOfRangeException'``.



.. class:: DivideByZeroException

   An integer was divided by zero.

   Raised as a :class:`krpc.ServerError` whose ``service`` and ``name`` are ``'KRPC'`` and ``'DivideByZeroException'``.



.. class:: Error

   A general error, described by its message.

   Raised as a :class:`krpc.ServerError` whose ``service`` and ``name`` are ``'KRPC'`` and ``'Error'``.



.. class:: IndexOutOfRangeException

   An index was outside the bounds of a collection, string or array.

   Raised as a :class:`krpc.ServerError` whose ``service`` and ``name`` are ``'KRPC'`` and ``'IndexOutOfRangeException'``.



.. class:: InvalidOperationException

   A method call was made to a method that is invalid
   given the current state of the object.

   Raised as a :class:`krpc.ServerError` whose ``service`` and ``name`` are ``'KRPC'`` and ``'InvalidOperationException'``.



.. class:: KeyNotFoundException

   A key was not found in a dictionary.

   Raised as a :class:`krpc.ServerError` whose ``service`` and ``name`` are ``'KRPC'`` and ``'KeyNotFoundException'``.



.. class:: NotSupportedException

   A method was invoked that is not supported.

   Raised as a :class:`krpc.ServerError` whose ``service`` and ``name`` are ``'KRPC'`` and ``'NotSupportedException'``.



.. class:: NullReferenceException

   A value was null where its type does not allow one.

   Raised as a :class:`krpc.ServerError` whose ``service`` and ``name`` are ``'KRPC'`` and ``'NullReferenceException'``.



.. class:: ObjectDestroyedException

   The game object that a service object refers to no longer exists,
   so the object cannot be used. For example, the part it represents
   was destroyed, or the vessel it belongs to was recovered.

   Raised as a :class:`krpc.ServerError` whose ``service`` and ``name`` are ``'KRPC'`` and ``'ObjectDestroyedException'``.

   .. note::

      See :class:`KRPC.ObjectNotLoadedException` for a game object that exists but is not
      loaded.

.. class:: ObjectNotLoadedException

   The game object that a service object refers to exists, but is not loaded.
   For example, the game is loading, or the vessel carrying the part is unloaded.

   Raised as a :class:`krpc.ServerError` whose ``service`` and ``name`` are ``'KRPC'`` and ``'ObjectNotLoadedException'``.

   .. note::

      See :class:`KRPC.ObjectDestroyedException` for a game object that no longer exists.

.. class:: OverflowException

   An arithmetic or conversion operation overflowed.

   Raised as a :class:`krpc.ServerError` whose ``service`` and ``name`` are ``'KRPC'`` and ``'OverflowException'``.



.. class:: StreamRemovedException

   A stream was removed by the server, e.g. an event that has finished.

   Raised as a :class:`krpc.ServerError` whose ``service`` and ``name`` are ``'KRPC'`` and ``'StreamRemovedException'``.



.. class:: TimeoutException

   An operation did not complete in the time allotted to it.

   Raised as a :class:`krpc.ServerError` whose ``service`` and ``name`` are ``'KRPC'`` and ``'TimeoutException'``.



.. class:: UnsupportedSceneException

   The procedure is not available in the current game scene.

   Raised as a :class:`krpc.ServerError` whose ``service`` and ``name`` are ``'KRPC'`` and ``'UnsupportedSceneException'``.
