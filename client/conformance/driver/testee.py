"""A testee process, and the client under test it drives."""

from __future__ import annotations

import contextlib
import json
import math
import subprocess
import threading
import time
from collections import defaultdict
from collections.abc import Callable, Iterator
from dataclasses import dataclass
from typing import Any, TypeVar, cast

import testee_pb2 as pb
from google.protobuf import json_format
from google.protobuf.descriptor import FieldDescriptor

from driver import values
from driver.conventions import Conventions

# The time a testee has to write its Hello, which includes starting a runtime such as .NET
HELLO_TIMEOUT = 60

# The time a command has to reply. A command that waits passes its own deadline.
REPLY_TIMEOUT = 10

# The time a test gives a command it started to reach the point where it waits
SETTLE = 0.2

# The longest a return the requirements call at once can take
AT_ONCE = 1

# The timeout of a wait a test expects to time out
SHORT_WAIT = 0.2


class TesteeFailed(Exception):
    """The testee exited, or missed the deadline for a reply."""

    # A class pytest leaves uncollected, though its name starts with Test
    __test__ = False


class TesteeError(Exception):
    """The command raised an error in the client."""

    __test__ = False

    def __init__(self, error: pb.Error) -> None:
        super().__init__(
            "%s error: %s" % (pb.Error.Kind.Name(error.kind), error.message)
        )
        self.error = error


class Unsupported(Exception):
    """The client offers no way to carry out the command. The test is skipped if the client
    lists it as unsupported, and fails otherwise."""


