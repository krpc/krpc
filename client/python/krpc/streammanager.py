import threading
from krpc.decoder import Decoder
from krpc.error import StreamError
import krpc.schema.KRPC_pb2 as KRPC


class StreamImpl(object):
    def __init__(self, conn, stream_id, return_type, update_lock):
        self._conn = conn
        self._stream_id = stream_id
        self._return_type = return_type
        self._update_lock = update_lock
        self._started = False
        self._updated = False
        self._value = None
        self._condition = threading.Condition()
        self._callbacks = []
        self._rate = 0

    @property
    def return_type(self):
        return self._return_type

    def start(self):
        if not self._started:
            self._conn.krpc.start_stream(self._stream_id)
            self._started = True

    @property
    def rate(self):
        return self._rate

    @rate.setter
    def rate(self, value):
        self._rate = value
        self._conn.krpc.set_stream_rate(self._stream_id, value)

    @property
    def started(self):
        return self._started

    @property
    def value(self):
        if not self._updated:
            raise StreamError("Stream has no value")
        return self._value

    @value.setter
    def value(self, value):
        with self._update_lock:
            self._updated = True
            self._value = value

    @property
    def updated(self):
        return self._updated

    @property
    def condition(self):
        return self._condition

    @property
    def callbacks(self):
        with self._update_lock:
            return self._callbacks

    def add_callback(self, callback):
        with self._update_lock:
            self._callbacks = self._callbacks[:]
            self._callbacks.append(callback)
            return self._callbacks

    def remove_callback(self, callback):
        with self._update_lock:
            self._callbacks = [x for x in self._callbacks if x != callback]
            return self._callbacks

    def remove(self):
        self._conn._stream_manager.remove_stream(self._stream_id)
        with self._update_lock:
            self._value = StreamError("Stream does not exist")


class StreamManager(object):
    def __init__(self, conn):
        self._conn = conn
        self._update_lock = threading.RLock()
        self._streams = {}

    def add_stream(self, return_type, call):
        stream_id = self._conn.krpc.add_stream(call, False).id
        with self._update_lock:
            if stream_id not in self._streams:
                self._streams[stream_id] = StreamImpl(
                    self._conn, stream_id, return_type, self._update_lock)
            return self._streams[stream_id]

    def get_stream(self, return_type, stream_id):
        with self._update_lock:
            if stream_id not in self._streams:
                self._streams[stream_id] = StreamImpl(
                    self._conn, stream_id, return_type, self._update_lock)
            return self._streams[stream_id]

    def remove_stream(self, stream_id):
        with self._update_lock:
            if stream_id in self._streams:
                self._conn.krpc.remove_stream(stream_id)
                del self._streams[stream_id]

    def update(self, results):
        with self._update_lock:
            for result in results:
                if result.id not in self._streams:
                    continue

                # Check for an error response
                if result.result.HasField('error'):
                    self._update_stream(
                        result.id,
                        self._conn._build_error(result.result.error))
                    continue

                # Decode the return value and store it in the cache
                typ = self._streams[result.id].return_type
                value = Decoder.decode(result.result.value, typ)
                self._update_stream(result.id, value)

    def _update_stream(self, stream_id, value):
        stream = self._streams[stream_id]
        with stream.condition:
            stream.value = value
            stream.condition.notify_all()
        for fn in stream.callbacks:
            fn(value)


def update_thread(manager, connection, stop):
    while True:

        # Read the size and position of the update message
        data = b''
        while True:
            try:
                data += connection.partial_receive(1)
                size = Decoder.decode_message_size(data)
                break
            except IndexError:
                pass
            except:  # pylint: disable=bare-except
                # TODO: is there a better way to catch exceptions when the
                #      thread is forcibly stopped (e.g. by CTRL+c)?
                return
            if stop.is_set():
                connection.close()
                return

        # Read and decode the update message
        data = connection.receive(size)
        update = Decoder.decode_message(data, KRPC.StreamUpdate)

        # Add the data to the cache
        manager.update(update.results)
