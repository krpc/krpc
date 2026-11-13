/** The base class of the errors the client throws and rejects with. */
export class RemoteError extends Error {
  constructor(message: string, options?: ErrorOptions) {
    super(message, options);
    this.name = new.target.name;
  }
}

/** A connection that failed, was refused or is closed. */
export class ConnectionError extends RemoteError {
  /** The status of a refused connection, such as `DENIED`, or an empty string. */
  readonly status: string;
  /** The reason in the server's disconnect message, such as `SERVER_STOPPED`, or an empty
   * string. */
  readonly reason: string;

  constructor(
    message: string,
    details: { status?: string; reason?: string; cause?: unknown } = {},
  ) {
    super(message, details.cause === undefined ? undefined : { cause: details.cause });
    this.status = details.status ?? "";
    this.reason = details.reason ?? "";
  }
}

/** An error the server sends for a call. */
export interface ServerErrorDetails {
  readonly service: string;
  readonly exception: string;
  readonly description: string;
  readonly stackTrace: string;
}

/** A procedure that threw an exception on the server. Each exception a service lists has a class
 * of its own, on the service, such as `conn.krpc.ObjectDestroyedException`. One the server maps to
 * a .NET exception extends the matching JavaScript error alone, such as `TypeError`. */
export class ServerError extends RemoteError {
  /** The service that lists the exception, or an empty string. */
  readonly service: string;
  /** The name of the exception, or an empty string. */
  readonly exception: string;
  /** The description of the exception, from the server. */
  readonly description: string;
  /** The stack trace of the exception on the server, or an empty string. */
  readonly stackTrace: string;

  /** The message names the exception, for an exception the client has no class for. */
  constructor(error: ServerErrorDetails, message = `${prefix(error)}${serverMessage(error)}`) {
    super(message);
    this.service = error.service;
    this.exception = error.exception;
    this.description = error.description;
    this.stackTrace = error.stackTrace;
  }
}

/** The JavaScript error each .NET exception the server maps one to extends. */
const NATIVE_ERRORS: ReadonlyMap<string, ErrorConstructor> = new Map([
  ["System.ArgumentException", TypeError],
  ["System.ArgumentNullException", TypeError],
  ["System.ArgumentOutOfRangeException", RangeError],
  ["System.NullReferenceException", TypeError],
  ["System.IndexOutOfRangeException", RangeError],
  ["System.DivideByZeroException", RangeError],
  ["System.OverflowException", RangeError],
]);

/** The JavaScript error the server's errors of a .NET exception extend, or undefined for a
 * ServerError. */
export function nativeError(mappedType: string): ErrorConstructor | undefined {
  return NATIVE_ERRORS.get(mappedType);
}

/** The class of an exception a service lists, given the JavaScript error it extends, or
 * undefined for a ServerError. The message of its errors is the description and stack trace. */
export function exceptionClass(
  name: string,
  native: ErrorConstructor | undefined,
): new (
  error: ServerErrorDetails,
) => Error {
  let cls: new (error: ServerErrorDetails) => Error;
  if (native === undefined) {
    cls = class extends ServerError {
      constructor(error: ServerErrorDetails) {
        super(error, serverMessage(error));
      }
    };
  } else {
    const nativeClass = class extends native {
      readonly service: string;
      readonly exception: string;
      readonly description: string;
      readonly stackTrace: string;

      constructor(error: ServerErrorDetails) {
        super(serverMessage(error));
        this.name = new.target.name;
        this.service = error.service;
        this.exception = error.exception;
        this.description = error.description;
        this.stackTrace = error.stackTrace;
      }
    };
    cls = nativeClass;
  }
  Object.defineProperty(cls, "name", { value: name });
  return cls;
}

function prefix(error: ServerErrorDetails): string {
  return error.exception === "" ? "" : `${error.service}.${error.exception}: `;
}

function serverMessage(error: ServerErrorDetails): string {
  let message = error.description;
  if (error.stackTrace !== "") {
    message += `\nServer stack trace:\n${error.stackTrace}`;
  }
  return message;
}

/** A value that failed to encode or decode, such as a malformed result from the server. */
export class EncodingError extends RemoteError {}

/** A stream or an event that was removed, by the program or by the server, or that cannot be
 * added. Reading a stream's value before its first value throws one too. It is the class of
 * `KRPC.StreamRemovedException`. */
export class StreamError extends RemoteError {}

/** Whether an exception is the one the server removes a stream with, which is a StreamError. */
export function isStreamRemoved(service: string, name: string): boolean {
  return service === "KRPC" && name === "StreamRemovedException";
}

/** A function the client cannot compile into a server side function. The message names the
 * construct, and its line and column in the function's source. */
export class CompilationError extends RemoteError {
  /** The line of the construct in the function's source, counting from 1, or 0 for none. */
  readonly line: number;
  /** The column of the construct in its line, counting from 1, or 0 for none. */
  readonly column: number;

  constructor(message: string, line = 0, column = 0) {
    super(line === 0 ? message : `${message} (line ${line}, column ${column} of the function)`);
    this.line = line;
    this.column = column;
  }
}
