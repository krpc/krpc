'use strict'

var websocket = require('ws');
var protobufjs = require('protobufjs')
var proto = protobufjs.loadProtoFile('krpc.proto').build();
var schema = proto.krpc.schema;

console.log('Connecting to the server')
let conn = new websocket('ws://127.0.0.1:50000')
conn.binaryType = 'arraybuffer'

conn.onopen = (evnt) => {
  // The handshake is the first binary message
  let request = new schema.ConnectionRequest({client_name: 'Jeb', protocol_version: 2});
  conn.send(new schema.Message({connection_request: request}).toArrayBuffer());
};

conn.onmessage = (evnt) => {
  let message = schema.Message.decode(evnt.data);
  if (message.connection_response) {
    if (message.connection_response.status !== schema.ConnectionResponse.Status.OK)
      throw new Error('Connection failed: ' + message.connection_response.message);
    console.log('Successfully connected');

    // Call KRPC.AddStream to stream KRPC.GetStatus, in a request with the id 1
    let call_to_stream = new schema.ProcedureCall('KRPC', 'GetStatus');
    let arg = new schema.Argument(0, call_to_stream.toArrayBuffer());
    let call = new schema.ProcedureCall('KRPC', 'AddStream', [arg]);
    conn.send(new schema.Message({request: new schema.Request({id: 1, calls: [call]})}).toArrayBuffer());
  } else if (message.response) {
    let stream = schema.Stream.decode(message.response.results[0].value);
    console.log('added stream id =', stream.id.toString());
  } else if (message.stream_update) {
    // Stream updates arrive on the same connection as the responses
    let status = schema.Status.decode(message.stream_update.results[0].result.value);
    console.log(status);
  }
};