class Testee:
    """A running testee. Replies are matched to commands by id, and notifications collected
    per callback, on a thread that reads the testee's output."""

    __test__ = False

    def __init__(self, command: list[str], env: dict[str, str]) -> None:
        # The testee's standard error goes to the test log
        self._process = subprocess.Popen(
            command, stdin=subprocess.PIPE, stdout=subprocess.PIPE, env=env
        )
        self._condition = threading.Condition()
        self._hello: pb.Hello | None = None
        self._replies: dict[int, pb.Reply] = {}
        self._notifications: dict[int, list[pb.Notification]] = defaultdict(list)
        # The callback of each call, in the order the testee wrote the calls
        self._calls: list[int] = []
        self._ended = False
        # Why the testee's output ended early, for a line that is not a message
        self._ended_reason = "The testee exited"
        self._next_id = 1
        self._next_callback = 1
        self._write_lock = threading.Lock()
        self._reader = threading.Thread(target=self._read, daemon=True)
        self._reader.start()
        with self._condition:
            self._condition.wait_for(
                lambda: self._hello is not None or self._ended, HELLO_TIMEOUT
            )
            hello = self._hello
            ended = self._ended
        if hello is None:
            self.stop()
            if not ended:
                raise TesteeFailed("The testee wrote no Hello")
            raise TesteeFailed(self._ended_reason + " before writing a Hello")
        self.hello: pb.Hello = hello

    @property
    def running(self) -> bool:
        return not self._ended and self._process.poll() is None

    def stop(self) -> None:
        if self._process.poll() is None:
            assert self._process.stdin is not None
            try:
                self._process.stdin.close()
                self._process.wait(5)
            except (OSError, subprocess.TimeoutExpired):
                self._process.kill()
                self._process.wait()
        self._reader.join(5)

    def send(
        self, thread: str = "", condition: int = 0, connection: str = "", **command: Any
    ) -> int:
        """Send a command, returning its id."""
        with self._write_lock:
            command_id = self._next_id
            self._next_id += 1
            message = pb.Command(
                id=command_id,
                thread=thread,
                condition=condition,
                connection=connection,
                **command,
            )
            assert self._process.stdin is not None
            try:
                self._process.stdin.write(encode_line(message))
                self._process.stdin.flush()
            except OSError as exn:
                raise TesteeFailed("The testee exited") from exn
        return command_id

    def has_reply(self, command_id: int) -> bool:
        with self._condition:
            return command_id in self._replies

    def reply(self, command_id: int, timeout: float = REPLY_TIMEOUT) -> pb.Reply:
        """The reply to a command. Raises the error it reports as a TesteeError. A testee that
        misses the deadline is stopped, so the next test starts a new one."""
        with self._condition:
            self._condition.wait_for(
                lambda: command_id in self._replies or self._ended, timeout
            )
            reply = self._replies.pop(command_id, None)
        if reply is None:
            ended = self._ended
            self._process.kill()
            if ended:
                raise TesteeFailed(self._ended_reason)
            raise TesteeFailed("The testee did not reply within %g seconds" % timeout)
        if reply.WhichOneof("result") == "error":
            if reply.error.kind == pb.Error.UNSUPPORTED:
                raise Unsupported("%s: %s" % (self.hello.client, reply.error.message))
            raise TesteeError(reply.error)
        return reply

    def run(
        self,
        timeout: float = REPLY_TIMEOUT,
        thread: str = "",
        connection: str = "",
        **command: Any,
    ) -> pb.Reply:
        return self.reply(self.send(thread, connection=connection, **command), timeout)

    def new_callback(self) -> int:
        """An id for a callback, unique for the testee's lifetime, as its notifications are
        kept that long."""
        with self._condition:
            callback = self._next_callback
            self._next_callback += 1
            return callback

    def notifications(self, callback: int) -> list[pb.Notification]:
        with self._condition:
            return list(self._notifications[callback])

    def call_order(self, *callbacks: int) -> list[int]:
        """The callback of each call of the given callbacks, in the order of the calls."""
        with self._condition:
            return [x for x in self._calls if x in callbacks]

    def wait_for_notifications(
        self, callback: int, count: int, timeout: float = REPLY_TIMEOUT
    ) -> list[pb.Notification]:
        """The notifications of a callback, once there are at least count of them."""
        with self._condition:
            self._condition.wait_for(
                lambda: len(self._notifications[callback]) >= count or self._ended,
                timeout,
            )
            return list(self._notifications[callback])

    def _read(self) -> None:
        assert self._process.stdout is not None
        for line in self._process.stdout:
            try:
                output = decode_line(line)
            except (ValueError, json_format.ParseError):
                self._ended_reason = (
                    "The testee wrote a line that is not a message: %r" % line[:80]
                )
                break
            with self._condition:
                case = output.WhichOneof("content")
                if case == "hello":
                    self._hello = output.hello
                elif case == "reply":
                    self._replies[output.reply.id] = output.reply
                elif case == "notification":
                    notification = output.notification
                    self._notifications[notification.callback].append(notification)
                    if _is_call(notification):
                        self._calls.append(notification.callback)
                self._condition.notify_all()
        with self._condition:
            self._ended = True
            self._condition.notify_all()


def decode_line(line: bytes) -> pb.Output:
    """The message on a line, which may end with a carriage return written in text mode."""
    value = json.loads(line, parse_int=_parse_int)
    _round_to_float_max(pb.Output.DESCRIPTOR, value)
    return json_format.ParseDict(value, pb.Output())


def _parse_int(text: str) -> int | float:
    """A JSON integer. Negative zero, which a testee writes for a negative zero double, stays
    negative zero."""
    return -0.0 if text == "-0" else int(text)


# Works around protocolbuffers/protobuf#31051: json_format rejects 3.4028235e+38 for a float
# field. Remove once requirements_lock.txt has a protobuf release with the fix.
def _round_to_float_max(descriptor: Any, value: dict[str, Any]) -> None:
    """Replace a float that rounds to the largest float in single precision with that float.
    Shortest forms, such as 3.4028235e+38, exceed it in double precision, and the parser
    rejects them."""
    for name, item in value.items():
        field = descriptor.fields_by_camelcase_name.get(
            name
        ) or descriptor.fields_by_name.get(name)
        if field is None:
            continue
        items = item if isinstance(item, list) else [item]
        for i, x in enumerate(items):
            if field.message_type is not None and isinstance(x, dict):
                _round_to_float_max(field.message_type, x)
            elif (
                field.type == FieldDescriptor.TYPE_FLOAT
                and isinstance(x, float)
                and values.FLOAT_MAX < abs(x) <= values.FLOAT_MAX * (1 + 2**-24)
            ):
                items[i] = math.copysign(values.FLOAT_MAX, x)
        if not isinstance(item, list):
            value[name] = items[0]


