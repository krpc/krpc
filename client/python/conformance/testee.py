"""The conformance testee for the Python client.

Reads commands from the conformance driver on standard input, runs them against the client,
and writes the replies to standard output. See conformance/testee.proto for the protocol.
"""

from __future__ import annotations

import enum
import gc
import json
import keyword
import queue
import sys
import threading
import time
from collections.abc import Callable
from typing import IO, Any

import testee_pb2 as pb
from google.protobuf import json_format
from google.protobuf.message import Message

import krpc
import krpc.error
from krpc.attributes import Attributes
from krpc.client import Client
from krpc.connection import Connection
from krpc.decoder import Decoder
from krpc.encoder import Encoder
from krpc.event import Event
from krpc.service import ServiceBase
from krpc.types import ClassBase, TypeBase
from krpc.utils import snake_case

PROFILES = [
    "core",
    "error-types",
    "dynamic",
    "defaults",
    "named-arguments",
    "streams",
    "events",
    "functions",
    "connections",
    "threads",
    "callbacks",
    "conditions",
    "ordering",
    "races",
]
PROTOCOLS = ["tcpip", "localsocket"]

STREAM_COMMANDS = {
    "add_stream",
    "copy_stream",
    "drop_handle",
    "set_rate",
    "rate",
    "remove",
    "get",
    "wait",
    "add_callback",
    "add_end_callback",
    "add_update_callback",
    "remove_callback",
    "remove_end_callback",
    "remove_update_callback",
    "wait_event",
    "add_event_callback",
    "event_stream",
    "compare_handles",
    "wait_on_condition",
}


class Output:
    """Writes messages to the driver. Replies and callbacks come from several threads, so each
    message is written whole under a lock."""

    def __init__(self, stream: IO[bytes]) -> None:
        self._stream = stream
        self._lock = threading.Lock()

    def write(self, message: pb.Output) -> None:
        line = (
            json.dumps(
                json_format.MessageToDict(message), separators=(",", ":")
            ).encode()
            + b"\n"
        )
        with self._lock:
            self._stream.write(line)
            self._stream.flush()


def read_command(stream: IO[bytes]) -> pb.Command | None:
    """The next command from the driver, or None at the end of the input. Each is its JSON form
    on a line of its own."""
    line = stream.readline()
    if not line:
        return None
    return json_format.Parse(line.rstrip(b"\r\n"), pb.Command())


class Worker:
    """A named thread that runs the commands sent to it in order."""

    def __init__(self, run: Callable[[pb.Command], None]) -> None:
        self._queue: queue.Queue[pb.Command | None] = queue.Queue()
        self._run = run
        self._thread = threading.Thread(target=self._loop, daemon=True)
        self._thread.start()

    def submit(self, command: pb.Command | None) -> None:
        self._queue.put(command)

    def join(self) -> None:
        self._thread.join()

    def _loop(self) -> None:
        while True:
            command = self._queue.get()
            if command is None:
                return
            self._run(command)


