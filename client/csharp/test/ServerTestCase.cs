using KRPC.Client;
using NUnit.Framework;

namespace KRPC.Client.Test
{
    public class ServerTestCase
    {
        protected Connection connection;

        [SetUp]
        public virtual void SetUp ()
        {
            connection = new Connection (rpcPort: 50018, streamPort: 50019);
        }
    }
}