def encode_line(message: pb.Command) -> bytes:
    """A message as the testee protocol frames it: its JSON form on a line of its own."""
    value = cast("dict[str, Any]", json_format.MessageToDict(message))
    _round_to_float_max(pb.Command.DESCRIPTOR, value)
    return json.dumps(value, separators=(",", ":")).encode() + b"\n"


@dataclass
class Endpoint:
    """Where a server listens."""

    protocol: str
    address: str = ""
    port: int = 0
    path: str = ""
    # The TCP/IP port of the control connection, for a server that serves one
    control_port: int = 0


class Client:
    """The client under test, driven through its testee. Calls return plain Python values, and
    raise a TesteeError for an error the client raises."""

    def __init__(self, testee: Testee, conventions: Conventions) -> None:
        self.testee = testee
        self.name = testee.hello.client
        self.conventions = conventions

    def has_profile(self, profile: str) -> bool:
        """Whether the client implements the profile."""
        return profile in self.testee.hello.profiles

    def connect(
        self,
        endpoint: Endpoint,
        name: str = "",
        handshake_timeout: float | None = None,
        connection: str = "",
        stall_timeout: float | None = None,
    ) -> None:
        """Connect. A named connection is opened beside the default one."""
        command = pb.Connect(
            protocol=endpoint.protocol,
            address=endpoint.address,
            port=endpoint.port,
            path=endpoint.path,
            name=name,
        )
        if handshake_timeout is not None:
            command.handshake_timeout = handshake_timeout
        if stall_timeout is not None:
            command.stall_timeout = stall_timeout
        self.testee.run(connect=command, connection=connection)

    def close(self) -> None:
        self.testee.run(close=pb.Close())

    def close_quietly(self) -> None:
        """Close a connection the test may have ended, which can raise."""
        try:
            self.close()
        except TesteeError:
            pass

    def close_interrupted(self) -> bool:
        """Close from a thread with an interrupt pending. Returns whether the interrupt is
        still pending after the close."""
        reply = self.testee.run(close=pb.Close(interrupted=True))
        return bool(values.native(reply.value))

    def invoke(
        self,
        service: str,
        procedure: str,
        *args: pb.Value,
        named: dict[str, pb.Value] | None = None,
        connection: str = "",
        through: values.Object | None = None,
    ) -> Any:
        """Call a procedure. Arguments in named are passed by the server's parameter name.
        A static method is called through the object in through, if one is given."""
        invoke = _invoke(service, procedure, args, named)
        if through is not None:
            invoke.through.CopyFrom(values.obj(through))
        reply = self.testee.run(invoke=invoke, connection=connection)
        return values.native(reply.value)

    def add_stream(
        self, service: str, procedure: str, *args: pb.Value, scoped: bool = False
    ) -> Stream:
        """Add a stream. A scoped stream is added with the language's scope idiom, read once,
        and removed by leaving the scope."""
        reply = self.testee.run(
            add_stream=pb.AddStream(
                invoke=_invoke(service, procedure, args), scoped=scoped
            )
        )
        return Stream(self, reply.stream)

    def event(self, service: str, procedure: str, *args: pb.Value) -> Event:
        """Call a procedure that returns an event."""
        result = self.invoke(service, procedure, *args)
        assert isinstance(result, values.EventHandle), result
        return Event(self, result.handle)

    def call_function(
        self, service: str, procedure: str, *args: pb.Value
    ) -> values.Object:
        """A server side function that makes a call, built over the call the client builds
        for its stream."""
        reply = self.testee.run(call=pb.Call(invoke=_invoke(service, procedure, args)))
        result = values.native(reply.value)
        assert isinstance(result, values.Object), result
        return result

    def add_function_stream(self, function: values.Object, typ: str) -> Stream:
        """Add a stream of a server side function whose value has the given type."""
        reply = self.testee.run(
            add_function_stream=pb.AddFunctionStream(
                function=values.obj(function), type=typ
            )
        )
        return Stream(self, reply.stream)

    def add_function_event(self, function: values.Object) -> Event:
        """Add an event over a server side function."""
        reply = self.testee.run(
            add_function_event=pb.AddFunctionEvent(function=values.obj(function))
        )
        result = values.native(reply.value)
        assert isinstance(result, values.EventHandle), result
        return Event(self, result.handle)

    def run_function(
        self, function: values.Object, typ: str, *args: tuple[str, pb.Value]
    ) -> Any:
        """Run a server side function whose value has the given type, or no value when the
        type is empty. Each argument is a type and a value."""
        reply = self.testee.run(
            run_function=pb.RunFunction(
                function=values.obj(function),
                type=typ,
                arguments=[x for _, x in args],
                argument_types=[t for t, _ in args],
            )
        )
        return values.native(reply.value)

    def add_update_callback(self, behavior: pb.Behavior | None = None) -> Callback:
        """Add a callback the connection calls once per stream update."""
        callback = self.new_callback()
        self.testee.run(
            add_update_callback=pb.AddUpdateCallback(
                callback=callback, behavior=behavior
            )
        )
        return Callback(self.testee, callback, "remove_update_callback")

    def compare(self, a: values.Object | pb.Value, b: values.Object | pb.Value) -> bool:
        """The language's equality of two values, each a returned object or a Value."""
        reply = self.testee.run(compare=pb.Compare(a=_value(a), b=_value(b)))
        return bool(values.native(reply.value))

    def order(self, a: values.Object | pb.Value, b: values.Object | pb.Value) -> int:
        """The sign of the language's ordering of two values, each a returned object or a
        Value."""
        reply = self.testee.run(order=pb.Order(a=_value(a), b=_value(b)))
        result = values.native(reply.value)
        assert isinstance(result, int)
        return (result > 0) - (result < 0)

    def hash(self, a: values.Object | pb.Value) -> int:
        """The language's hash of a value. Raises Unsupported where values have no hash."""
        reply = self.testee.run(hash=pb.Hash(a=_value(a)))
        result = values.native(reply.value)
        if result is None:
            raise Unsupported("%s: The language has no hash of a value" % self.name)
        assert isinstance(result, int)
        return result

    def compare_handles(self, a: Stream | Event, b: Stream | Event) -> bool:
        """The language's equality of two stream handles, or two event handles."""
        reply = self.testee.run(
            compare_handles=pb.CompareHandles(a=a.handle, b=b.handle)
        )
        return bool(values.native(reply.value))

    def hash_handle(self, a: Stream | Event) -> int:
        """The language's hash of a stream handle or an event handle. Raises Unsupported where
        handles have no hash."""
        reply = self.testee.run(hash_handle=pb.HashHandle(stream=a.handle))
        result = values.native(reply.value)
        if result is None:
            raise Unsupported("%s: The language has no hash of a handle" % self.name)
        assert isinstance(result, int)
        return result

    def notify_condition(self, handle: int) -> None:
        """Notify the threads waiting on the condition of a stream or an event, with no
        update."""
        self.testee.run(notify_condition=pb.NotifyCondition(stream=handle))

    def object_id(self, a: pb.Value) -> int:
        """The server's id for an object, from the client's helper."""
        reply = self.testee.run(object_id=pb.ObjectId(a=a))
        result = values.native(reply.value)
        assert isinstance(result, int)
        return result

    def members(self, service: str = "", name: str = "") -> set[str] | None:
        """The names the client exposes for a service, class or enum, or None where the testee
        cannot list them."""
        reply = self.testee.run(members=pb.Members(service=service, name=name))
        result = values.native(reply.value)
        if result is None:
            return None
        assert isinstance(result, list)
        return set(result)

    def encode(self, typ: str, value: pb.Value) -> bytes:
        """A value encoded with the client's encoder."""
        reply = self.testee.run(encode=pb.Encode(type=typ, value=value))
        result = values.native(reply.value)
        assert isinstance(result, bytes), result
        return result

    def decode(self, typ: str, data: bytes) -> Any:
        """Bytes decoded with the client's decoder."""
        reply = self.testee.run(decode=pb.Decode(type=typ, data=data))
        return values.native(reply.value)

    def new_callback(self) -> int:
        return self.testee.new_callback()

    @contextlib.contextmanager
    def holding(self, handle: int = 0, release: int = 0) -> Iterator[None]:
        """Hold the condition of a stream or an event, or of the connection's stream updates
        for a handle of zero, while the default thread runs the commands in the block. The
        hold is let go of through the release handle, or through the one that took it."""
        self.testee.run(hold_condition=pb.HoldCondition(stream=handle))
        try:
            yield
        finally:
            self.testee.run(release_condition=pb.ReleaseCondition(stream=release))

    def wait_for_update(self, timeout: float | None = None) -> None:
        """Wait for the next stream update message, holding the update condition."""
        command = pb.WaitForUpdate()
        if timeout is not None:
            command.timeout = timeout
        self.testee.run(REPLY_TIMEOUT + (timeout or 0), wait_for_update=command)

    def start(self, thread: str, condition: int = 0, **command: Any) -> Pending:
        """Send a command to run on a thread of the testee, without waiting for its reply.
        The testee holds the condition of the stream or event with the given handle while it
        runs the command."""
        return Pending(self.testee, self.testee.send(thread, condition, **command))

    @contextlib.contextmanager
    def paused(self, point: pb.RacePoint) -> Iterator[Pause]:
        """Arm a pause at a race point of the client. The next thread to reach the point
        pauses there until the block exits, or until an earlier resume."""
        self.testee.run(pause=pb.Pause(point=point))
        pause = Pause(self.testee)
        try:
            yield pause
        finally:
            pause.resume()

    def fail_write(self, message: str) -> None:
        """Arm a write fault. The client's next write to a connection fails with the
        message, as a failed socket write does."""
        self.testee.run(fail_write=pb.FailWrite(message=message))

    def interrupt(self, thread: str) -> None:
        """Interrupt the command running on a thread. Raises Unsupported where the language
        cannot."""
        self.testee.run(thread="interrupter", interrupt=pb.Interrupt(thread=thread))


