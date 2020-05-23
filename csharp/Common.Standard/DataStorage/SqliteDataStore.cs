//using System;
//using System.Collections.Generic;
//using System.Data;
//using System.IO;
//using System.Linq;

//namespace Common.Standard.DataStorage
//{
//    /// <summary>
//    /// Implements the simple IDataStore interface, leveraging SQLite as a key+value data store.
//    /// </summary>
//    public class SqliteDataStore : IDataStore
//    {
//        //private
//        private readonly string _connectString;

//        /// <summary>
//        /// Class constructor.. creates database file and table if either does not exist.
//        /// </summary>
//        public SqliteDataStore(string databaseFile)
//        {
//            _connectString = String.Format("Data Source={0};Version=3;", databaseFile);

//            string folder = Path.GetDirectoryName(databaseFile);
//            if ((!String.IsNullOrWhiteSpace(folder)) && (!Directory.Exists(folder)))
//                Directory.CreateDirectory(folder);

//            if (!File.Exists(databaseFile))
//            {
//                SQLiteConnection.CreateFile(databaseFile);
//                using (SQLiteConnection connection = new SQLiteConnection(_connectString))
//                {
//                    connection.Open();
//                    string sql = "CREATE TABLE IF NOT EXISTS data_dictionary(key TEXT PRIMARY KEY, value BLOB);";
//                    using (SQLiteCommand cmd = new SQLiteCommand(sql, connection))
//                    {
//                        cmd.ExecuteNonQuery();
//                    }
//                }
//            }
//        }

//        /// <summary>
//        /// Adds a string value to data store.
//        /// </summary>
//        public void Write(string key, string value)
//        {
//            using (SQLiteConnection connection = new SQLiteConnection(_connectString))
//            {
//                connection.Open();
//                string sql = "INSERT OR REPLACE INTO data_dictionary (key, value) VALUES (@key, @value);";
//                using (SQLiteCommand cmd = new SQLiteCommand(sql, connection))
//                {
//                    cmd.Parameters.AddWithValue("@key", key);
//                    cmd.Parameters.AddWithValue("@value", value);
//                    cmd.ExecuteNonQuery();
//                }
//            }
//        }

//        /// <summary>
//        /// Efficiently adds a collection of strings to data store.
//        /// </summary>
//        public void WriteMany(IEnumerable<Tuple<string, string>> keyValues)
//        {
//            using (SQLiteConnection connection = new SQLiteConnection(_connectString))
//            {
//                connection.Open();
//                using (SQLiteCommand cmd = new SQLiteCommand(connection))
//                {
//                    using (SQLiteTransaction trans = connection.BeginTransaction())
//                    {
//                        foreach (Tuple<string, string> kv in keyValues)
//                        {
//                            string sql = "INSERT OR REPLACE INTO data_dictionary (key, value) VALUES (@key, @value);";
//                            cmd.CommandText = sql;
//                            cmd.Parameters.Clear();
//                            cmd.Parameters.AddWithValue("@key", kv.Item1);
//                            cmd.Parameters.AddWithValue("@value", kv.Item2);
//                            cmd.ExecuteNonQuery();
//                        }
//                    }
//                }
//            }
//        }

//        /// <summary>
//        /// Fetches a string value from data store.
//        /// </summary>
//        public string Read(string key)
//        {
//            using (SQLiteConnection connection = new SQLiteConnection(_connectString))
//            {
//                connection.Open();
//                string sql = "SELECT value FROM data_dictionary WHERE key = @key;";
//                using (SQLiteCommand cmd = new SQLiteCommand(sql, connection))
//                {
//                    cmd.Parameters.AddWithValue("@key", key);
//                    using (SQLiteDataReader dr = cmd.ExecuteReader())
//                    {
//                        if (dr.Read())
//                        {
//                            string value = dr.GetString(0);
//                            return value;
//                        }
//                    }
//                }
//            }
//            return null;
//        }

//        /// <summary>
//        /// Deletes a string or object value from data store.
//        /// </summary>
//        public void Delete(string key)
//        {
//            using (SQLiteConnection connection = new SQLiteConnection(_connectString))
//            {
//                connection.Open();
//                string sql = "DELETE FROM data_dictionary WHERE key = @key;";
//                using (SQLiteCommand cmd = new SQLiteCommand(sql, connection))
//                {
//                    cmd.Parameters.AddWithValue("@key", key);
//                    cmd.ExecuteNonQuery();
//                }
//            }
//        }

//        /// <summary>
//        /// Serializes an object to binary and adds to data store.
//        /// </summary>
//        public void WriteObject(string key, object value)
//        {
//            byte[] bytes = BinarySerialization.Serialize(value);
//            using (SQLiteConnection connection = new SQLiteConnection(_connectString))
//            {
//                connection.Open();
//                string sql = "INSERT OR REPLACE INTO data_dictionary (key, value) VALUES (@key, @value);";
//                using (SQLiteCommand cmd = new SQLiteCommand(sql, connection))
//                {
//                    cmd.Parameters.AddWithValue("@key", key);
//                    cmd.Parameters.Add("@value", DbType.Binary, bytes.Length).Value = bytes;
//                    cmd.ExecuteNonQuery();
//                }
//            }
//        }

//        /// <summary>
//        /// Fetches binary value from data store and deserializes to object.
//        /// </summary>
//        public object ReadObject(string key)
//        {
//            byte[] bytes = null;
//            using (SQLiteConnection connection = new SQLiteConnection(_connectString))
//            {
//                connection.Open();
//                string sql = "SELECT value FROM data_dictionary WHERE key = @key;";
//                using (SQLiteCommand cmd = new SQLiteCommand(sql, connection))
//                {
//                    cmd.Parameters.AddWithValue("@key", key);
//                    using (SQLiteDataReader dr = cmd.ExecuteReader())
//                    {
//                        if (dr.Read())
//                            bytes = GetBytes(dr);
//                    }
//                }
//            }
//            object value = BinarySerialization.Deserialize(bytes);
//            return value;
//        }

//        /// <summary>
//        /// Returns a list of all currently stored keys.
//        /// </summary>
//        public List<string> GetAllKeys()
//        {
//            HashSet<string> keys = new HashSet<string>();
//            using (SQLiteConnection connection = new SQLiteConnection(_connectString))
//            {
//                connection.Open();
//                string sql = "SELECT key FROM data_dictionary;";
//                using (SQLiteCommand cmd = new SQLiteCommand(sql, connection))
//                {
//                    using (SQLiteDataReader dr = cmd.ExecuteReader())
//                    {
//                        while (dr.Read())
//                        {
//                            if ((!dr.IsDBNull(0)) && (!keys.Contains(dr.GetString(0))))
//                                keys.Add(dr.GetString(0));
//                        }
//                    }
//                }
//            }
//            return keys.ToList();
//        }

//        /// <summary>
//        /// Reads bytes from data reader, the proper way.
//        /// </summary>
//        private static byte[] GetBytes(SQLiteDataReader reader)
//        {
//            byte[] buffer = new byte[2048];
//            long bytesRead;
//            long fieldOffset = 0;
//            using (MemoryStream stream = new MemoryStream())
//            {
//                while ((bytesRead = reader.GetBytes(0, fieldOffset, buffer, 0, buffer.Length)) > 0)
//                {
//                    stream.Write(buffer, 0, (int)bytesRead);
//                    fieldOffset += bytesRead;
//                }
//                return stream.ToArray();
//            }
//        }
//    }
//}
