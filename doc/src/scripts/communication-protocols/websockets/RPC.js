var websocket = require('ws');
var protobufjs = require('protobufjs')
var proto = protobufjs.loadProtoFile('krpc.proto').build();
var schema = proto.krpc.schema;

console.log('Connecting to the server')
let conn = new websocket('ws://127.0.0.1:50000')
conn.binaryType = 'arraybuffer'

conn.onopen = (e) => {
  // The handshake is the first binary message
  let request = new schema.ConnectionRequest({client_name: 'Jeb', protocol_version: 2});
  conn.send(new schema.Message({connection_request: request}).toArrayBuffer());
  conn.onmessage = (e) => {
    let response = schema.Message.decode(e.data).connection_response;
    if (response.status !== schema.ConnectionResponse.Status.OK)
      throw new Error('Connection failed: ' + response.message);
    console.log('Successfully connected');

    // Invoke the KRPC.GetStatus RPC, in a request with the id 1
    let call = new schema.ProcedureCall('KRPC', 'GetStatus');
    conn.send(new schema.Message({request: new schema.Request({id: 1, calls: [call]})}).toArrayBuffer());
    conn.onmessage = (e) => {
      let response = schema.Message.decode(e.data).response;
      let status = schema.Status.decode(response.results[0].value);
      console.log(status);

      // End the connection with a disconnect, then a close frame
      let disconnect = new schema.Disconnect({reason: schema.Disconnect.Reason.CLOSED});
      conn.send(new schema.Message({disconnect: disconnect}).toArrayBuffer());
      conn.close();
    };
    conn.onclose = (e) => process.exit(0);
  };
};