class Pause:
    """A pause armed at a race point."""

    def __init__(self, testee: Testee) -> None:
        self._testee = testee
        self._resumed = False

    def resume(self) -> None:
        """Let the paused thread go on, once."""
        if not self._resumed:
            self._resumed = True
            self._testee.run(resume=pb.Resume())

    def wait(self, timeout: float = REPLY_TIMEOUT) -> None:
        """Wait until a thread has paused at the point."""
        reply = self._testee.run(
            REPLY_TIMEOUT + timeout, await_pause=pb.AwaitPause(timeout=timeout)
        )
        assert values.native(reply.value), "No thread paused at the race point"


_H = TypeVar("_H", bound="_Handle")


class _Handle:
    """A stream or event handle the testee holds. An event shares the stream commands
    through its handle."""

    def __init__(self, client: Client, handle: int) -> None:
        self._client = client
        self.handle = handle

    def remove(self) -> None:
        self._client.testee.run(remove=pb.Remove(stream=self.handle))

    def copy(self: _H) -> _H:
        """A copy of the handle, as the language copies one."""
        reply = self._client.testee.run(copy_stream=pb.CopyStream(stream=self.handle))
        return type(self)(self._client, reply.stream)

    def drop(self) -> None:
        """Drop the handle, as a program lets go of its reference."""
        self._client.testee.run(drop_handle=pb.DropHandle(stream=self.handle))

    def add_callback(self, behavior: pb.Behavior | None = None) -> Callback:
        callback = self._client.new_callback()
        self._client.testee.run(**self._add_callback(callback, behavior))
        return Callback(self._client.testee, callback, "remove_callback")

    def start_callback(
        self, thread: str, behavior: pb.Behavior | None = None
    ) -> tuple[Callback, Pending]:
        """Add a callback on a thread of the testee, without waiting for the add to
        return."""
        callback = self._client.new_callback()
        pending = self._client.start(thread, **self._add_callback(callback, behavior))
        return Callback(self._client.testee, callback, "remove_callback"), pending

    def add_end_callback(self, behavior: pb.Behavior | None = None) -> Callback:
        """Add a callback called once when the stream ends. Its calls are Endings."""
        callback = self._client.new_callback()
        self._client.testee.run(
            add_end_callback=pb.AddEndCallback(
                stream=self.handle, callback=callback, behavior=behavior
            )
        )
        return Callback(self._client.testee, callback, "remove_end_callback")

    def _add_callback(
        self, callback: int, behavior: pb.Behavior | None
    ) -> dict[str, Any]:
        """The command that adds a callback."""
        raise NotImplementedError