class Testee:
    def __init__(self, output: Output) -> None:
        self._output = output
        self._lock = threading.Lock()
        # The connections, by the driver's name for each
        self._conns: dict[str, Client] = {}
        # The name of the connection the command a thread is running is for
        self._local = threading.local()
        self._next_handle = 1
        self._objects: dict[int, ClassBase] = {}
        self._streams: dict[int, Any] = {}
        # The function streams of a type named without "nullable"
        self._not_nullable: set[int] = set()
        # The callbacks the driver added, by its id, each with the function to remove it
        self._callbacks: dict[int, Callable[[], None]] = {}
        # The releases of blocked callbacks, by callback id
        self._releases: dict[int, threading.Semaphore] = {}
        # The number of calls of each callback, by callback id
        self._calls: dict[int, int] = {}

    def run(self, command: pb.Command) -> None:
        self._output.write(pb.Output(reply=self._reply(command)))

    def _reply(self, command: pb.Command) -> pb.Reply:
        reply = pb.Reply(id=command.id, thread=thread_name())
        self._local.connection = command.connection
        try:
            if command.condition:
                with self._stream(command.condition).condition:
                    self._dispatch(command, reply)
            else:
                self._dispatch(command, reply)
        except Fatal as exn:
            reply.ClearField("result")
            reply.error.CopyFrom(pb.Error(kind=pb.Error.FATAL, message=str(exn)))
        except Exception as exn:  # noqa: BLE001
            reply.ClearField("result")
            reply.error.CopyFrom(to_error(exn))
            if reply.error.name and not self._is_exposed(exn, reply.error):
                reply.error.ClearField("service")
                reply.error.ClearField("name")
        return reply

    def _held(self) -> list[threading.Condition]:
        """The conditions the thread holds, in the order it took them."""
        if not hasattr(self._local, "held"):
            self._local.held = []
        return self._local.held

    def _is_exposed(self, exn: Exception, error: pb.Error) -> bool:
        """Whether an error is an instance of the type the client exposes for it."""
        try:
            return isinstance(exn, self._type(error.service, error.name))
        except (AttributeError, ValueError):
            return False

    def close(self) -> None:
        for conn in self._conns.values():
            conn.close()

    def _close(self, command: pb.Close) -> None:
        if command.interrupted:
            raise Unsupported("A Python thread cannot be interrupted")
        self._connection().close()

    def _dispatch(self, command: pb.Command, reply: pb.Reply) -> None:
        case = command.WhichOneof("command")
        if case == "connect":
            self._connect(command.connect)
            reply.value.SetInParent()
        elif case == "close":
            self._close(command.close)
            reply.value.SetInParent()
        elif case == "invoke":
            func, args = self._resolve(command.invoke)
            kwargs = {
                snake_case(x.name): self._from_value(x.value)
                for x in command.invoke.named_arguments
            }
            reply.value.CopyFrom(self._to_value(func(*args, **kwargs)))
        elif case in STREAM_COMMANDS:
            self._dispatch_stream(case, command, reply)
        elif case == "call":
            func, args = self._resolve(command.call.invoke)
            conn = self._connection()
            function = conn.krpc.Expression.call(conn.get_call(func, *args))
            reply.value.CopyFrom(self._to_value(function))
        elif case == "add_function_stream":
            reply.stream = self._add_function_stream(command.add_function_stream)
        elif case == "add_function_event":
            function = self._from_value(command.add_function_event.function)
            event = self._connection().add_event(function)
            reply.value.CopyFrom(self._to_value(event))
        elif case == "run_function":
            reply.value.CopyFrom(self._run_function(command.run_function))
        elif case == "hash":
            reply.value.uint64 = hash(self._from_value(command.hash.a)) % 2**64
        elif case == "hash_handle":
            reply.value.uint64 = hash(self._stream(command.hash_handle.stream)) % 2**64
        elif case == "object_id":
            obj = self._from_value(command.object_id.a)
            reply.value.uint64 = krpc.get_object_id(obj)
        elif case == "members":
            names = self._members(command.members)
            reply.value.list.items.extend(pb.Value(string=x) for x in sorted(names))
        elif case == "encode":
            typ = self._codec_type(command.encode.type)
            reply.value.bytes = Encoder.encode(
                self._from_value(command.encode.value), typ
            )
        elif case == "decode":
            typ = self._codec_type(command.decode.type)
            value = Decoder.decode(self._connection(), command.decode.data, typ)
            reply.value.CopyFrom(self._to_value(value))
        elif case == "release":
            self._release(command.release.callback).release()
            reply.value.SetInParent()
        elif case == "interrupt":
            raise Unsupported("A Python thread cannot be interrupted")
        elif case in ("pause", "await_pause", "resume", "fail_write"):
            self._fault(case, command)
            reply.value.SetInParent()
        elif case in ("compare", "order"):
            self._compare(case, command, reply)
        elif case in (
            "hold_condition",
            "release_condition",
            "notify_condition",
            "wait_for_update",
        ):
            self._dispatch_condition(case, command)
            reply.value.SetInParent()
        else:
            raise ValueError("Unknown command %s" % case)

    def _compare(self, case: str, command: pb.Command, reply: pb.Reply) -> None:
        """Compare two values with the language's equality, or order them."""
        operands = getattr(command, case)
        a = self._from_value(operands.a)
        b = self._from_value(operands.b)
        if case == "compare":
            reply.value.bool = a == b
        else:
            sign = (a > b) - (a < b)
            operators = [a <= b, a >= b, a == b, a != b]
            if operators != [sign <= 0, sign >= 0, sign == 0, sign != 0]:
                raise ValueError("The comparison operators disagree on the order")
            reply.value.sint32 = sign

    def _dispatch_condition(self, case: str, command: pb.Command) -> None:
        conn = self._connection()
        if case == "hold_condition":
            handle = command.hold_condition.stream
            if handle:
                condition = self._stream(handle).condition
            else:
                condition = conn.stream_update_condition
            condition.acquire()
            self._held().append(condition)
        elif case == "release_condition":
            condition = self._held().pop()
            handle = command.release_condition.stream
            (self._stream(handle).condition if handle else condition).release()
        elif case == "notify_condition":
            condition = self._stream(command.notify_condition.stream).condition
            with condition:
                condition.notify_all()
        else:
            wait = command.wait_for_update
            timeout = wait.timeout if wait.HasField("timeout") else None
            with conn.stream_update_condition:
                conn.wait_for_stream_update(timeout)

    def _dispatch_stream(self, case: str, command: pb.Command, reply: pb.Reply) -> None:
        if case == "add_stream":
            func, args = self._resolve(command.add_stream.invoke)
            if command.add_stream.scoped:
                with self._connection().stream(func, *args) as stream:
                    stream()
            else:
                stream = self._connection().add_stream(func, *args)
            reply.stream = self._add_handle(self._streams, stream)
        elif case == "copy_stream":
            stream = self._stream(command.copy_stream.stream)
            reply.stream = self._add_handle(self._streams, stream)
        elif case == "drop_handle":
            self._drop_handle(command.drop_handle.stream)
            reply.value.SetInParent()
        elif case == "wait_on_condition":
            wait = command.wait_on_condition
            reply.value.bool = self._stream(wait.stream).condition.wait(wait.timeout)
        elif case == "compare_handles":
            a = self._stream(command.compare_handles.a)
            reply.value.bool = a == self._stream(command.compare_handles.b)
        elif case == "set_rate":
            self._stream(command.set_rate.stream).rate = command.set_rate.rate
            reply.value.SetInParent()
        elif case == "rate":
            reply.value.float = self._stream(command.rate.stream).rate
        elif case == "remove":
            self._stream(command.remove.stream).remove()
            reply.value.SetInParent()
        elif case == "get":
            reply.value.CopyFrom(self._get(command.get.stream))
        elif case == "wait":
            stream = self._stream(command.wait.stream)
            timeout = command.wait.timeout if command.wait.HasField("timeout") else None
            with stream.condition:
                reply.value.bool = stream.wait(timeout)
        elif case == "add_callback":
            self._add_callback(command.add_callback)
            reply.value.SetInParent()
        elif case == "add_end_callback":
            self._add_end_callback(command.add_end_callback)
            reply.value.SetInParent()
        elif case == "add_update_callback":
            self._add_update_callback(command.add_update_callback)
            reply.value.SetInParent()
        elif case == "wait_event":
            event = self._stream(command.wait_event.event)
            timeout = (
                command.wait_event.timeout
                if command.wait_event.HasField("timeout")
                else None
            )
            with event.condition:
                reply.value.bool = event.wait(timeout)
        elif case == "add_event_callback":
            self._add_event_callback(command.add_event_callback)
            reply.value.SetInParent()
        elif case == "event_stream":
            stream = self._stream(command.event_stream.event).stream
            reply.stream = self._add_handle(self._streams, stream)
        else:
            with self._lock:
                remove = self._callbacks.pop(getattr(command, case).callback)
            remove()
            reply.value.SetInParent()

    def _connect(self, command: pb.Connect) -> None:
        name = self._local.connection
        with self._lock:
            if name:
                closed = [self._conns.pop(name)] if name in self._conns else []
            else:
                closed = list(self._conns.values())
                self._conns.clear()
                self._objects.clear()
                self._streams.clear()
                self._callbacks.clear()
                self._releases.clear()
        for conn in closed:
            conn.close()
        kwargs: dict[str, Any] = {"name": command.name or None}
        if command.HasField("handshake_timeout"):
            kwargs["handshake_timeout"] = command.handshake_timeout
        if command.HasField("stall_timeout"):
            kwargs["stall_timeout"] = command.stall_timeout
        if command.protocol == "localsocket":
            conn = krpc.connect_local(path=command.path, **kwargs)
        elif command.protocol == "tcpip":
            if command.address:
                kwargs["address"] = command.address
            if command.port:
                kwargs["port"] = command.port
            conn = krpc.connect(**kwargs)
        else:
            raise ValueError("Unknown protocol %s" % command.protocol)
        with self._lock:
            self._conns[name] = conn

    def _connection(self) -> Client:
        name = getattr(self._local, "connection", "")
        with self._lock:
            if name not in self._conns:
                raise ValueError("Not connected")
            return self._conns[name]

    def _fault(self, case: str, command: pb.Command) -> None:
        """Arm a write fault, which fails the next write to a connection with an OSError,
        as a failed socket write does. Both of the connection class's writes are replaced
        for the one write, which covers a connection not yet made. The client has no race
        points."""
        if case != "fail_write":
            raise RuntimeError("The client has no race points")
        message = command.fail_write.message
        writes = (Connection.send_request, Connection.send_message)

        def fail(*_: object) -> None:
            Connection.send_request, Connection.send_message = writes
            raise OSError(message)

        Connection.send_request = fail  # pyright: ignore[reportAttributeAccessIssue]
        Connection.send_message = fail  # pyright: ignore[reportAttributeAccessIssue]

    def _resolve(self, command: pb.Invoke) -> tuple[Callable[..., Any], list[Any]]:
        """The callable for a procedure, and the arguments to call it with. Getters and
        setters resolve to getattr and setattr, the form add_stream takes for a property.
        """
        name = command.procedure
        args = [self._from_value(x) for x in command.arguments]
        if Attributes.is_a_class_member(name):
            member = Attributes.get_class_member_name(name)
            if Attributes.is_a_class_static_method(name):
                if command.HasField("through"):
                    through = self._from_value(command.through)
                    return getattr(through, member_name(member)), args
                service = getattr(self._connection(), snake_case(command.service))
                cls = getattr(service, Attributes.get_class_name(name))
                return getattr(cls, member_name(member)), args
            if Attributes.is_a_class_property_getter(name):
                return getattr, [args[0], snake_case(member)]
            if Attributes.is_a_class_property_setter(name):
                return setattr, [args[0], snake_case(member), args[1]]
            return getattr(args[0], member_name(member)), args[1:]
        service = getattr(self._connection(), snake_case(command.service))
        if Attributes.is_a_property_getter(name):
            return getattr, [service, snake_case(Attributes.get_property_name(name))]
        if Attributes.is_a_property_setter(name):
            prop = snake_case(Attributes.get_property_name(name))
            return setattr, [service, prop, args[0]]
        return getattr(service, member_name(name)), args

    def _call_back(
        self,
        callback: int,
        behavior: pb.Behavior,
        call: Callable[[pb.Notification], None] | None = None,
    ) -> None:
        """Write the notification for a call of a callback, then do what its behavior says."""
        notification = pb.Notification(callback=callback, thread=thread_name())
        if call is not None:
            call(notification)
        self._output.write(pb.Output(notification=notification))
        with self._lock:
            calls = self._calls[callback] = self._calls.get(callback, 0) + 1
        if calls <= behavior.skip or 0 < behavior.only < calls:
            return
        if behavior.sleep:
            time.sleep(behavior.sleep)
        action = behavior.WhichOneof("action")
        try:
            if action == "block":
                self._release(callback).acquire()
            elif action == "run":
                result = self._reply(behavior.run)
                notification = pb.Notification(
                    callback=callback, thread=thread_name(), result=result
                )
                self._output.write(pb.Output(notification=notification))
            elif action == "fail":
                raise RuntimeError(behavior.fail)
            elif action == "fail_fatally":
                raise Fatal(behavior.fail_fatally)
        finally:
            if behavior.report_return:
                notification = pb.Notification(
                    callback=callback, thread=thread_name(), returned=True
                )
                self._output.write(pb.Output(notification=notification))

    def _release(self, callback: int) -> threading.Semaphore:
        with self._lock:
            if callback not in self._releases:
                self._releases[callback] = threading.Semaphore(0)
            return self._releases[callback]

    def _add_callback(self, command: pb.AddCallback) -> None:
        callback = command.callback

        def notify(value: object) -> None:
            def call(notification: pb.Notification) -> None:
                notification.value.CopyFrom(self._to_value(value))

            self._call_back(callback, command.behavior, call)

        stream = self._stream(command.stream)
        stream.add_callback(notify)
        with self._lock:
            self._callbacks[callback] = lambda: stream.remove_callback(notify)

    def _add_end_callback(self, command: pb.AddEndCallback) -> None:
        callback = command.callback

        def notify(error: Exception | None) -> None:
            def call(notification: pb.Notification) -> None:
                notification.ending.SetInParent()
                if error is not None:
                    notification.ending.error.CopyFrom(to_error(error))

            self._call_back(callback, command.behavior, call)

        stream = self._stream(command.stream)
        stream.add_end_callback(notify)
        with self._lock:
            self._callbacks[callback] = lambda: stream.remove_end_callback(notify)

    def _add_event_callback(self, command: pb.AddEventCallback) -> None:
        callback = command.callback

        def notify() -> None:
            self._call_back(callback, command.behavior)

        event = self._stream(command.event)
        event.add_callback(notify)
        with self._lock:
            self._callbacks[callback] = lambda: event.remove_callback(notify)

    def _add_update_callback(self, command: pb.AddUpdateCallback) -> None:
        callback = command.callback

        def notify() -> None:
            self._call_back(callback, command.behavior)

        conn = self._connection()
        conn.add_stream_update_callback(notify)
        with self._lock:
            self._callbacks[callback] = lambda: conn.remove_stream_update_callback(
                notify
            )

    def _add_function_stream(self, command: pb.AddFunctionStream) -> int:
        stream = self._connection().add_stream(self._from_value(command.function))
        handle = self._add_handle(self._streams, stream)
        if not command.type.startswith("nullable "):
            self._not_nullable.add(handle)
        return handle

    def _get(self, handle: int) -> pb.Value:
        value = self._stream(handle)()
        if value is None and handle in self._not_nullable:
            raise Unsupported("A Python value can be None")
        return self._to_value(value)

    def _run_function(self, command: pb.RunFunction) -> pb.Value:
        function = self._from_value(command.function)
        args = [self._from_value(x) for x in command.arguments]
        value = self._connection().run(function, *args)
        if value is None and command.type and not command.type.startswith("nullable "):
            raise Unsupported("A Python value can be None")
        return self._to_value(value)

    def _drop_handle(self, handle: int) -> None:
        with self._lock:
            del self._streams[handle]
        gc.collect()

    def _stream(self, handle: int) -> Any:
        with self._lock:
            return self._streams[handle]

    def _add_handle(self, table: dict[int, Any], value: object) -> int:
        with self._lock:
            handle = self._next_handle
            self._next_handle += 1
            table[handle] = value
            return handle

    def _from_value(self, value: pb.Value) -> Any:
        case = value.WhichOneof("value")
        if case is None or case == "null":
            return None
        if case == "object":
            if value.object.handle == 0:
                cls = self._type(value.object.service, value.object.name)
                return cls(self._connection(), value.object.id)
            with self._lock:
                return self._objects[value.object.handle]
        if case == "enum":
            return self._type(value.enum.service, value.enum.name)(value.enum.value)
        if case == "list":
            return [self._from_value(x) for x in value.list.items]
        if case == "set":
            return {self._from_value(x) for x in value.set.items}
        if case == "tuple":
            return tuple(self._from_value(x) for x in value.tuple.items)
        if case == "dictionary":
            return {
                self._from_value(x.key): self._from_value(x.value)
                for x in value.dictionary.entries
            }
        if case == "struct":
            fields = [self._from_value(x) for x in value.struct.fields]
            return self._type(value.struct.service, value.struct.name)(*fields)
        return getattr(value, case)

    def _members(self, command: pb.Members) -> list[str]:
        conn = self._connection()
        if not command.service:
            return [
                x
                for x in dir(conn)
                if not x.startswith("_") and is_service(getattr(conn, x))
            ]
        obj = getattr(conn, snake_case(command.service))
        if command.name:
            obj = getattr(obj, command.name)
            if isinstance(obj, type) and issubclass(obj, enum.Enum):
                return [x.name for x in obj]
        return [x for x in dir(obj) if not x.startswith("_")]

    def _codec_type(self, name: str) -> TypeBase:
        """The client's type for a codec type name, such as "list(nullable uint32)"."""
        types = self._connection()._types
        if name.startswith("nullable "):
            return types.nullable(self._codec_type(name[len("nullable ") :]))
        if "(" in name:
            kind, _, rest = name.partition("(")
            items = [self._codec_type(x) for x in split_codec_types(rest[:-1])]
            if kind == "list":
                return types.list_type(items[0])
            if kind == "set":
                return types.set_type(items[0])
            if kind == "dictionary":
                return types.dictionary_type(items[0], items[1])
            return types.tuple_type(*items)
        if name == "TestClass":
            return types.class_type("TestService", name)
        if name == "TestEnum":
            return types.enumeration_type("TestService", name)
        if name.startswith("Test"):
            return types.struct_type("TestService", name)
        return getattr(types, name + "_type")

    def _type(self, service: str, name: str) -> Any:
        """An enum, struct, class or exception type of a service."""
        return getattr(getattr(self._connection(), snake_case(service)), name)

    def _to_value(self, value: object) -> pb.Value:
        result = pb.Value()
        if value is None:
            result.null = True
        elif isinstance(value, bool):
            result.bool = value
        elif isinstance(value, int):
            # The driver compares integers by value, whatever their type
            if value < 0:
                result.sint64 = value
            else:
                result.uint64 = value
        elif isinstance(value, float):
            result.double = value
        elif isinstance(value, str):
            result.string = value
        elif isinstance(value, bytes):
            result.bytes = value
        elif isinstance(value, Message):
            result.message = value.SerializeToString()
        elif isinstance(value, Event):
            # Events share the table of streams, as both end and are removed alike
            result.event = self._add_handle(self._streams, value)
        elif isinstance(value, ClassBase):
            result.object.handle = self._add_handle(self._objects, value)
            result.object.id = krpc.get_object_id(value)
        elif isinstance(value, enum.Enum):
            result.enum.name = type(value).__name__
            result.enum.value = value.value
        elif isinstance(value, tuple) and hasattr(value, "_fields"):
            # A struct is a named tuple
            result.struct.name = type(value).__name__
            result.struct.fields.extend(self._to_value(x) for x in value)
        elif isinstance(value, tuple):
            result.tuple.items.extend(self._to_value(x) for x in value)
        elif isinstance(value, list):
            result.list.items.extend(self._to_value(x) for x in value)
        elif isinstance(value, set):
            result.set.items.extend(self._to_value(x) for x in value)
        elif isinstance(value, dict):
            result.dictionary.SetInParent()
            for k, v in value.items():
                entry = result.dictionary.entries.add()
                entry.key.CopyFrom(self._to_value(k))
                entry.value.CopyFrom(self._to_value(v))
        else:
            raise TypeError("No conformance value for %s" % type(value))
        return result


