--- The errors the client raises, as objects. Each class's class_of checks a category, such as
--- krpc.ConnectionError:class_of(err), and matches its subclasses. An error converts to its
--- message with tostring, and concatenates as its message.
local class = require 'pl.class'

local error_module = {}

--- The base of every error the client raises of its own. message is what failed, followed by
--- the message of cause, the error that caused it, if there is one.
local RemoteError = class()
error_module.RemoteError = RemoteError

function RemoteError:_init(message, cause)
  if cause ~= nil then
    message = message .. ': ' .. tostring(cause)
  end
  self.message = message
  self.cause = cause
end

function RemoteError:__tostring()
  return self.message
end

function RemoteError.__concat(a, b)
  return tostring(a) .. tostring(b)
end

--- Raised when connecting fails, the server refuses the connection, or the connection fails or
--- ends, and by every call after it has. status is the name of the ConnectionResponse status of
--- a refused connection, such as 'DENIED', and reason the name of the Disconnect reason of a
--- connection the server ended, such as 'SERVER_STOPPED'.
local ConnectionError = class(RemoteError)
error_module.ConnectionError = ConnectionError

function ConnectionError:_init(message, cause, status, reason)
  self:super(message, cause)
  self.status = status
  self.reason = reason
end

--- Raised for an error the server reports. service and name are those of the server's
--- exception, or nil for an error with no type.
local ServerError = class(RemoteError)
error_module.ServerError = ServerError

function ServerError:_init(message, service, name)
  self:super(message)
  self.service = service
  self.name = name
end

--- Raised for a value that fails to decode.
local EncodingError = class(RemoteError)
error_module.EncodingError = EncodingError

return error_module
