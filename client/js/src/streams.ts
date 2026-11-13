import { create, toBinary } from "@bufbuild/protobuf";
import { decode, encode, type ObjectFactory } from "./codec.js";
import { ConnectionError, EncodingError, StreamError } from "./errors.js";
import {
  type Argument,
  ArgumentSchema,
  type ProcedureCall,
  ProcedureCallSchema,
  type ProcedureResult,
  type StreamResult,
} from "./schema/krpc_pb.js";
import type { Session, StreamHandler } from "./session.js";
import type { Type } from "./types.js";

// The symbol an `await using` declaration calls, which a program's TypeScript library can leave out
declare global {
  interface SymbolConstructor {
    readonly asyncDispose: unique symbol;
  }
}

/** The longest delay setTimeout takes, in milliseconds. */
const MAX_DELAY = 2 ** 31 - 1;

const UINT64: Type = { kind: "uint64", nullable: false };
const FLOAT: Type = { kind: "float", nullable: false };
const BOOL: Type = { kind: "bool", nullable: false };

/** Reports an error a callback threw, with reportError where the runtime has it, and carries on. */
function report(error: unknown): void {
  const reportError = (globalThis as { reportError?: (error: unknown) => void }).reportError;
  if (typeof reportError === "function") {
    reportError(error);
  } else {
    console.error(error);
  }
}

function isPromiseLike(value: unknown): value is PromiseLike<unknown> {
  return (
    (typeof value === "object" || typeof value === "function") &&
    value !== null &&
    typeof (value as { then?: unknown }).then === "function"
  );
}

/** The timeout of a wait, in milliseconds. Undefined and Infinity wait with no deadline. */
function waitTimeout(timeout: number | undefined): number {
  if (timeout === undefined) {
    return Number.POSITIVE_INFINITY;
  }
  if (!(timeout >= 0)) {
    throw new RangeError(`The timeout ${timeout} is negative or NaN`);
  }
  return timeout;
}

/** A function the program adds to a stream, an event or the connection. A callback runs one call
 * at a time: while a call's promise is pending, a newer value waits for it to settle, and
 * replaces an older one that is waiting. */
class Callback {
  /** The version of the last value passed, or the firing count for an event callback. */
  version = 0;
  running = false;
  removed = false;
  pending: { version: number; value: unknown } | null = null;

  constructor(
    readonly fn: (...args: never[]) => unknown,
    readonly kind: "value" | "event" | "update",
    /** Runs once a call has settled and no value waits. */
    readonly idle: () => void = () => {},
  ) {}

  /** Calls the callback with a value, or queues the value while a call runs. Returns false,
   * passing nothing, for a removed callback or a value no newer than the last one. */
  pass(version: number, value: unknown): boolean {
    if (this.removed || version <= this.version) {
      return false;
    }
    if (this.running) {
      this.pending = { version, value };
      return true;
    }
    this.version = version;
    let result: unknown;
    try {
      const fn = this.fn as (...args: unknown[]) => unknown;
      result = this.kind === "value" ? fn(value) : fn();
    } catch (error) {
      report(error);
    }
    if (isPromiseLike(result)) {
      this.running = true;
      result.then(
        () => this.#returned(),
        (error: unknown) => {
          report(error);
          this.#returned();
        },
      );
    }
    return true;
  }

  #returned(): void {
    this.running = false;
    const pending = this.pending;
    this.pending = null;
    if (pending !== null) {
      this.pass(pending.version, pending.value);
    }
    if (!this.running) {
      this.idle();
    }
  }
}

/** Calls end callbacks with the error that ended their stream. */
function callEnd(callbacks: readonly ((error: Error) => unknown)[], ending: Error): void {
  for (const fn of callbacks) {
    try {
      const result = fn(ending);
      if (isPromiseLike(result)) {
        result.then(undefined, report);
      }
    } catch (error) {
      report(error);
    }
  }
}

/** The state of one stream, which every handle to it shares. The message handler stores each
 * value, and a microtask after it passes the value to the callbacks. */
