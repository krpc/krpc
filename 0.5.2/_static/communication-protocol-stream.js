'use strict'

var websocket = require('ws');
var protobufjs = require('protobufjs')
var proto = protobufjs.loadProtoFile('krpc.proto').build();

console.log('Connecting to RPC server')
let rpcConn = new websocket('ws://127.0.0.1:50000')
rpcConn.binaryType = 'arraybuffer'

rpcConn.onopen = (evnt) => {
  console.log('Successfully connected')
  console.log('Calling KRPC.GetClientID')
  let call = new proto.krpc.schema.ProcedureCall('KRPC', 'GetClientID');
  let request = new proto.krpc.schema.Request([call]);
  rpcConn.send(request.toArrayBuffer());
};

rpcConn.onmessage = (evnt) => {
  let response = proto.krpc.schema.Response.decode(evnt.data);
  response.results[0].value.readVarint32(); // skip size
  let client_identifier = response.results[0].value.toBase64();
  console.log('Client identifier =', client_identifier);

  console.log('Connecting to Stream server');
  let streamConn = new websocket('ws://127.0.0.1:50001?id=' + client_identifier);
  streamConn.binaryType = 'arraybuffer';

  streamConn.onopen = (evnt) => {
    console.log('Successfully connected');

    let call_to_stream = new proto.krpc.schema.ProcedureCall('KRPC', 'GetStatus');
    let arg = new proto.krpc.schema.Argument(0, call_to_stream.toArrayBuffer());
    let call = new proto.krpc.schema.ProcedureCall('KRPC', 'AddStream', [arg]);
    let request = new proto.krpc.schema.Request([call]);
    rpcConn.send(request.toArrayBuffer());
    rpcConn.onmessage = (evnt) => {
      let response = proto.krpc.schema.Response.decode(evnt.data);
      let stream = proto.krpc.schema.Stream.decode(response.results[0].value);
      console.log("added stream id =", stream.id.toString());
    };
  };

  streamConn.onmessage = (evnt) => {
    let value = new proto.krpc.schema.StreamUpdate.decode(evnt.data);
    let status = proto.krpc.schema.Status.decode(value.results[0].result.value)
    console.log(status);
  };
};
