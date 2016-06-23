namespace KRPC.Utils
{
    abstract class ConfigurationStorageNode : IPersistenceLoad, IPersistenceSave
    {
        /// <summary>
        /// Override to provide custom behaviour before saving.
        /// </summary>
        protected virtual void BeforeSave ()
        {
        }

        /// <summary>
        /// Override to provide custom behaviour after loading.
        /// </summary>
        protected virtual void AfterLoad ()
        {
        }

        void IPersistenceLoad.PersistenceLoad ()
        {
            AfterLoad ();
        }

        void IPersistenceSave.PersistenceSave ()
        {
            BeforeSave ();
        }
    }
}
