import { create, fromBinary, toBinary } from "@bufbuild/protobuf";
import { ConnectionError, EncodingError, ServerError, type ServerErrorDetails } from "./errors.js";
import {
  type Argument,
  ConnectionResponse_Status,
  Disconnect_Reason,
  DisconnectSchema,
  type Message,
  MessageSchema,
  type ProcedureResult,
  RequestSchema,
} from "./schema/krpc_pb.js";

/** The protocol version of the messages the client sends and accepts. */
export const PROTOCOL_VERSION = 2;

/** The longest delay setTimeout takes, in milliseconds. */
const MAX_DELAY = 2 ** 31 - 1;

/** The time closing waits for the server to close, in milliseconds. */
const CLOSE_TIMEOUT = 1000;

interface PendingCall {
  resolve(result: ProcedureResult): void;
  reject(error: Error): void;
}

/** One connection to a server over WebSockets: the handshake, requests matched to their
 * responses by id, and the close. */
export class Session {
  readonly #socket: WebSocket;
  readonly #pending = new Map<bigint, PendingCall>();
  readonly #closed: Promise<void>;
  #nextId = 1n;
  #ended: ConnectionError | null = null;
  /** Makes the error a call rejects with, for an error the server sends. */
  errorFactory: (error: ServerErrorDetails) => Error = (error) => new ServerError(error);