class StreamState {
  hasValue = false;
  value: unknown;
  /** The number of values received. */
  version = 0;
  /** The number of true values received, which for an event is its firings. */
  fired = 0;
  /** The highest firing count passed to an event callback. */
  callbacksFired = 0;
  ending: Error | null = null;
  /** Whether a remove or a close ended the stream, which discards an unseen firing. */
  endedByClient = false;
  rate = 0;
  callbacks: Callback[] = [];
  endCallbacks: ((error: Error) => unknown)[] = [];
  readonly #waiters = new Set<() => void>();
  /** The end callbacks of a server ending, waiting for the value callbacks to settle. */
  #deferred: ((error: Error) => unknown)[] = [];

  constructor(
    readonly id: bigint,
    /** The type of the stream's values, or null for a procedure with no return value. */
    readonly type: Type | null,
  ) {}

  /** Resolves with true on the next value or ending, or with false once the timeout or the
   * longest delay elapses. */
  changed(timeout: number): Promise<boolean> {
    return new Promise((resolve) => {
      let timer: ReturnType<typeof setTimeout> | undefined;
      const wake = () => {
        clearTimeout(timer);
        resolve(true);
      };
      this.#waiters.add(wake);
      if (Number.isFinite(timeout)) {
        // A longer timeout wakes at the longest delay, and the caller waits again
        const delay = Math.min(timeout, MAX_DELAY);
        timer = setTimeout(() => {
          this.#waiters.delete(wake);
          resolve(false);
        }, delay);
      }
    });
  }

  setValue(value: unknown): void {
    this.value = value;
    this.hasValue = true;
    this.version++;
    if (value === true) {
      this.fired++;
    }
    this.#wake();
  }

  /** Ends the stream, and returns the end callbacks to call. Returns none for a stream that has
   * already ended. A remove or a close drops the values waiting for callbacks. */
  end(ending: Error, byClient: boolean): ((error: Error) => unknown)[] {
    if (this.ending !== null) {
      return [];
    }
    this.ending = ending;
    this.endedByClient = byClient;
    if (byClient) {
      for (const callback of this.callbacks) {
        callback.removed = true;
        callback.pending = null;
      }
    }
    this.#wake();
    const callbacks = this.endCallbacks;
    this.endCallbacks = [];
    return callbacks;
  }

  /** Calls the end callbacks of a server ending, once the value callbacks have settled, so a
   * value that arrived before the ending reaches them first. */
  serverEnded(callbacks: ((error: Error) => unknown)[]): void {
    this.#deferred.push(...callbacks);
    this.#endIfIdle();
  }

  #endIfIdle(): void {
    if (this.#deferred.length === 0 || this.callbacks.some((x) => x.running)) {
      return;
    }
    const callbacks = this.#deferred;
    this.#deferred = [];
    callEnd(callbacks, this.ending as Error);
  }

  /** Passes a value to the callbacks the stream had when the value arrived. */
  dispatch(callbacks: readonly Callback[], version: number, fired: number, value: unknown): void {
    if (this.endedByClient) {
      return;
    }
    for (const callback of callbacks) {
      if (callback.kind === "value") {
        callback.pass(version, value);
      } else if (value === true && callback.pass(fired, value)) {
        this.callbacksFired = Math.max(this.callbacksFired, fired);
      }
    }
  }

  addCallback(fn: (...args: never[]) => unknown, kind: "value" | "event"): Callback {
    const callback = new Callback(fn, kind, () => this.#endIfIdle());
    this.callbacks = [...this.callbacks, callback];
    return callback;
  }

  removeCallback(fn: unknown, kind: "value" | "event"): void {
    for (const callback of this.callbacks) {
      if (callback.fn === fn && callback.kind === kind) {
        callback.removed = true;
        callback.pending = null;
      }
    }
    this.callbacks = this.callbacks.filter((x) => !x.removed);
    this.#endIfIdle();
  }

  addEndCallback(fn: (error: Error) => unknown): void {
    if (this.ending === null) {
      this.endCallbacks = [...this.endCallbacks, fn];
    } else {
      // A stream that has already ended calls the callback now, as no ending is to come
      callEnd([fn], this.ending);
    }
  }

  removeEndCallback(fn: unknown): void {
    this.endCallbacks = this.endCallbacks.filter((x) => x !== fn);
  }

  #wake(): void {
    const waiters = [...this.#waiters];
    this.#waiters.clear();
    for (const wake of waiters) {
      wake();
    }
  }
}

