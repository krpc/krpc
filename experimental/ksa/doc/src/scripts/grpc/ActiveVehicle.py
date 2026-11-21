import grpc

# Generated from stubs/grpc/KSA.proto
import KSA_pb2 as ksa
import KSA_pb2_grpc

channel = grpc.insecure_channel("127.0.0.1:50002")
# The server shows this name in its list of clients
metadata = (("krpc-client-name", "Example"),)

service = KSA_pb2_grpc.KSAServiceStub(channel)
vehicles = KSA_pb2_grpc.VehicleServiceStub(channel)

vehicle = service.GetActiveVehicle(
    ksa.KSA.GetActiveVehicleRequest(), metadata=metadata
).value
name = vehicles.GetName(ksa.Vehicle.GetNameRequest(this=vehicle), metadata=metadata)
print(name.value)

ut = service.GetUT(ksa.KSA.GetUTRequest(), metadata=metadata)
print(f"UT {ut.value:.0f} s")
channel.close()