def split_codec_types(names: str) -> list[str]:
    """The comma separated codec type names in a list of them, which may nest."""
    result = []
    depth = 0
    start = 0
    for i, char in enumerate(names):
        if char == "(":
            depth += 1
        elif char == ")":
            depth -= 1
        elif char == "," and depth == 0:
            result.append(names[start:i])
            start = i + 1
    result.append(names[start:])
    return result


def member_name(name: str) -> str:
    """The name the client gives a procedure or method, with an underscore after a keyword."""
    name = snake_case(name)
    return name + "_" if keyword.iskeyword(name) else name


def is_service(value: object) -> bool:
    """Whether a value is a service: one built from the server's definitions, or a
    pre-generated stub."""
    if isinstance(value, type) and issubclass(value, ServiceBase):
        return True
    return type(value).__module__.startswith("krpc.services.")


class Unsupported(Exception):
    """The client offers no way to do what a command asks."""


class Fatal(BaseException):
    """The error a callback raises for its fail_fatally behavior."""


def thread_name() -> str:
    return str(threading.get_ident())


def to_error(exn: Exception) -> pb.Error:
    # A KeyError's text is the repr of its message
    if isinstance(exn, KeyError) and exn.args:
        message = str(exn.args[0])
    else:
        message = str(exn)
    error = pb.Error(message=message)
    if exn.__cause__ is not None:
        error.cause = f"{type(exn.__cause__).__name__}: {exn.__cause__}"
    error.native_types.extend(
        cls.__name__
        for cls in type(exn).__mro__
        if cls.__module__ == "builtins" and cls not in (object, BaseException)
    )
    service = getattr(type(exn), "_service_name", None)
    if isinstance(exn, Unsupported):
        error.kind = pb.Error.UNSUPPORTED
    elif service is not None:
        error.kind = pb.Error.RPC
        error.service = service
        error.name = type(exn).__name__
    elif isinstance(exn, krpc.error.ConnectionError):
        error.kind = pb.Error.CONNECTION
        error.status = exn.status or ""
        error.reason = exn.reason or ""
    elif isinstance(exn, krpc.error.RPCError):
        error.kind = pb.Error.RPC
    elif isinstance(exn, krpc.error.StreamError):
        error.kind = pb.Error.STREAM
    elif isinstance(exn, krpc.error.EncodingError):
        error.kind = pb.Error.ENCODING
    elif isinstance(exn, (TypeError, ValueError)):
        error.kind = pb.Error.ARGUMENT
    return error


def main() -> int:
    # The protocol owns standard output, so anything else the process prints goes to standard
    # error
    stdout = sys.stdout.buffer
    sys.stdout = sys.stderr
    output = Output(stdout)
    output.write(
        pb.Output(
            hello=pb.Hello(
                client="python",
                profiles=PROFILES,
                protocols=PROTOCOLS,
            )
        )
    )

    testee = Testee(output)
    workers: dict[str, Worker] = {}
    while True:
        command = read_command(sys.stdin.buffer)
        if command is None:
            break
        if command.thread not in workers:
            workers[command.thread] = Worker(testee.run)
        workers[command.thread].submit(command)
    for worker in workers.values():
        worker.submit(None)
        worker.join()
    testee.close()
    return 0


if __name__ == "__main__":
    sys.exit(main())