/** The streams of a connection. The session passes it each stream update, and the end of the
 * connection. */
export class StreamManager implements StreamHandler {
  readonly #session: Session;
  readonly #objects: ObjectFactory;
  readonly #streams = new Map<bigint, StreamState>();
  #updateCallbacks: Callback[] = [];
  #updates = 0;
  /** The error streams end with once the connection has ended. */
  #closed: ConnectionError | null = null;

  constructor(session: Session, objects: ObjectFactory) {
    this.#session = session;
    this.#objects = objects;
    session.streams = this;
  }

  /** Adds a stream of a call, whose values have the given type, or null for none. */
  add<T>(call: ProcedureCall, type: Type | null): Promise<Stream<T>> {
    return this.addWith("AddStream", toBinary(ProcedureCallSchema, call), type);
  }

  /** Adds a stream with a KRPC procedure that returns one, given its encoded argument. */
  async addWith<T>(procedure: string, argument: Uint8Array, type: Type | null): Promise<Stream<T>> {
    let state: StreamState | undefined;
    const args = [create(ArgumentSchema, { position: 0, value: argument })];
    await this.#session.call("KRPC", procedure, args, (result) => {
      state = this.#register(streamId(result), type);
    });
    return new Stream<T>(state as StreamState, this);
  }

  /** A function that registers the stream of an event a call returns, for the session to run on
   * the result, and a function that makes the event once the call has returned. */
  event(): { receive: (result: ProcedureResult) => void; make: () => Event } {
    let state: StreamState | undefined;
    return {
      receive: (result) => {
        state = this.#register(streamId(result), BOOL);
      },
      make: () => new Event(new Stream<boolean>(state as StreamState, this)),
    };
  }

  /** Registers a stream the server has added, before the session handles its first update. */
  #register(id: bigint, type: Type | null): StreamState {
    const existing = this.#streams.get(id);
    if (existing !== undefined) {
      return existing;
    }
    const state = new StreamState(id, type);
    if (this.#closed === null) {
      this.#streams.set(id, state);
    } else {
      state.end(this.#closed, true);
    }
    return state;
  }

  /** Removes a stream, and calls its end callbacks. Removing a stream that has ended does
   * nothing. */
  async remove(state: StreamState): Promise<void> {
    if (this.#streams.get(state.id) !== state) {
      return;
    }
    this.#streams.delete(state.id);
    const ending = new StreamError("The stream has been removed");
    // Ended first, so a value that arrives during the call is dropped
    callEnd(state.end(ending, true), ending);
    await this.#call("RemoveStream", [encode(UINT64, state.id)]);
  }

  async setRate(state: StreamState, rate: number): Promise<void> {
    await this.#call("SetStreamRate", [encode(UINT64, state.id), encode(FLOAT, rate)]);
    state.rate = rate;
  }

  addUpdateCallback(fn: () => unknown): void {
    this.#updateCallbacks = [...this.#updateCallbacks, new Callback(fn, "update")];
  }

  removeUpdateCallback(fn: () => unknown): void {
    for (const callback of this.#updateCallbacks) {
      if (callback.fn === fn) {
        callback.removed = true;
        callback.pending = null;
      }
    }
    this.#updateCallbacks = this.#updateCallbacks.filter((x) => !x.removed);
  }

  update(results: readonly StreamResult[]): void {
    const work: (() => void)[] = [];
    for (const { id, result } of results) {
      const state = this.#streams.get(id);
      if (state === undefined || result === undefined) {
        continue;
      }
      if (result.error !== undefined) {
        // The server has removed the stream
        this.#streams.delete(id);
        const callbacks = state.end(this.#session.errorFactory(result.error), false);
        work.push(() => state.serverEnded(callbacks));
        continue;
      }
      let value: unknown;
      try {
        value = this.#decode(state, result);
      } catch {
        // A value that fails to decode is dropped
        continue;
      }
      state.setValue(value);
      // Read with the value stored, so a callback added after this reads the value itself
      const { callbacks, version, fired } = state;
      work.push(() => state.dispatch(callbacks, version, fired, value));
    }
    const updates = ++this.#updates;
    const updateCallbacks = this.#updateCallbacks;
    // The callbacks run once the message handler returns
    queueMicrotask(() => {
      for (const fn of work) {
        fn();
      }
      for (const callback of updateCallbacks) {
        if (this.#closed !== null) {
          break;
        }
        callback.pass(updates, undefined);
      }
    });
  }

  end(error: ConnectionError, failed: boolean): void {
    // The error of a failure is the cause of the streams' error
    const ending = failed
      ? new ConnectionError("The connection is closed", { reason: error.reason, cause: error })
      : error;
    this.#closed = ending;
    const states = [...this.#streams.values()];
    this.#streams.clear();
    for (const callback of this.#updateCallbacks) {
      callback.removed = true;
    }
    const endings = states.map((x) => x.end(ending, true));
    const callEndings = () => {
      for (const callbacks of endings) {
        callEnd(callbacks, ending);
      }
    };
    // A close calls the end callbacks itself, and a failure once the message handler returns
    if (failed) {
      queueMicrotask(callEndings);
    } else {
      callEndings();
    }
  }

  #decode(state: StreamState, result: ProcedureResult): unknown {
    if (state.type === null) {
      return undefined;
    }
    if (result.isNull) {
      return null;
    }
    return decode(state.type, result.value, this.#objects);
  }

  #call(procedure: string, values: Uint8Array[]): Promise<ProcedureResult> {
    const args: Argument[] = values.map((value, position) =>
      create(ArgumentSchema, { position, value }),
    );
    return this.#session.call("KRPC", procedure, args);
  }
}

