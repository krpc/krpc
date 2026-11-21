import math
import sys
import threading

import grpc
import ksatest

# Generated from bin/stubs/grpc/KSA.proto and StdLib.proto by test.sh
sys.path.insert(0, str(ksatest.ROOT / "bin" / "grpc-python"))
import KSA_pb2 as pb  # noqa: E402
import KSA_pb2_grpc  # noqa: E402
import StdLib_pb2 as stdlib_pb  # noqa: E402
import StdLib_pb2_grpc  # noqa: E402

ADDRESS = "127.0.0.1:50002"


def open_channel(name):
    """A channel to the gRPC server, and a stub that names the client on each call."""
    # The channels of a process share one connection unless each has its own pool, and the
    # server makes one client per connection
    channel = grpc.insecure_channel(
        ADDRESS, options=[("grpc.use_local_subchannel_pool", 1)]
    )
    metadata = (("krpc-client-name", name),)

    class Service:
        def __init__(self, stub):
            self.stub = stub

        def __getattr__(self, method):
            call = getattr(self.stub, method)
            return lambda request, **kwargs: call(request, metadata=metadata, **kwargs)

    class Stubs:
        """The stub of each service, e.g. stubs.Vehicle for VehicleService."""

        def __getattr__(self, holder):
            return Service(getattr(KSA_pb2_grpc, f"{holder}ServiceStub")(channel))

    return channel, Stubs()


def client_names(conn):
    return [client.name for client in conn.krpc.clients]


