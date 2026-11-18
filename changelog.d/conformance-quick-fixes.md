## client/cpp

- Throw `std::invalid_argument` for a default-constructed object passed as an argument or called
  through, before the call is sent

## client/csharp

- Order remote objects by id, with `CompareTo` and the comparison operators

## client/java

- Fix remote objects with an id past 63 bits ordering first

## client/lua

- Order remote objects by id with `<`
