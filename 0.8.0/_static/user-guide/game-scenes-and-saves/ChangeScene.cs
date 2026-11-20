using System.Threading;
using KRPC.Client;
using KRPC.Client.Services.KRPC;

class ChangeScene {
    public static void Main () {
        using (var connection = new Connection ()) {
            var krpc = connection.KRPC ();
            // [docs]
            krpc.GameScene = GameScene.SpaceCenter;
            while (krpc.GameScene != GameScene.SpaceCenter)
                Thread.Sleep (100);
            // [/docs]
        }
    }
}
