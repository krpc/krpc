## client/python

- Services
  - Add `connect[T]` and `connect_local[T]`, which type the client as `T` for type hints, such as for a mod's services (#1303)
  - Add a client type to each service's module, such as `SpaceCenterClient`, to combine with others in a subclass (#1303)
