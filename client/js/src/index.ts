export { Connection, type ConnectOptions, connect, type DynamicConnection } from "./connection.js";
export {
  ConnectionError,
  EncodingError,
  RemoteError,
  ServerError,
  type ServerErrorDetails,
} from "./errors.js";
export { getObjectId, RemoteObject } from "./objects.js";
export { RemoteEnum, RemoteStruct } from "./types.js";
export { equals } from "./values.js";
