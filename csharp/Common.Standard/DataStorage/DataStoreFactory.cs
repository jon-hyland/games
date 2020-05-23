namespace Common.Standard.DataStorage
{
    /// <summary>
    /// Instanciates a data store of specified type.
    /// </summary>
    public class DataStoreFactory
    {
        public static IDataStore Create(DataStoreType type, string location)
        {
            switch (type)
            {
                case DataStoreType.Sqlite:
                    //return new SqliteDataStore(location);
                    return null;
                case DataStoreType.Disk:
                    return new DiskDataStore(location);
                default:
                    return null;
            }
        }
    }
}
