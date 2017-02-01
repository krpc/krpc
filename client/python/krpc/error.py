class RPCError(RuntimeError):
    """ Error raised when an RPC returns an error response """
    def __init__(self, message):
        super(RPCError, self).__init__(message)


class NetworkError(RuntimeError):
    """ Error raised when something goes wrong with the network connection """
    def __init__(self, address, port, message):
        super(NetworkError, self).__init__(
            '%s (address=%s, port=%s)' % (message, str(address), str(port)))