  private constructor(socket: WebSocket) {
    this.#socket = socket;
    this.#closed = new Promise((resolve) => {
      socket.addEventListener("close", () => {
        this.#end(new ConnectionError("The server closed the connection"));
        resolve();
      });
    });
    // The handler holds no await, so no other code runs while it handles a message
    socket.addEventListener("message", (event) => this.#receive(event.data));
  }

  /** Opens a WebSocket to the server and sends the connection request. The timeout, in
   * milliseconds, covers the upgrade and the handshake. */
  static open(url: string, name: string, timeout: number): Promise<Session> {
    return new Promise((resolve, reject) => {
      let socket: WebSocket;
      try {
        socket = new WebSocket(url);
      } catch (error) {
        reject(
          new ConnectionError(`Failed to connect to ${url}: ${String(error)}`, { cause: error }),
        );
        return;
      }
      socket.binaryType = "arraybuffer";
      // A setTimeout delay above the largest 32-bit integer fires at once
      const timer =
        timeout > MAX_DELAY
          ? undefined
          : setTimeout(() => {
              fail(
                new ConnectionError("Timed out waiting for the server to accept the connection"),
              );
            }, timeout);
      const done = () => {
        clearTimeout(timer);
        socket.removeEventListener("open", onOpen);
        socket.removeEventListener("message", onMessage);
        socket.removeEventListener("close", onClose);
      };
      const fail = (error: ConnectionError) => {
        done();
        socket.close();
        reject(error);
      };
      const onOpen = () => {
        const request = create(MessageSchema, {
          content: {
            case: "connectionRequest",
            value: { clientName: name, protocolVersion: PROTOCOL_VERSION },
          },
        });
        try {
          socket.send(toBinary(MessageSchema, request));
        } catch (error) {
          fail(
            new ConnectionError(`Failed to send the connection request: ${String(error)}`, {
              cause: error,
            }),
          );
        }
      };
      const onMessage = (event: MessageEvent) => {
        let message: Message;
        try {
          message = parse(event.data);
        } catch (error) {
          fail(new ConnectionError(`Malformed connection response: ${String(error)}`));
          return;
        }
        const content = message.content;
        if (content.case === "disconnect") {
          fail(
            new ConnectionError(content.value.message, {
              reason: reasonName(content.value.reason),
            }),
          );
          return;
        }
        if (content.case !== "connectionResponse") {
          fail(
            new ConnectionError(
              `Expected a connection response, got ${content.case ?? "an empty message"}`,
            ),
          );
          return;
        }
        const response = content.value;
        if (response.status !== ConnectionResponse_Status.OK) {
          fail(
            new ConnectionError(response.message, {
              status: ConnectionResponse_Status[response.status] ?? "",
            }),
          );
          return;
        }
        if (response.protocolVersion !== PROTOCOL_VERSION) {
          fail(
            new ConnectionError(`The server accepted protocol version ${response.protocolVersion}`),
          );
          return;
        }
        done();
        resolve(new Session(socket));
      };
      const onClose = () => {
        done();
        reject(new ConnectionError(`Failed to connect to ${url}`));
      };
      socket.addEventListener("open", onOpen);
      socket.addEventListener("message", onMessage);
      socket.addEventListener("close", onClose);
    });
  }

  /** Calls a procedure, and returns its result. Rejects with a ServerError for a procedure that
   * threw an exception, and with a ConnectionError once the connection has ended. */
  call(service: string, procedure: string, args: Argument[]): Promise<ProcedureResult> {
    if (this.#ended !== null) {
      return Promise.reject(this.#ended);
    }
    const id = this.#nextId++;
    const request = create(RequestSchema, { id, calls: [{ service, procedure, arguments: args }] });
    const message = create(MessageSchema, { content: { case: "request", value: request } });
    return new Promise((resolve, reject) => {
      this.#pending.set(id, { resolve, reject });
      try {
        this.#socket.send(toBinary(MessageSchema, message));
      } catch (error) {
        this.#end(
          new ConnectionError(`Failed to send a request: ${String(error)}`, { cause: error }),
        );
        this.#socket.close();
      }
    });
  }

  /** Sends a disconnect and a close frame, then waits for the server to close, for up to a
   * second. Closing a closed connection does nothing. */
  async close(): Promise<void> {
    if (this.#ended === null) {
      this.#end(new ConnectionError("The connection is closed"));
      try {
        this.#sendDisconnect(Disconnect_Reason.CLOSED);
        this.#socket.close(1000);
      } catch {
        // The socket has already failed
      }
    }
    let timer: ReturnType<typeof setTimeout> | undefined;
    const limit = new Promise<void>((resolve) => {
      timer = setTimeout(resolve, CLOSE_TIMEOUT);
    });
    await Promise.race([this.#closed, limit]);
    clearTimeout(timer);
  }

  /** The error that ended the connection, or null while it is open. */
  get ended(): ConnectionError | null {
    return this.#ended;
  }

  #receive(data: unknown): void {
    if (this.#ended !== null) {
      return;
    }
    let message: Message;
    try {
      message = parse(data);
    } catch {
      this.#fail(Disconnect_Reason.MALFORMED_MESSAGE, "A message from the server failed to parse");
      return;
    }
    const content = message.content;
    switch (content.case) {
      case "response": {
        const response = content.value;
        const pending = this.#pending.get(response.id);
        if (pending === undefined) {
          this.#fail(
            Disconnect_Reason.PROTOCOL_ERROR,
            `A response has the unknown id ${response.id}`,
          );
          return;
        }
        this.#pending.delete(response.id);
        const error = response.error ?? response.results[0]?.error;
        if (error !== undefined) {
          pending.reject(this.errorFactory(error));
        } else if (response.results.length !== 1) {
          pending.reject(new EncodingError("A response has no result"));
        } else {
          pending.resolve(response.results[0]);
        }
        return;
      }
      case "disconnect":
        this.#end(
          new ConnectionError(content.value.message, { reason: reasonName(content.value.reason) }),
        );
        this.#socket.close();
        return;
      case "connectionRequest":
      case "connectionResponse":
      case "request":
        this.#fail(Disconnect_Reason.PROTOCOL_ERROR, `The server sent a ${content.case}`);
        return;
      default:
        // A stream update, or a message from a later version of the protocol
        return;
    }
  }

  /** Ends the connection with a disconnect, for an error in what the server sent. */
  #fail(reason: Disconnect_Reason, message: string): void {
    try {
      this.#sendDisconnect(reason);
    } catch {
      // The socket has already failed
    }
    this.#end(new ConnectionError(message, { reason: Disconnect_Reason[reason] }));
    this.#socket.close();
  }

  #sendDisconnect(reason: Disconnect_Reason): void {
    const disconnect = create(DisconnectSchema, { reason });
    const message = create(MessageSchema, { content: { case: "disconnect", value: disconnect } });
    this.#socket.send(toBinary(MessageSchema, message));
  }

  /** Records the error that ended the connection, and rejects each call waiting for a
   * response with it. */
  #end(error: ConnectionError): void {
    if (this.#ended !== null) {
      return;
    }
    this.#ended = error;
    for (const pending of this.#pending.values()) {
      pending.reject(error);
    }
    this.#pending.clear();
  }
}

function parse(data: unknown): Message {
  if (!(data instanceof ArrayBuffer)) {
    throw new Error("A text message");
  }
  return fromBinary(MessageSchema, new Uint8Array(data));
}

/** The name of a disconnect reason, or its number for a reason the schema does not name. */
function reasonName(reason: Disconnect_Reason): string {
  return Disconnect_Reason[reason] ?? String(reason);
}
