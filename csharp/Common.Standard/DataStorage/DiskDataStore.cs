using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace Common.Standard.DataStorage
{
    /// <summary>
    /// Implements the simple IDataStore interface, leveraging individual files on disk as a key+value data store.
    /// </summary>
    public class DiskDataStore : IDataStore
    {
        //private
        private readonly string _folder;
        private static readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

        /// <summary>
        /// Class constructor.
        /// </summary>
        public DiskDataStore(string folder)
        {
            _folder = folder;
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);
        }

        /// <summary>
        /// Adds a string value to data store.
        /// </summary>
        public void Write(string key, string value)
        {
            string file = GetFileName(key, true);
            _lock.EnterWriteLock();
            try
            {
                using (StreamWriter sw = new StreamWriter(file, false, Encoding.UTF8))
                {
                    sw.Write(value ?? "");
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Efficiently adds a collection of strings to data store.
        /// </summary>
        public void WriteMany(IEnumerable<Tuple<string, string>> keyValues)
        {
            foreach (Tuple<string, string> kv in keyValues)
            {
                Write(kv.Item1, kv.Item2);
            }
        }

        /// <summary>
        /// Fetches a string value from data store.
        /// </summary>
        public string Read(string key)
        {
            string file = GetFileName(key, true);
            _lock.EnterReadLock();
            try
            {
                if (!File.Exists(file))
                    return null;
                using (StreamReader sr = new StreamReader(file))
                {
                    string value = sr.ReadToEnd();
                    return value;
                }
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        /// <summary>
        /// Deletes a string or object value from data store.
        /// </summary>
        public void Delete(string key)
        {
            string file = GetFileName(key, true);
            _lock.EnterWriteLock();
            try
            {
                if (File.Exists(file))
                    File.Delete(file);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Serializes an object to binary and adds to data store.
        /// </summary>
        public void WriteObject(string key, object value)
        {
            byte[] bytes = BinarySerialization.Serialize(value);
            string file = GetFileName(key, false);
            _lock.EnterWriteLock();
            try
            {
                using (FileStream fs = new FileStream(file, FileMode.Create, FileAccess.Write, FileShare.Write))
                {
                    fs.Write(bytes, 0, bytes.Length);
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Fetches binary value from data store and deserializes to object.
        /// </summary>
        public object ReadObject(string key)
        {
            string file = GetFileName(key, true);
            byte[] bytes;
            _lock.EnterReadLock();
            try
            {
                if (!File.Exists(file))
                    return null;
                using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    bytes = new byte[fs.Length];
                    fs.Read(bytes, 0, (int)fs.Length);
                }
            }
            finally
            {
                _lock.ExitReadLock();
            }
            object value = BinarySerialization.Deserialize(bytes);
            return value;
        }

        /// <summary>
        /// Returns a list of all currently stored keys.
        /// </summary>
        public List<string> GetAllKeys()
        {
            HashSet<string> keys = new HashSet<string>();
            _lock.EnterReadLock();
            try
            {
                string[] files = Directory.GetFiles(_folder, "*.json");
                foreach (string file in files)
                {
                    string key = Path.GetFileNameWithoutExtension(file);
                    if (!keys.Contains(key))
                        keys.Add(key);
                }

                files = Directory.GetFiles(_folder, "*.binary");
                foreach (string file in files)
                {
                    string key = Path.GetFileNameWithoutExtension(file);
                    if (!keys.Contains(key))
                        keys.Add(key);
                }
            }
            finally
            {
                _lock.ExitReadLock();
            }
            return keys.ToList();
        }

        /// <summary>
        /// Uses key to create filename, using .json or .binary file extension depending on storage type.
        /// </summary>
        private string GetFileName(string key, bool isJson)
        {
            return Path.Combine(_folder, key + (isJson ? ".json" : ".binary"));
        }
    }
}
