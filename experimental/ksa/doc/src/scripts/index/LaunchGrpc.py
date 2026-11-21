import time

import grpc

# Generated from stubs/grpc/KSA.proto
import KSA_pb2 as ksa
import KSA_pb2_grpc as rpc

channel = grpc.insecure_channel("127.0.0.1:50002")
service = rpc.KSAServiceStub(channel)
vehicles = rpc.VehicleServiceStub(channel)
auto_pilots = rpc.AutoPilotServiceStub(channel)
controls = rpc.ControlServiceStub(channel)
flights = rpc.FlightServiceStub(channel)

# The first call names the client in the server's list of clients
vehicle = service.LaunchVehicle(
    ksa.KSA.LaunchVehicleRequest(name="Rocket", site="CCSFS LC-39A"),
    metadata=(("krpc-client-name", "Launch"),),
).value
name = vehicles.GetName(ksa.Vehicle.GetNameRequest(this=vehicle)).value
auto_pilot = vehicles.GetAutoPilot(ksa.Vehicle.GetAutoPilotRequest(this=vehicle)).value
control = vehicles.GetControl(ksa.Vehicle.GetControlRequest(this=vehicle)).value
flight = vehicles.GetFlight(ksa.Vehicle.GetFlightRequest(this=vehicle)).value

auto_pilots.TargetPitchAndHeading(
    ksa.AutoPilot.TargetPitchAndHeadingRequest(this=auto_pilot, pitch=90, heading=90)
)
auto_pilots.Engage(ksa.AutoPilot.EngageRequest(this=auto_pilot))
controls.SetThrottle(ksa.Control.SetThrottleRequest(this=control, value=1))
controls.ActivateNextSequence(ksa.Control.ActivateNextSequenceRequest(this=control))
controls.SetEngineOn(ksa.Control.SetEngineOnRequest(this=control, value=True))
print(f"{name} has launched")

# gRPC has no events, so poll the altitude
request = ksa.Flight.GetMeanAltitudeRequest(this=flight)
while flights.GetMeanAltitude(request).value < 10000:
    time.sleep(0.1)

controls.SetEngineOn(ksa.Control.SetEngineOnRequest(this=control, value=False))
auto_pilots.Disengage(ksa.AutoPilot.DisengageRequest(this=auto_pilot))
print(f"{name} reached 10 km")
channel.close()
