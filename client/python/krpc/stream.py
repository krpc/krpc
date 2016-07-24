from krpc.types import Types
from krpc.decoder import Decoder
from krpc.error import RPCError


class StreamExistsError(RuntimeError):
    def __init__(self, stream_id):
        super(StreamExistsError, self).__init__('stream %d already exists' % stream_id)
        self.stream_id = stream_id


class Stream(object):
    """ A streamed request. When invoked, returns the most recent value of the request. """

    def __init__(self, conn, func, *args, **kwargs):
        self._conn = conn
        self._func = func
        self._args = args
        self._kwargs = kwargs
        self._removed = False
        self._callbacks = list()

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
        with self._conn._stream_cache_lock:
            self._stream_id = self._conn.krpc.add_stream(self._request)
            if self._stream_id in self._conn._stream_cache:
                raise StreamExistsError(self._stream_id)
            self._conn._stream_cache[self._stream_id] = self

    def __call__(self):
        """Returns the most recent value for this stream"""
        if isinstance(self._value, Exception):
            raise self._value
        return self._value

    def remove(self):
        """
        Removes this stream

        If any callbacks are registered, they will be called after the stream is removed.
        """
        with self._conn._stream_cache_lock:
            if self._stream_id in self._conn._stream_cache:
                self._conn.krpc.remove_stream(self._stream_id)
                del self._conn._stream_cache[self._stream_id]
                self._removed = True
                self.update(RuntimeError('Stream has been removed'))

    @property
    def removed(self):
        """True if the stream has been removed"""
        return self._removed

    @property
    def error(self):
        """True if this stream currently is an error (its current value is an exception)"""
        return isinstance(self._value, Exception)

    @property
    def return_type(self):
        """ The return type of this stream """
        return self._return_type

    def update(self, value):
        """Update the stream's most recent value """
        self._value = value

        for callback in self._callbacks:
            callback(self)

    def add_callback(self, callback, allow_duplicates=True):
        """
        Adds a callback function that will be called any time this stream's value is changed.

        Callbacks will receive this stream as their sole argument.  Note that callbacks are called whenever the stream
        value updates, including when the new value is an exception and when the stream is removed.

        Callbacks can examine the values of `Stream.error` and `Stream.removed` to determine the current state of the
        stream.

        Adding a callback that already exists will result in it being called twice.  To prevent this, specify
        `False` for the allow_duplicates parameter.

        This function is not thread-safe other than against kRPC's stream update thread.

        :param callback: Function to call when value changes
        :param allow_duplicates: True to allow duplicates, False to suppress
        :return: True if the callback was added, False otherwise
        """
        if not allow_duplicates and callback in self._callbacks:
            return False

        # It'd be bad to remove/add items to the callback structure while we were mid-update, since adding to an
        # iterable during iteration is bad.  However, we probably don't want to have to acquire a lock every time
        # update() is called (since it may be very, very frequently).  So our compromise is to duplicate the callback
        # list and modify the duplicate in add/remove_callback.
        callbacks = self._callbacks.copy()
        callbacks.append(callback)
        self._callbacks = callbacks
        return True

    def remove_callback(self, callback, remove_all=False):
        """
        Removes a previously-added callback function.

        If the callback function was previously added multiple times, only one instance of it will be removed.
        Specify `True` for the remove_all parameter to remove all instances.

        :param callback: Callback to remove
        :param remove_all: True to remove all instances instead of just the first.
        :return: True if at least one instance of the callback was removed.
        """
        if not self._callbacks:
            return False

        if remove_all:
            # This implementation is 'safe' to the same extent add_callback is, see the comments there.
            prevlen = len(self._callbacks)
            self._callbacks = list(item for item in self._callbacks if item != callback)
            return prevlen != len(self._callbacks)

        # See add_callback for why we do this.
        callbacks = self._callbacks.copy()
        try:
            callbacks.remove(callback)
        except ValueError:
            return False
        self._callbacks = callbacks
        return True


def add_stream(conn, func, *args, **kwargs):
    """ Create a stream and return it """
    try:
        return Stream(conn, func, *args, **kwargs)
    except StreamExistsError as ex:
        return conn._stream_cache[ex.stream_id]


def update_thread(connection, stop, cache, cache_lock):
    stream_message_type = Types().stream_message_type

    while True:

        # Read the size and position of the response message
        data = b''
        while True:
            try:
                data += connection.partial_receive(1)
                size, _ = Decoder.decode_size_and_position(data)
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

        # Read and decode the response message
        data = connection.receive(size)
        response = Decoder.decode(data, stream_message_type)

        # Add the data to the cache
        with cache_lock:
            for response in response.responses:
                if response.id not in cache:
                    continue

                # Check for an error response
                if response.response.error:
                    cache[response.id].value = RPCError(response.response.error)
                    continue

                # Decode the return value and store it in the cache
                typ = cache[response.id].return_type
                value = Decoder.decode(response.response.return_value, typ)
                cache[response.id].update(value)
