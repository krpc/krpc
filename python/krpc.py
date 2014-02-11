import proto.RPC_pb2 as rpc
import proto.Control_pb2 as schema_control
import proto.Orbit_pb2 as schema_orbit
import socket

TCP_IP = '127.0.0.1'
TCP_PORT = 8888
BUFFER_SIZE = 4096

class Client(object):
    def __init__(self):
        self._conn = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        self._conn.connect((TCP_IP, TCP_PORT))

    def sendRequest(self, request):
        """ Send an rpc.Request """
        data = request.SerializeToString()
        self._conn.send(data)

    def getResponse(self):
        """ Receive and return an rpc.Response """
        data = self._conn.recv(BUFFER_SIZE)
        response = rpc.Response()
        response.ParseFromString(data)
        return response

class Service(object):
    def __init__(self, service, client):
        self._service = service
        self._client = client

    def execute(self, method, data = None):
        request = rpc.Request()
        request.service = self._service
        request.method = method
        if data != None:
            request.request = data.SerializeToString()
        self._client.sendRequest(request)
        response = self._client.getResponse()
        if response.error:
            raise RuntimeError(response.message.replace('\r','\n'))
        return response.response

class Control(Service):
    def __init__(self, client):
        super(Control, self).__init__("Control", client)

    def Set(self,
            throttle=None,
            pitch=None,
            yaw=None,
            roll=None,
            x=None,
            y=None,
            z=None,
            sas=None,
            rcs=None):
        controls = schema_control.Controls()
        if throttle != None:
            controls.throttle = throttle
        if sas != None:
            controls.sas = sas
        #TODO: implement fully
        self.execute("Set", controls)

    def Get(self):
        response = self.execute("Get")
        result = schema_control.Controls()
        result.ParseFromString(response)
        return result

    def ActivateNextStage(self):
        self.execute("ActivateNextStage")

class Orbit(Service):
    def __init__(self, client):
        super(Orbit, self).__init__("Orbit", client)

    def Get(self):
        response = self.execute("Get");
        result = schema_orbit.OrbitData()
        result.ParseFromString(response)
        return result
