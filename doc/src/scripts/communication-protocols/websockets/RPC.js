'use strict';

const WebSocket = require('ws');
const protobuf = require('protobufjs');

const root = protobuf.loadSync('krpc.proto');
const Message = root.lookupType('krpc.schema.Message');
const Status = root.lookupType('krpc.schema.Status');

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

    // Invoke the KRPC.GetStatus RPC, in a request with the id 1
    send(conn, {request: {id: 1, calls: [{service: 'KRPC', procedure: 'GetStatus'}]}});
  } else if (message.response) {
    // Check for an error in the response, and in the results
    const response = message.response;
    if (response.error)
      throw new Error('ERROR: ' + response.error.description);
    if (response.results[0].error)
      throw new Error('ERROR: ' + response.results[0].error.description);

    // Decode the return value as a Status message, and print it out
    console.log(Status.toObject(Status.decode(response.results[0].value), {longs: Number}));

    // End the connection with a disconnect, then a close frame
    send(conn, {disconnect: {reason: 'CLOSED'}});
    conn.close();
  }
};
