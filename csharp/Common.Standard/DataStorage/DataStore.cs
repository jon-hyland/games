using System;
using System.Collections.Generic;
using System.Linq;

namespace Common.Standard.DataStorage
{
    /// <summary>
    /// Generic wrapper around an IDataStore object.
    /// </summary>
    public class DataStore
    {
        //static
        private static DataStoreType _defaultType;
        private static string _defaultLocation;

        //private
        private readonly string _keyPrefix;
        private readonly IDataStore _dataStore;

        //public
        public IDataStore Inner => _dataStore;

        /// <summary>
        /// Class constructor.
        /// </summary>
        public DataStore(string keyPrefix, DataStoreType type, string location)
        {
            _keyPrefix = keyPrefix;
            _dataStore = DataStoreFactory.Create(type, location);
        }

        /// <summary>
        /// Sets the static defaults for creating a data store.
        /// </summary>
        public static void Initialize(DataStoreType defaultType, string defaultLocation)
        {
            _defaultType = defaultType;
            _defaultLocation = defaultLocation;
        }

        /// <summary>
        /// Creates a data store wrapper of default type.
        /// </summary>
        public static DataStore CreateDefault(string keyPrefix)
        {
            return new DataStore(keyPrefix, _defaultType, _defaultLocation);
        }

        /// <summary>
        /// Creates a standard-looking datakey based on keyPrefix and specified item key.
        /// </summary>
        private string CreateKey(string key)
        {
            return $"{_keyPrefix}.{key}";
        }

        /// <summary>
        /// Writes a string-based key/value pair to the data store.
        /// </summary>
        public void Write(string key, string value)
        {
            try
            {
                _dataStore.Write(CreateKey(key), value);
            }
            catch (Exception ex)
            {
                throw new Exception("Unable to write to data store", ex);
            }
        }

        /// <summary>
        /// Efficiently writes a collection of string-based key/value pairs to the data store.
        /// </summary>
        public void WriteMany(IEnumerable<Tuple<string, string>> keyValues)
        {
            try
            {
                var kv = keyValues.Select(k => new Tuple<string, string>(CreateKey(k.Item1), k.Item2));
                _dataStore.WriteMany(kv);
            }
            catch (Exception ex)
            {
                throw new Exception("Unable to write-many to data store", ex);
            }
        }

        /// <summary>
        /// Reads a string-based key/value pair from the data store.
        /// </summary>
        public string Read(string key)
        {
            try
            {
                return _dataStore.Read(CreateKey(key));
            }
            catch (Exception ex)
            {
                throw new Exception("Unable to read from data store", ex);
            }
        }

        /// <summary>
        /// Deletes a string or object value from data store.
        /// </summary>
        public void Delete(string key)
        {
            try
            {
                _dataStore.Delete(CreateKey(key));
            }
            catch (Exception ex)
            {
                throw new Exception("Unable to delete from data store", ex);
            }
        }

        /// <summary>
        /// Returns a list of all keys stored in the data store.
        /// </summary>
        /// <returns></returns>
        public List<string> GetAllKeys()
        {
            try
            {
                return _dataStore.GetAllKeys();
            }
            catch (Exception ex)
            {
                throw new Exception("Unable to get all keys from data store", ex);
            }
        }

        /// <summary>
        /// Creates a default client and performs a test write+read operation, returning the result (success / fail).
        /// </summary>
        public static bool TestReadWrite(out string errorMessage)
        {
            errorMessage = null;
            try
            {
                DataStore ds = CreateDefault("Machine.DataStore");
                ds.Write("TestReadWrite", "TestReadWrite");
                string value = ds.Read("TestReadWrite");
                if (!value.Equals("TestReadWrite"))
                    throw new Exception("Values do not match");
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.GetType().ToString() + ": " + ex.Message;
            }
            return false;
        }

    }
}
