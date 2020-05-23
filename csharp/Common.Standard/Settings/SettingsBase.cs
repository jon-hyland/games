using Common.Standard.DataStorage;
using Common.Standard.Error;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;

namespace Common.Standard.Settings
{
    /// <summary>
    /// Base class for all simple settings classes.  Provides built-in JSON serialization/deserialization
    /// and data store persistence.
    /// </summary>
    public abstract class SettingsBase
    {
        //private
        private readonly string _key = null;
        private readonly DataStore _dataStore = null;
        private static readonly DefaultContractResolver _contractResolver = new DefaultContractResolver { NamingStrategy = new CamelCaseNamingStrategy() };
        private static readonly JsonSerializerSettings _serializerSettings = new JsonSerializerSettings { ContractResolver = _contractResolver, Formatting = Formatting.Indented };

        //public
        public string Key => _key;

        /// <summary>
        /// Static constructor.
        /// </summary>
        static SettingsBase()
        {
            _serializerSettings.Converters.Add(new IPAddressConverter());
            _serializerSettings.Converters.Add(new IPEndPointConverter());
            _serializerSettings.Converters.Add(new PhysicalAddressConverter());
        }

        /// <summary>
        /// Class constructor (JSON deserialization and/or constructor specified key).
        /// </summary>
        [JsonConstructor]
        public SettingsBase(string key)
        {
            if (String.IsNullOrWhiteSpace(key))
                key = GetType().GetCustomAttribute<SettingPropsAttribute>(true)?.Key ?? null;
            if (String.IsNullOrWhiteSpace(key))
                throw new Exception($"Settings class {GetType().Name} is missing key");
            _key = key;
            _dataStore = DataStore.CreateDefault(key);
        }

        /// <summary>
        /// Class constructor (attribute specified key).
        /// </summary>
        [JsonConstructor]
        public SettingsBase()
        {
            string key = GetType().GetCustomAttribute<SettingPropsAttribute>(true)?.Key ?? null;
            if (String.IsNullOrWhiteSpace(key))
                throw new Exception($"Settings class {GetType().Name} is missing key");
            _key = key;
            _dataStore = DataStore.CreateDefault(key);
        }

        /// <summary>
        /// Serializes class to JSON.
        /// </summary>
        public string ToJson()
        {
            string json = JsonConvert.SerializeObject(this, _serializerSettings);
            return json;
        }

        /// <summary>
        /// Deserializes class from JSON.
        /// </summary>
        public static T FromJson<T>(string json) where T : SettingsBase
        {
            T obj = JsonConvert.DeserializeObject<T>(json, _serializerSettings);
            return obj;
        }

        /// <summary>
        /// Serializes class to JSON, and saves to datastore.
        /// </summary>
        public void Save()
        {
            string json = ToJson();
            _dataStore.Write("Settings", json);
        }

        /// <summary>
        /// Loads from datastore, and deserializes JSON to class.
        /// </summary>
        public static T Load<T>(string key = null) where T : SettingsBase
        {
            bool noKey = String.IsNullOrWhiteSpace(key);
            if (String.IsNullOrWhiteSpace(key))
                key = typeof(T).GetCustomAttribute<SettingPropsAttribute>(true)?.Key ?? null;
            if (String.IsNullOrWhiteSpace(key))
                throw new Exception($"Settings class {typeof(T).Name} is missing key");

            try
            {
                DataStore dataStore = DataStore.CreateDefault(key);
                string json = dataStore.Read("Settings");
                if (!String.IsNullOrWhiteSpace(json))
                    return FromJson<T>(json);
            }
            catch (Exception ex)
            {
                ErrorHandler.LogError(new Exception($"Error loading or parsing settings [key: {key}]", ex));
            }

            if (noKey)
                return (T)Activator.CreateInstance(typeof(T));
            return (T)Activator.CreateInstance(typeof(T), new object[] { key });
        }

        /// <summary>
        /// Deletes settings from datastore.
        /// </summary>
        public void Delete()
        {
            _dataStore.Delete("Settings");
        }

        #region Converters

        /// <summary>
        /// Custom JSON converter for IPAddress.
        /// </summary>
        private class IPAddressConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                return (objectType == typeof(IPAddress));
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                writer.WriteValue(value.ToString());
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                return IPAddress.Parse((string)reader.Value);
            }
        }

        /// <summary>
        /// Custom JSON converter for IPEndPoint.
        /// </summary>
        private class IPEndPointConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                return (objectType == typeof(IPEndPoint));
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                IPEndPoint ep = (IPEndPoint)value;
                JObject jo = new JObject
                {
                    { "Address", JToken.FromObject(ep.Address, serializer) },
                    { "Port", ep.Port }
                };
                jo.WriteTo(writer);
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                JObject jo = JObject.Load(reader);
                IPAddress address = jo["Address"].ToObject<IPAddress>(serializer);
                int port = (int)jo["Port"];
                return new IPEndPoint(address, port);
            }
        }

        /// <summary>
        /// Custom JSON converter for PhysicalAddress.
        /// </summary>
        private class PhysicalAddressConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                return (objectType == typeof(PhysicalAddress));
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                writer.WriteValue(value.ToString());
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                return PhysicalAddress.Parse((string)reader.Value);
            }
        }

        #endregion

    }
}
