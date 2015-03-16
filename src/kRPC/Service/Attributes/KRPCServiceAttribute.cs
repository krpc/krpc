using System;

namespace KRPC.Service.Attributes
{
    [AttributeUsage (AttributeTargets.Class)]
    public class KRPCServiceAttribute : Attribute
    {
        public string Name { get; set; }

        public GameScene GameScene { get; set; }

        public KRPCServiceAttribute ()
        {
            GameScene = GameScene.All;
        }
    }
}
