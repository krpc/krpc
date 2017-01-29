using UnityEngine;

namespace KRPC.Utils
{
    sealed class RectStorage : ConfigurationStorageNode
    {
        [Persistent]
        float x;
        [Persistent]
        float y;
        [Persistent]
        float width;
        [Persistent]
        float height;

        public static RectStorage FromRect (Rect rect)
        {
            var rectStorage = new RectStorage ();
            rectStorage.x = rect.x;
            rectStorage.y = rect.y;
            rectStorage.width = rect.width;
            rectStorage.height = rect.height;
            return rectStorage;
        }

        public Rect AsRect ()
        {
            return new Rect (x, y, width, height);
        }
    }
}
