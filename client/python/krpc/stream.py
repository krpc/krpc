from krpc.decoder import Decoder
from krpc.error import RPCError, StreamError
import krpc.schema


class Stream(object):
    """ A streamed request. When invoked, returns the
        most recent value of the request. """

    def __init__(self, conn, func, *args, **kwargs):
        self._conn = conn
        self._func = func
        self._args = args
        self._kwargs = kwargs
        # Get the request and return type
        if func == getattr:
            # A property or class property getter
            attr = func(args[0].__class__, args[1])
            self._call = attr.fget._build_call(args[0])
            self._return_type = attr.fget._return_type
        elif func == setattr:
            # A property setter
            raise StreamError('Cannot stream a property setter')
        elif hasattr(func, '__self__'):
            # A method
            self._call = func._build_call(func.__self__, *args, **kwargs)
            self._return_type = func._return_type
        else:
            # A class method
            self._call = func._build_call(*args, **kwargs)
            self._return_type = func._return_type
        # Set the initial value by running the RPC once
        self._value = func(*args, **kwargs)
        # Add the stream to the server and add the initial value to the cache
        with self._conn._stream_cache_lock:
            self._stream_id = self._conn.krpc.add_stream(self._call).id
            if self._stream_id not in self._conn._stream_cache:
                self._conn._stream_cache[self._stream_id] = self

    def __call__(self):
        """ Get the most recent value for this stream """
        if isinstance(self._value, Exception):
            raise self._value
        return self._value

    def remove(self):
        """ Remove the stream """
        with self._conn._stream_cache_lock:
            if self._stream_id in self._conn._stream_cache:
                self._conn.krpc.remove_stream(self._stream_id)
                del self._conn._stream_cache[self._stream_id]
                self._value = StreamError('Stream has been removed')

    @property
    def return_type(self):
        """ The return type of this stream """
        return self._return_type

    def update(self, value):
        """ Update the stream's most recent value """
        self._value = value


def add_stream(conn, func, *args, **kwargs):
    """ Create a stream and return it """
    stream = Stream(conn, func, *args, **kwargs)
    return conn._stream_cache[stream._stream_id]


def update_thread(connection, stop, cache, cache_lock):
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
        update = Decoder.decode_message(data, krpc.schema.KRPC.StreamUpdate)

        # Add the data to the cache
        with cache_lock:
            for result in update.results:
                if result.id not in cache:
                    continue

                # Check for an error response
                if result.result.HasField('error'):
                    cache[result.id].value = RPCError(
                        result.result.error.description)
                    continue

                # Decode the return value and store it in the cache
                typ = cache[result.id].return_type
                value = Decoder.decode(result.result.value, typ)
                cache[result.id].update(value)
