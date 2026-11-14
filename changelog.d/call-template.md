## client/python

- Add `Connection.get_call_template`, which builds a call with no instance or arguments, for
  `Expression.call_with_arguments` (#1352)
- Take a member, such as `Engine.has_fuel`, as its call template where a call is expected (#1352)

## client/lua

- Add `Connection:get_call_template`, which builds a call with no instance or arguments, for
  `Expression.call_with_arguments` (#1352)
- Take a procedure, such as `Engine.get_has_fuel`, as its call template where a call is expected (#1352)

## client/csharp

- Add `Connection.GetCallTemplate` and `Connection.GetSetCallTemplate`, which build a call with
  no instance or arguments, for `Expression.CallWithArguments` (#1352)

## client/java

- Add `Connection.getCallTemplate`, which builds a call with no instance or arguments, for
  `Expression.callWithArguments` (#1352)

## client/cpp

- Add a static `_call_template` function for each member, which builds a call with no instance
  or arguments, for `Expression::call_with_arguments` (#1352)

## client/cnano

- Add a `_call_template` function for each member, which fills in a call with no instance or
  arguments, for `krpc_KRPC_Expression_CallWithArguments` (#1352)

## core

- **Breaking:** `KRPC.Expression.CallWithArguments` takes a call template, and throws an exception
  for a call that carries arguments (#1352)
- Fail when a server side function is built, in place of when it runs, for a null constant passed
  to a parameter that cannot be null (#1352)
