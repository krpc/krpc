from krpc.types import Types
from krpc.decoder import Decoder
from krpc.error import RPCError
import socket
import threading

_stream_cache = {}
_stream_cache_lock = threading.Lock()

class StreamExistsError(RuntimeError):
    def __init__(self, stream_id):
        self.stream_id = stream_id

class Stream(object):
    """ A streamed request. When invoked, returns the most recent value of the request. """

    def __init__(self, conn, func, *args, **kwargs):
        self._conn = conn
        self._func = func
        self._args = args
        self._kwargs = kwargs
        # Get the request and return type
        if func == getattr:
            # A property or class property getter
            attr = func(args[0].__class__, args[1])
            self._request = attr.fget._build_request(args[0])
            self._return_type = attr.fget._return_type
        elif func == setattr:
            # A property setter
            raise ValueError('Cannot stream a property setter')
        elif hasattr(func, '__self__'):
            # A method
            self._request = func._build_request(func.__self__, *args, **kwargs)
            self._return_type = func._return_type
        else:
            # A class method
            self._request = func._build_request(*args, **kwargs)
            self._return_type = func._return_type
        # Set the initial value by running the RPC once
        self._value = func(*args, **kwargs)
        # Add the stream to the server and add the initial value to the cache
        with _stream_cache_lock:
            self._stream_id = self._conn.krpc.add_stream(self._request)
            if self._stream_id in _stream_cache:
                raise StreamExistsError(self._stream_id)
            _stream_cache[self._stream_id] = self

    def __call__(self):
        """ Get the most recent value for this stream """
        if isinstance(self._value, Exception):
            raise self._value
        return self._value

    def remove(self):
        """ Remove the stream """
        with _stream_cache_lock:
            if self._stream_id in _stream_cache:
                self._conn.krpc.remove_stream(self._stream_id)
                del _stream_cache[self._stream_id]
                self._value = RuntimeError('Stream has been removed')

    @property
    def return_type(self):
        """ The return type of this stream """
        return self._return_type

    def update(self, value):
        """ Update the stream's most recent value """
        self._value = value

def add_stream(conn, func, *args, **kwargs):
    """ Create a stream and return it """
    try:
        return Stream(conn, func, *args, **kwargs)
    except StreamExistsError as e:
        return _stream_cache[e.stream_id]

def update_thread(connection, stop):
    stream_message_type = Types().as_type('KRPC.StreamMessage')
    response_type = Types().as_type('KRPC.Response')

    while True:

        # Read the size and position of the response message
        data = b''
        while True:
            try:
                data += connection.partial_receive(1)
                size,position = Decoder.decode_size_and_position(data)
                break
            except IndexError:
                pass
            except:
                #TODO: is there a better way to catch exceptions when the thread is forcibly stopped (e.g. by CTRL+c)?
                return
            if stop.is_set():
                connection.close()
                return

        # Read and decode the response message
        data = connection.receive(size)
        response = Decoder.decode(data, stream_message_type)

        # Add the data to the cache
        with _stream_cache_lock:
            for response in response.responses:
                id = response.id
                if id not in _stream_cache:
                    continue

                # Check for an error response
                if response.response.has_error:
                    _stream_cache[id].value = RPCError(response.response.error)
                    continue

                # Decode the return value and store it in the cache
                typ = _stream_cache[id].return_type
                value = Decoder.decode(response.response.return_value, typ)
                _stream_cache[id].update(value)