class Stream(_Handle):
    """A stream handle the testee holds."""

    def get(self) -> Any:
        reply = self._client.testee.run(get=pb.Get(stream=self.handle))
        return values.native(reply.value)

    def wait(self, timeout: float | None = None) -> bool:
        """Whether the stream updated. Without a timeout, the testee has the reply deadline
        to see an update."""
        command = pb.Wait(stream=self.handle)
        if timeout is not None:
            command.timeout = timeout
        deadline = REPLY_TIMEOUT + (timeout or 0)
        reply = self._client.testee.run(deadline, wait=command)
        return bool(values.native(reply.value))

    @property
    def rate(self) -> float:
        reply = self._client.testee.run(rate=pb.Rate(stream=self.handle))
        return reply.value.float

    @rate.setter
    def rate(self, value: float) -> None:
        self._client.testee.run(set_rate=pb.SetRate(stream=self.handle, rate=value))

    def _add_callback(
        self, callback: int, behavior: pb.Behavior | None
    ) -> dict[str, Any]:
        command = pb.AddCallback(
            stream=self.handle, callback=callback, behavior=behavior
        )
        return {"add_callback": command}


class Event(_Handle):
    """An event handle the testee holds. Its callbacks are called with no value on each
    firing."""

    def wait(self, timeout: float | None = None) -> bool:
        """Whether the event fired. Without a timeout, the testee has the reply deadline to
        see a firing."""
        command = pb.WaitEvent(event=self.handle)
        if timeout is not None:
            command.timeout = timeout
        deadline = REPLY_TIMEOUT + (timeout or 0)
        reply = self._client.testee.run(deadline, wait_event=command)
        return bool(values.native(reply.value))

    @property
    def stream(self) -> Stream:
        """The event's stream, a stream of bool."""
        reply = self._client.testee.run(event_stream=pb.EventStream(event=self.handle))
        return Stream(self._client, reply.stream)

    def _add_callback(
        self, callback: int, behavior: pb.Behavior | None
    ) -> dict[str, Any]:
        command = pb.AddEventCallback(
            event=self.handle, callback=callback, behavior=behavior
        )
        return {"add_event_callback": command}


