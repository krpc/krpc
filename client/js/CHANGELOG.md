## [v0.8.0] - unreleased
- Add a JavaScript client, for web pages and Node.js, that connects over WebSockets (#1346)
- Export each service's enumerations, structures and exceptions from the package, for use without a
  connection (#1346)
- Add streams, with `Connection.addStream`, and events, each with callbacks (#1346)
- Add server side functions built with the expression API, run with `Connection.run`, or
  streamed with `Connection.addStream` and `Connection.addEvent` (#1346)
- Compile a JavaScript function into a server side function, with its captured values (#1346)
