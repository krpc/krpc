using NUnit.Framework;
using Moq;
using KRPC.Server;
using KRPC.Schema.RPC;
using KRPC.Utils;
using System.IO;

namespace KRPCTest.Server
{
	[TestFixture]
	public class RPCServerTest
	{
		Request request;
		byte[] requestBytes;

		[SetUp]
		public void SetUp ()
		{
			// Create a request object and get the binary representation of it
			request = Request.CreateBuilder ().SetService ("service").SetMethod ("method").BuildPartial ();
			using (MemoryStream stream = new MemoryStream()) {
				request.WriteTo (stream);
				requestBytes = stream.ToArray ();
			}
		}

		[Test]
		public void NoClientsConnected ()
		{
			// Set up mock IServer
			var mock = new Mock<IServer>();

			int[] clients = {};
			mock.Setup(x => x.GetConnectedClientIds()).Returns(clients);

			// Set up RPCServer
			var server = new RPCServer(mock.Object);
			server.Start();
			mock.Verify(x => x.Start(), Times.Once());

			// Run tests
			Assert.Throws<NoRequestException>(() => { server.GetRequest(); });

			// Tear down RPCServer
			server.Stop();
			mock.Verify(x => x.Start(), Times.Once());
			mock.Verify(x => x.Stop(), Times.Once());
		}

		/// <summary>
		/// Test GetRequest when the client has sent zero bytes.
		/// </summary>
		[Test]
		public void ReceiveNoRequest()
		{
			// Set up mock IServer
			var mock = new Mock<IServer>();

			int[] clients = {0};
			mock.Setup(x => x.GetConnectedClientIds()).Returns(clients);
			
			var clientStream = new Mock<INetworkStream>();
			clientStream.Setup(x => x.DataAvailable).Returns(false);
			mock.Setup (x => x.GetClientStream(0)).Returns(clientStream.Object);

			// Set up RPCServer
			var server = new RPCServer(mock.Object);
			server.Start();

			// Run tests
			Assert.Throws<NoRequestException>(() => { server.GetRequest(); });

			// Tear down RPCServer
			server.Stop();
			mock.Verify(x => x.Start(), Times.Once());
			mock.Verify(x => x.Stop(), Times.Once());
		}

		/// <summary>
		/// Create a mock network stream that returns the given data in a single call to Read
		/// </summary>
		private Mock<INetworkStream> createMockNetworkStream (byte[] data)
		{
			var mock = new Mock<INetworkStream>();
			mock.Setup (x => x.DataAvailable).Returns(true);
			mock.Setup (x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()))
				.Callback<byte[],int,int>((buffer, offset, size) =>
				{
					// Check the output buffer is large enough
					Assert.IsTrue(size >= data.Length);
					data.CopyTo(buffer,0);
					mock.Setup (x => x.DataAvailable).Returns(false);
				}).Returns(data.Length);
			return mock;
		}

		/// <summary>
		/// Test GetRequest when the client has sent the bytes for a single well-formed message.
		/// </summary>
		[Test]
		public void ReceiveSingleRequest ()
		{
			// Set up mock IServer
			var mock = new Mock<IServer>();

			int[] clients = {0};
			mock.Setup(x => x.GetConnectedClientIds()).Returns(clients);
			var clientStream = createMockNetworkStream(requestBytes).Object;
			mock.Setup (x => x.GetClientStream(0)).Returns(clientStream);

			// Set up RPCServer
			var server = new RPCServer(mock.Object);
			server.Start();

			// Run tests
			Tuple<int,Request> ret = server.GetRequest();
			Request req = ret.Item2;
			Assert.IsTrue(req.IsInitialized);
			Assert.AreEqual(request.Service, req.Service);
			Assert.AreEqual(request.Method, req.Method);
			Assert.Throws<NoRequestException>(() => { server.GetRequest(); });

			// Tear down RPCServer
			server.Stop();
			mock.Verify(x => x.Start(), Times.Once());
			mock.Verify(x => x.Stop(), Times.Once());
		}

		/// <summary>
		/// Test GetRequest when the client has sent a partial message (a subset of the bytes for a single well-formed message).
		/// and a subsequent call to GetRequest after the remaining bytes have been sent.
		/// </summary>
		[Test]
		public void ReceivePartialRequest ()
		{
			// Set up mock IServer
			var mock = new Mock<IServer> ();

			int[] clients = {0};
			mock.Setup (x => x.GetConnectedClientIds ()).Returns (clients);

			// Set up mock of client stream
			// A complex use case of Mocking, so simpler to create an actual object
			var stream = new MemoryStream();
			stream.Write(requestBytes, 0, requestBytes.Length-1);
			stream.Seek (0, SeekOrigin.Begin);
			var clientStream = new MemoryStreamWrapper(stream);
			mock.Setup (x => x.GetClientStream(0)).Returns(clientStream);
			// Set up RPCServer
			var server = new RPCServer(mock.Object);
			server.Start();

			// Try getting request whilst there is partial data
			Assert.Throws<NoRequestException>(() => { server.GetRequest(); });
			// Write the final byte
			stream.Seek (0, SeekOrigin.End);
			stream.WriteByte(requestBytes[requestBytes.Length-1]);
			stream.Seek (-1, SeekOrigin.Current);
			// Try the request again
			Tuple<int,Request> ret = server.GetRequest();
			Request req = ret.Item2;
			Assert.IsTrue(req.IsInitialized);
			Assert.AreEqual(request.Service, req.Service);
			Assert.AreEqual(request.Method, req.Method);
			// Should be no further requests
			Assert.Throws<NoRequestException>(() => { server.GetRequest(); });

			// Tear down RPCServer
			server.Stop();
			mock.Verify(x => x.Start(), Times.Once());
			mock.Verify(x => x.Stop(), Times.Once());
		}
		
		/// <summary>
		/// Test GetRequest when the client has sent a malformed request message.
		/// </summary>
		[Test]
		public void ReceiveMalformedRequest ()
		{
			// Set up mock IServer
			var mock = new Mock<IServer>();

			int[] clients = {0};
			mock.Setup(x => x.GetConnectedClientIds()).Returns(clients);

			var request = Request.CreateBuilder().SetService("service").BuildPartial();
			MemoryStream buffer = new MemoryStream();
			request.WriteTo(buffer);
			var clientStream = createMockNetworkStream(buffer.ToArray()).Object;
			mock.Setup (x => x.GetClientStream(0)).Returns(clientStream);

			// Set up RPCServer
			var server = new RPCServer(mock.Object);
			server.Start();

			// Run tests
			Assert.Throws<MalformedRequestException>(() => { server.GetRequest(); });
			Assert.Throws<NoRequestException>(() => { server.GetRequest(); });

			// Tear down RPCServer
			server.Stop();
			mock.Verify(x => x.Start(), Times.Once());
			mock.Verify(x => x.Stop(), Times.Once());
		}
	}
}