class TestGrpc(ksatest.TestCase):
    @classmethod
    def setUpClass(cls):
        super().setUpClass()
        cls.load_save()
        cls.channel, cls.stub = open_channel("grpc test")

    @classmethod
    def tearDownClass(cls):
        cls.channel.close()

    def vehicle(self, name):
        for vehicle in self.stub.KSA.GetVehicles(pb.KSA.GetVehiclesRequest()).value:
            if (
                self.stub.Vehicle.GetName(pb.Vehicle.GetNameRequest(this=vehicle)).value
                == name
            ):
                return vehicle
        raise AssertionError(f"No vehicle named {name}")

    def test_object_ids_match_krpc(self):
        vehicle = self.vehicle("Gemini7")
        self.assertEqual(self.find_vehicle("Gemini7")._object_id, vehicle.id)

    def test_scalars(self):
        ut = self.stub.KSA.GetUT(pb.KSA.GetUTRequest()).value
        self.assertAlmostEqual(self.ksa.ut, ut, delta=1)
        vehicle = self.vehicle("Gemini7")
        mass = self.stub.Vehicle.GetMass(pb.Vehicle.GetMassRequest(this=vehicle)).value
        self.assertAlmostEqual(self.find_vehicle("Gemini7").mass, mass, places=3)

    def test_set_name(self):
        vehicle = self.vehicle("Gemini7")
        try:
            self.stub.Vehicle.SetName(
                pb.Vehicle.SetNameRequest(this=vehicle, value="GrpcRenamed")
            )
            self.assertEqual("GrpcRenamed", self.find_vehicle("GrpcRenamed").name)
        finally:
            self.stub.Vehicle.SetName(
                pb.Vehicle.SetNameRequest(this=vehicle, value="Gemini7")
            )
        self.assertEqual("Gemini7", self.find_vehicle("Gemini7").name)

    def test_enum(self):
        vehicle = self.vehicle("Gemini7")
        situation = self.stub.Vehicle.GetSituation(
            pb.Vehicle.GetSituationRequest(this=vehicle)
        ).value
        self.assertEqual(pb.SITUATION_FREEFALL, situation)

    def test_tuple(self):
        vehicle = self.vehicle("Gemini7")
        body = self.stub.KSA.GetBodies(pb.KSA.GetBodiesRequest()).value["Earth"]
        frame = self.stub.Body.GetReferenceFrame(
            pb.Body.GetReferenceFrameRequest(this=body)
        ).value
        position = self.stub.Vehicle.Position(
            pb.Vehicle.PositionRequest(this=vehicle, reference_frame=frame)
        ).value
        expected = self.find_vehicle("Gemini7").position(
            self.ksa.bodies["Earth"].reference_frame
        )
        actual = (position.item1, position.item2, position.item3)
        for x, y in zip(expected, actual):
            # The vehicle moves between the two calls
            self.assertAlmostEqual(x, y, delta=1e4)
        self.assertGreater(sum(x * x for x in actual) ** 0.5, 6.3e6)

    def test_struct(self):
        body = self.stub.KSA.GetBodies(pb.KSA.GetBodiesRequest()).value["Earth"]
        frame = self.stub.Body.GetReferenceFrame(
            pb.Body.GetReferenceFrameRequest(this=body)
        ).value
        line = self.stub.Drawing.AddLine(
            pb.Drawing.AddLineRequest(
                start=pb.Tuple_double_double_double(item1=0, item2=0, item3=0),
                end=pb.Tuple_double_double_double(item1=1e7, item2=0, item3=0),
                reference_frame=frame,
            )
        ).value
        try:
            self.stub.Line.SetColor(
                pb.Line.SetColorRequest(
                    this=line, value=stdlib_pb.ColorRGB(r=1, g=0.5, b=0.25)
                )
            )
            color = self.stub.Line.GetColor(pb.Line.GetColorRequest(this=line)).value
            self.assertEqual((1, 0.5, 0.25), (color.r, color.g, color.b))
            color = self.ksa.Line(self.conn, line.id).color
            self.assertEqual((1, 0.5, 0.25), (color.r, color.g, color.b))
        finally:
            self.stub.Line.Remove(pb.Line.RemoveRequest(this=line))

    def test_struct_result(self):
        stdlib = StdLib_pb2_grpc.StdLibServiceStub(self.channel)
        half = math.sqrt(0.5)
        result = stdlib.QuaternionToAxisAngle(
            stdlib_pb.StdLib.QuaternionToAxisAngleRequest(
                q=stdlib_pb.Tuple_double_double_double_double(
                    item1=0, item2=0, item3=half, item4=half
                )
            )
        ).value
        axis = (result.axis.item1, result.axis.item2, result.axis.item3)
        for x, y in zip((0, 0, 1), axis):
            self.assertAlmostEqual(x, y)
        self.assertAlmostEqual(math.pi / 2, result.angle)

    def test_list_and_dictionary(self):
        vehicles = self.stub.KSA.GetVehicles(pb.KSA.GetVehiclesRequest()).value
        self.assertEqual(
            sorted(v._object_id for v in self.ksa.vehicles),
            sorted(v.id for v in vehicles),
        )
        bodies = self.stub.KSA.GetBodies(pb.KSA.GetBodiesRequest()).value
        self.assertEqual(sorted(self.ksa.bodies.keys()), sorted(bodies.keys()))
        self.assertEqual(self.ksa.bodies["Earth"]._object_id, bodies["Earth"].id)

    def test_nullable(self):
        rocket = self.vehicle("Rocket")
        gemini = self.vehicle("Gemini7")
        target = self.stub.Vehicle.GetTargetVehicle(
            pb.Vehicle.GetTargetVehicleRequest(this=rocket)
        )
        self.assertFalse(target.HasField("value"))
        try:
            self.stub.Vehicle.SetTargetVehicle(
                pb.Vehicle.SetTargetVehicleRequest(this=rocket, value=gemini)
            )
            target = self.stub.Vehicle.GetTargetVehicle(
                pb.Vehicle.GetTargetVehicleRequest(this=rocket)
            )
            self.assertEqual(gemini.id, target.value.id)
        finally:
            self.stub.Vehicle.SetTargetVehicle(
                pb.Vehicle.SetTargetVehicleRequest(this=rocket)
            )
        target = self.stub.Vehicle.GetTargetVehicle(
            pb.Vehicle.GetTargetVehicleRequest(this=rocket)
        )
        self.assertFalse(target.HasField("value"))

    def test_default_arguments(self):
        vehicle = self.vehicle("Gemini7")
        ut = self.stub.KSA.GetUT(pb.KSA.GetUTRequest()).value
        burn = self.stub.Vehicle.AddBurn(
            pb.Vehicle.AddBurnRequest(this=vehicle, ut=ut + 3600, prograde=10)
        ).value
        try:
            self.assertEqual(
                10,
                self.stub.Burn.GetPrograde(pb.Burn.GetProgradeRequest(this=burn)).value,
            )
            self.assertEqual(
                0, self.stub.Burn.GetNormal(pb.Burn.GetNormalRequest(this=burn)).value
            )
        finally:
            self.stub.Burn.Remove(pb.Burn.RemoveRequest(this=burn))

    def test_service_error(self):
        vehicle = self.vehicle("Gemini7")
        with self.assertRaises(grpc.RpcError) as context:
            self.stub.Vehicle.SetName(
                pb.Vehicle.SetNameRequest(this=vehicle, value="Rocket")
            )
        error = context.exception
        self.assertEqual(grpc.StatusCode.INVALID_ARGUMENT, error.code())
        trailers = dict(error.trailing_metadata())
        self.assertEqual("ArgumentException", trailers["krpc-error-name"])
        self.assertEqual("Gemini7", self.find_vehicle("Gemini7").name)

    def test_unknown_object(self):
        with self.assertRaises(grpc.RpcError) as context:
            self.stub.Vehicle.GetName(
                pb.Vehicle.GetNameRequest(this=pb.Vehicle(id=999999))
            )
        self.assertEqual(grpc.StatusCode.INVALID_ARGUMENT, context.exception.code())

    def test_connections_are_clients(self):
        # A connection's client is created by its first call
        self.stub.KSA.GetUT(pb.KSA.GetUTRequest())
        ksatest.wait_until(lambda: "grpc test" in client_names(self.conn))
        channel, stub = open_channel("grpc second")
        try:
            stub.KSA.GetUT(pb.KSA.GetUTRequest())
            ksatest.wait_until(lambda: "grpc second" in client_names(self.conn))
            self.assertIn("grpc test", client_names(self.conn))
        finally:
            channel.close()
        ksatest.wait_until(lambda: "grpc second" not in client_names(self.conn))
        self.assertIn("grpc test", client_names(self.conn))

    def test_yielding_call(self):
        channel, stub = open_channel("grpc warp")
        try:
            ut = stub.KSA.GetUT(pb.KSA.GetUTRequest()).value
            result = {}

            def warp():
                stub.KSA.WarpTo(pb.KSA.WarpToRequest(ut=ut + 60))
                result["ut"] = self.stub.KSA.GetUT(pb.KSA.GetUTRequest()).value

            thread = threading.Thread(target=warp)
            thread.start()
            # Other clients' calls run while the warp yields
            self.stub.KSA.GetUT(pb.KSA.GetUTRequest())
            self.assertTrue(thread.is_alive())
            thread.join(timeout=60)
            self.assertFalse(thread.is_alive())
            self.assertGreaterEqual(result["ut"], ut + 60)
        finally:
            channel.close()
        self.ksa.simulation_speed = 1
