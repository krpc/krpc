export { Connection, type ConnectOptions, connect, type DynamicConnection } from "./connection.js";
export { declareEnumeration, declareException, declareStruct } from "./declarations.js";
export {
  CompilationError,
  ConnectionError,
  EncodingError,
  RemoteError,
  ServerError,
  type ServerErrorDetails,
  StreamError,
} from "./errors.js";
export { getObjectId, RemoteObject, RemoteService } from "./objects.js";
export type * as schema from "./schema/krpc_pb.js";
export type {
  ArgumentOf,
  ArgumentsOf,
  ClientTypes,
  OnClient,
  OnServer,
  ServerFunction,
  ServerObject,
  ServerTypes,
} from "./server.js";
export * from "./services/index.js";
export { Event, Stream } from "./streams.js";
export { RemoteEnum, RemoteFlags, RemoteStruct } from "./types.js";
export {
  type NamedType,
  type TypeLike,
  TypeValue,
  types,
  type ValueOf,
  type ValuesOf,
} from "./typevalues.js";
export { equals } from "./values.js";
