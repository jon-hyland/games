using Newtonsoft.Json;

namespace Common.Json
{
    /// <summary>
    /// Simple class to serialize objects to string (JSON format), and back again.
    /// </summary>
    public static class JsonSerialization
    {
        public static string Serialize(object value, bool typeNameHandling = false)
        {
            if (value == null)
                return null;
            string json = JsonConvert.SerializeObject(value, Formatting.Indented, new JsonSerializerSettings
            {
                TypeNameHandling = typeNameHandling ? TypeNameHandling.All : TypeNameHandling.None
            });
            return json;
        }

        public static object Deserialize(string json, bool typeNameHandling = false)
        {
            object value = JsonConvert.DeserializeObject(json, new JsonSerializerSettings
            {
                TypeNameHandling = typeNameHandling ? TypeNameHandling.All : TypeNameHandling.None
            });
            return value;
        }
    }
}
