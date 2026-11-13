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

/** A procedure that threw an exception on the server. The message starts with the service and
 * name of the exception, for an exception a service lists. */
export class ServerError extends RemoteError {
  /** The service that lists the exception, or an empty string. */
  readonly service: string;
  /** The name of the exception, or an empty string. */
  readonly exception: string;
  /** The description of the exception, from the server. */
  readonly description: string;
  /** The stack trace of the exception on the server, or an empty string. */
  readonly stackTrace: string;

  constructor(error: {
    service: string;
    exception: string;
    description: string;
    stackTrace: string;
  }) {
    let message = error.description;
    if (error.exception !== "") {
      message = `${error.service}.${error.exception}: ${message}`;
    }
    if (error.stackTrace !== "") {
      message += `\nServer stack trace:\n${error.stackTrace}`;
    }
    super(message);
    this.service = error.service;
    this.exception = error.exception;
    this.description = error.description;
    this.stackTrace = error.stackTrace;
  }
}

/** A value that failed to encode or decode, such as a malformed result from the server. */
export class EncodingError extends RemoteError {}
