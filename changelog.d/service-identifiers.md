## core

- **Breaking:** Reject members of a service or class, values of an enumeration, fields of a
  structure, or parameters of a procedure that share a name, ignoring case (#1338)
- **Breaking:** Reject a method `GetFoo` or `SetFoo` beside a property `Foo` (#1338)
- **Breaking:** Reject parameter and enumeration value names that are not ASCII letters and digits,
  starting with a letter (#1338)