/** The id of the stream in a result, which is a STREAM or EVENT value. */
function streamId(result: ProcedureResult): bigint {
  if (result.isNull) {
    throw new EncodingError("The server returned no stream");
  }
  return decode(UINT64, result.value, NO_OBJECTS) as bigint;
}

const NO_OBJECTS: ObjectFactory = {
  object() {
    throw new EncodingError("A stream id is not an object");
  },
};

/** The state of a stream, for its event. */
let stateOf: (stream: Stream<unknown>) => StreamState;

/** A call the server repeats, sending each new value. Each add makes a new stream. A stream runs
 * until it is removed, such as at the end of an `await using` block, or its connection closes. */
export class Stream<T> {
  readonly #state: StreamState;
  readonly #manager: StreamManager;

  static {
    stateOf = (stream) => stream.#state;
  }

  /** @internal */
  constructor(state: StreamState, manager: StreamManager) {
    this.#state = state;
    this.#manager = manager;
  }

  /** The latest value. Waits for the first value. Rejects with the error that ended the
   * stream, once it has ended. */
  async get(): Promise<T> {
    const state = this.#state;
    while (!state.hasValue && state.ending === null) {
      await state.changed(Number.POSITIVE_INFINITY);
    }
    if (state.ending !== null) {
      throw state.ending;
    }
    return state.value as T;
  }

  /** The latest value. Throws a StreamError before the first value, and the error that ended
   * the stream once it has ended. */
  get value(): T {
    const state = this.#state;
    if (state.ending !== null) {
      throw state.ending;
    }
    if (!state.hasValue) {
      throw new StreamError("The stream has no value yet");
    }
    return state.value as T;
  }

  /** Waits for the stream's next value. Resolves with true on a value, and with false once the
   * timeout elapses, in milliseconds. Rejects with the error that ends the stream. */
  async wait(timeout?: number): Promise<boolean> {
    const state = this.#state;
    const deadline = performance.now() + waitTimeout(timeout);
    const version = state.version;
    for (;;) {
      if (state.ending !== null) {
        throw state.ending;
      }
      if (state.version > version) {
        return true;
      }
      const remaining = deadline - performance.now();
      if (remaining <= 0) {
        return false;
      }
      await state.changed(remaining);
    }
  }

  /** The most times a second the server sends the stream's value. Zero is no limit. */
  get rate(): number {
    return this.#state.rate;
  }

