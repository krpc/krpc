from contextlib import contextmanager

# AddStream is called with a single argument, containing the bytes of a Request object
# Server decodes and saves the Request object, and this is what it executes
# Server sends multiple Response objects to streaming channel in response

class Stream(object):
    """ A streamed request """

    def __init__(self, func, *args, **kwargs):
        self._func = func
        self._args = args
        self._kwargs = kwargs
        # Get the request
        if func == getattr:
            # A property or class property getter
            attr = func(args[0].__class__, args[1])
            self._request = attr.fget._build_request(args[0])
        elif func == setattr:
            # A property setter
            raise ValueError('Cannot stream a property setter')
        elif hasattr(func, '__self__'):
            # A method
            self._request = func._build_request(func.__self__, *args, **kwargs)
        else:
            # A class method
            self._request = func._build_request(*args, **kwargs)
        # Set the initial value by running the RPC
        self._value = func(*args, **kwargs)

    @property
    def request(self):
        """ The request run to generate this stream """
        return self._request

    @property
    def value(self):
        """ The most recent value from this stream """
        return self._value

    def __call__(self):
        return self.value

def add_stream(func, *args, **kwargs):
    """ Create a stream and return it """
    return Stream(func, *args, **kwargs)

def remove_stream(s):
    """ Remove a stream """
    del s

@contextmanager
def stream(func, *args, **kwargs):
    """ 'with' support """
    s = add_stream(func, *args, **kwargs)
    try:
        yield s
    finally:
        remove_stream(s)
