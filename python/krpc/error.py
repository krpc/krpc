class RPCError(RuntimeError):
    """ Error raised when an RPC returns an error response """
    def __init__(self, message):
        super(RPCError, self).__init__(message)
