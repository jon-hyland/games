using System;
using System.Collections.Generic;

namespace Common.Standard.DataStorage
{
    public interface IDataStore
    {
        void Write(string key, string value);
        void WriteMany(IEnumerable<Tuple<string, string>> keyValues);
        string Read(string key);
        void Delete(string key);

        void WriteObject(string key, object value);
        object ReadObject(string key);

        List<string> GetAllKeys();
    }
}
