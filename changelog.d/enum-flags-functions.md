## core

- Take a flags enumeration value in `KRPC.Expression.Not`, which gives the declared values it does
  not hold (#1313)

## client/python

- Compile `|`, `&`, `^`, `~`, `in` and truth tests on flags enumeration values in server side
  functions (#1313)

## client/csharp

- Compile `~` and `HasFlag` on flags enumeration values in server side functions (#1313)
