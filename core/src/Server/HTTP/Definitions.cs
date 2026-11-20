using KRPC.Server.ProtocolBuffers;

namespace KRPC.Server.HTTP
{
    /// <summary>
    /// The routes, the JSON codec and the OpenAPI document of the HTTP servers, built from the
    /// service definitions as the first HTTP server starts. The services are fixed once the
    /// game has loaded, so every HTTP server shares them for the life of the process. With the
    /// servers started as the game loads, the cost of the build lands in the load.
    /// </summary>
    sealed class Definitions
    {
        static Definitions instance;

        public static Definitions Instance {
            get {
                if (instance == null)
                    instance = new Definitions ();
                return instance;
            }
        }

        Definitions ()
        {
            var services = global::KRPC.Service.KRPC.KRPC.GetServices ().ToProtobufMessage ();
            Routes = new Routes (services);
            Codec = new Codec (services);
            OpenApi = HTTP.OpenApi.Document (services, Routes.All, Core.Instance.Version);
        }

        public Routes Routes { get; private set; }

        public Codec Codec { get; private set; }

        /// <summary>
        /// The OpenAPI document, as JSON.
        /// </summary>
        public string OpenApi { get; private set; }
    }
}
