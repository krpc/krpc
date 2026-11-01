'use strict';

const WebSocket = require('ws');
const protobuf = require('protobufjs');

const root = protobuf.loadSync('krpc.proto');
const Message = root.lookupType('krpc.schema.Message');
const ProcedureCall = root.lookupType('krpc.schema.ProcedureCall');
const Stream = root.lookupType('krpc.schema.Stream');
const Tuple = root.lookupType('krpc.schema.Tuple');

function send(conn, message) {
  conn.send(Message.encode(Message.fromObject(message)).finish());
}

function receive(event) {
  const message = Message.decode(new Uint8Array(event.data));
  return Message.toObject(message, {enums: String, longs: Number, defaults: true});
}

// Open a websockets connection to the server
const conn = new WebSocket('ws://127.0.0.1:50000/');
conn.binaryType = 'arraybuffer';

conn.onopen = () => {
  // Send a connection request
  send(conn, {connectionRequest: {clientName: 'Example', protocolVersion: 2}});
};

let stream = null;

conn.onmessage = (event) => {
  const message = receive(event);
  if (message.connectionResponse) {
    // Check the connection was successful
    const response = message.connectionResponse;
    if (response.status !== 'OK')
      throw new Error('Connection failed: ' + response.message);
    if (response.protocolVersion !== 2)
      throw new Error('The server does not support protocol version 2');
    console.log('Connected to the server');

    // Build a KRPC.GetStatus call to be streamed
    const streamCall = ProcedureCall.encode({service: 'KRPC', procedure: 'GetStatus'}).finish();

    // Call KRPC.AddStream to add the stream, in a request with the id 1
    const call = {service: 'KRPC', procedure: 'AddStream', arguments: [{position: 0, value: streamCall}]};
    send(conn, {request: {id: 1, calls: [call]}});
  } else if (message.response) {
    // Check for an error in the response, and in the results
    const response = message.response;
    if (response.error)
      throw new Error('ERROR: ' + response.error.description);
    if (response.results[0].error)
      throw new Error('ERROR: ' + response.results[0].error.description);

    // Decode the return value as a Stream message. The stream's first update follows it.
    stream = Stream.toObject(Stream.decode(response.results[0].value), {longs: Number});
  } else if (message.streamUpdate) {
    // Decode and print the return value of each stream update
    for (const result of message.streamUpdate.results) {
      if (result.id === stream.id) {
        // The value is a ServerStatus structure sent as a Tuple of its fields. Its fourth field
        // is the server statistics, another structure, whose second field is the number of
        // bytes the server has written.
        const status = Tuple.decode(result.result.value);
        const statistics = Tuple.decode(status.items[3]);
        console.log(protobuf.Reader.create(statistics.items[1]).uint64().toString());
      }
    }
  }
};