class Pending:
    """A command sent to the testee, whose reply is yet to be read."""

    def __init__(self, testee: Testee, command_id: int) -> None:
        self._testee = testee
        self.id = command_id

    def reply(self, timeout: float = REPLY_TIMEOUT) -> pb.Reply:
        """The reply. Raises the error it reports as a TesteeError."""
        return self._testee.reply(self.id, timeout)

    def result(self, timeout: float = REPLY_TIMEOUT) -> Any:
        return values.native(self.reply(timeout).value)

    def error(self, timeout: float = REPLY_TIMEOUT) -> pb.Error:
        """The error the reply reports, which it must report."""
        try:
            reply = self.reply(timeout)
        except TesteeError as exn:
            return exn.error
        raise AssertionError("The command did not raise: %s" % reply)

    def done(self) -> bool:
        """Whether the reply has arrived."""
        return self._testee.has_reply(self.id)


@dataclass
class Ending:
    """A call of an end callback, with the error it was passed, if any."""

    error: pb.Error | None


class Callback:
    """A callback the testee added, and what it was called with: a value for a stream
    callback, an Ending for an end callback, and None for an update callback."""

    def __init__(self, testee: Testee, callback: int, remove_command: str) -> None:
        self._testee = testee
        self._remove_command = remove_command
        self.id = callback

    @property
    def calls(self) -> list[Any]:
        """The calls so far."""
        return [_call(x) for x in self.notifications if _is_call(x)]

    @property
    def notifications(self) -> list[pb.Notification]:
        """Every notification so far, including those for returns and for the replies to
        calls the callback made."""
        return self._testee.notifications(self.id)

    def wait_for_calls(self, count: int, timeout: float = REPLY_TIMEOUT) -> list[Any]:
        """The calls of the callback, once there are at least count of them."""
        arrived = wait_until(lambda: len(self.calls) >= count, timeout)
        assert arrived, "The callback has %d calls, not %d" % (len(self.calls), count)
        return self.calls

    def wait_for(
        self, condition: Callable[[list[pb.Notification]], bool]
    ) -> list[pb.Notification]:
        """The notifications, once a condition on them holds."""
        held = wait_until(lambda: condition(self.notifications))
        assert held, self.notifications
        return self.notifications

    def release(self) -> None:
        """Release one blocked call of the callback."""
        self._testee.run(release=pb.Release(callback=self.id))

    def remove(self) -> None:
        command: dict[str, Any] = {self._remove_command: {"callback": self.id}}
        self._testee.run(**command)


