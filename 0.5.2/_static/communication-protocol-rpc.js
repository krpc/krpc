var websocket = require('ws');
var protobufjs = require('protobufjs')
var ByteBuffer = require('bytebuffer')
var proto = protobufjs.loadProtoFile('krpc.proto').build();

console.log('Connecting to RPC server')
let rpcConn = new websocket('ws://127.0.0.1:50000')
rpcConn.binaryType = 'arraybuffer'

rpcConn.onopen = (e) => {
  console.log('Successfully connected')
  let call = new proto.krpc.schema.ProcedureCall('KRPC', 'GetStatus');
  let request = new proto.krpc.schema.Request([call]);
  rpcConn.send(request.toArrayBuffer());
  rpcConn.onmessage = (e) => {
    let response = proto.krpc.schema.Response.decode(e.data)
    let status = proto.krpc.schema.Status.decode(response.results[0].value)
    console.log(status);
    process.exit(0);
  };
};