  /** Sets the most times a second the server sends the stream's value. Zero is no limit. */
  setRate(rate: number): Promise<void> {
    return this.#manager.setRate(this.#state, rate);
  }

  /** Adds a function to call with each new value. A stream that has a value calls it with that
   * value before this returns. A callback that returns a promise is not called again until the
   * promise settles, and then gets the latest value. */
  addCallback(callback: (value: T) => unknown): void {
    const state = this.#state;
    const added = state.addCallback(callback, "value");
    // No await between reading the value and the first call, so no update passes the callback
    // a newer value first
    if (state.hasValue && state.ending === null) {
      added.pass(state.version, state.value);
    }
  }

  removeCallback(callback: (value: T) => unknown): void {
    const state = this.#state;
    state.removeCallback(callback, "value");
  }

  /** Adds a function to call once when the stream ends, with the error that ended it: a
   * StreamError for a remove, the server's error, or a ConnectionError. A stream that has
   * ended calls it before this returns. */
  addEndCallback(callback: (error: Error) => unknown): void {
    const state = this.#state;
    state.addEndCallback(callback);
  }

  removeEndCallback(callback: (error: Error) => unknown): void {
    const state = this.#state;
    state.removeEndCallback(callback);
  }

  /** Removes the stream, and calls its end callbacks. Reads and waits then reject with a
   * StreamError. Removing a stream that has ended does nothing. */
  remove(): Promise<void> {
    return this.#manager.remove(this.#state);
  }

  /** Removes the stream, at the end of an `await using` block. */
  [Symbol.asyncDispose](): Promise<void> {
    return this.remove();
  }
}

/** An event, which fires each time its stream's value is true. A firing is unseen until a wait
 * returns for it, or a callback is called for it. */
export class Event {
  readonly #stream: Stream<boolean>;
  /** The firing count the last wait saw. */
  #seen = 0;

  /** @internal */
  constructor(stream: Stream<boolean>) {
    this.#stream = stream;
  }

  /** The event's stream, of a bool that is true once the event has fired. */
  get stream(): Stream<boolean> {
    return this.#stream;
  }

  /** Waits for the event to fire. Resolves at once for an unseen firing. Resolves with false
   * once the timeout elapses, in milliseconds. Rejects with the error that ends the event. */
  async wait(timeout?: number): Promise<boolean> {
    const state = stateOf(this.#stream);
    const deadline = performance.now() + waitTimeout(timeout);
    // A remove or a close discards an unseen firing
    if (state.endedByClient) {
      throw state.ending;
    }
    // Each wait returns for a firing after the last wait that returned before it started
    const seen = this.#seen;
    for (;;) {
      if (state.fired > seen) {
        this.#seen = Math.max(this.#seen, state.fired);
        return true;
      }
      if (state.ending !== null) {
        throw state.ending;
      }
      const remaining = deadline - performance.now();
      if (remaining <= 0) {
        return false;
      }
      await state.changed(remaining);
    }
  }

  /** Adds a function to call each time the event fires. For a firing that is unseen, and that
   * no callback has been called for, it is called before this returns. */
  addCallback(callback: () => unknown): void {
    const state = stateOf(this.#stream);
    const added = state.addCallback(callback, "event");
    const fired = state.fired;
    if (!state.endedByClient && fired > Math.max(this.#seen, state.callbacksFired)) {
      if (added.pass(fired, true)) {
        state.callbacksFired = fired;
      }
    }
  }

  removeCallback(callback: () => unknown): void {
    const state = stateOf(this.#stream);
    state.removeCallback(callback, "event");
  }

  /** Adds a function to call once when the event ends, with the error that ended it. An event
   * that has ended calls it before this returns. */
  addEndCallback(callback: (error: Error) => unknown): void {
    this.#stream.addEndCallback(callback);
  }

  removeEndCallback(callback: (error: Error) => unknown): void {
    this.#stream.removeEndCallback(callback);
  }

  /** Removes the event, and calls its end callbacks. */
  remove(): Promise<void> {
    return this.#stream.remove();
  }

  /** Removes the event, at the end of an `await using` block. */
  [Symbol.asyncDispose](): Promise<void> {
    return this.remove();
  }
}