def _is_call(notification: pb.Notification) -> bool:
    return not notification.returned and not notification.HasField("result")


def _call(notification: pb.Notification) -> Any:
    case = notification.WhichOneof("call")
    if case == "value":
        return values.native(notification.value)
    if case == "ending":
        ending = notification.ending
        return Ending(ending.error if ending.HasField("error") else None)
    return None


def invocation(service: str, procedure: str, *args: pb.Value) -> pb.Invoke:
    """A call to pass to a command, or to a callback's behavior."""
    return _invoke(service, procedure, args)


def _invoke(
    service: str,
    procedure: str,
    args: tuple[pb.Value, ...],
    named: dict[str, pb.Value] | None = None,
) -> pb.Invoke:
    return pb.Invoke(
        service=service,
        procedure=procedure,
        arguments=list(args),
        named_arguments=[
            pb.NamedArgument(name=k, value=v) for k, v in (named or {}).items()
        ],
    )


def _value(x: values.Object | pb.Value) -> pb.Value:
    return x if isinstance(x, pb.Value) else values.obj(x)


def wait_until(condition: Any, timeout: float = REPLY_TIMEOUT) -> bool:
    """Poll a condition until it holds or the timeout elapses."""
    deadline = time.monotonic() + timeout
    while not condition():
        if time.monotonic() > deadline:
            return False
        time.sleep(0.01)
    return True


def update_barrier(client: Client, control: Any, name: str) -> None:
    """Wait until the client has called the callbacks for each update before this one. A
    later update of a stream of the barrier's own reaches its callback, as the client calls
    callbacks for one update at a time, in order."""
    stream = client.add_stream("ConformanceService", "Value", pb.Value(string=name))
    callback = stream.add_callback()
    (value,) = callback.wait_for_calls(1)
    control.conformance_service.set_value(name, value + 1)
    assert wait_until(lambda: callback.calls[-1:] == [value + 1])
    stream.remove()


def wait_for_control(
    stream: Any, condition: Callable[[Any], bool], timeout: float = REPLY_TIMEOUT
) -> None:
    """Wait until the value of a stream on the control connection meets a condition."""
    deadline = time.monotonic() + timeout
    with stream.condition:
        while not condition(stream()):
            remaining = deadline - time.monotonic()
            assert remaining > 0, "The control stream's value is %r" % (stream(),)
            stream.wait(remaining)
