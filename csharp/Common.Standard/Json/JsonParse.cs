using Newtonsoft.Json;
using System;
using System.Net;

namespace Common.Standard.Json
{
    /// <summary>
    /// Provides safe methods to convert data from dynamic objects parsed from JSON.
    /// </summary>
    public static class JsonParse
    {
        /// <summary>
        /// Serializes object to JSON.
        /// </summary>
        public static string Serialize(object value, bool typeHandling = false)
        {
            if (value == null)
                return null;
            string json = JsonConvert.SerializeObject(value, Formatting.Indented, new JsonSerializerSettings
            {
                TypeNameHandling = typeHandling ? TypeNameHandling.All : TypeNameHandling.None
            });
            return json;
        }

        /// <summary>
        /// Deserializes JSON to dynamic object.
        /// </summary>
        public static dynamic Deserialize(string json, bool typeHandling = false)
        {
            dynamic value = JsonConvert.DeserializeObject(json, new JsonSerializerSettings
            {
                TypeNameHandling = typeHandling ? TypeNameHandling.All : TypeNameHandling.None
            });
            return value;
        }

        /// <summary>
        /// Gets a string.
        /// </summary>
        public static string GetString(dynamic data, string def = "")
        {
            try
            {
                if (data != null)
                    return Convert.ToString(data);
            }
            catch
            {
            }
            return def;
        }

        /// <summary>
        /// Gets a boolean.
        /// </summary>
        public static bool GetBoolean(dynamic data, bool def = default)
        {
            try
            {
                if (data != null)
                {
                    if (data is String)
                        return String.Equals(Convert.ToString(data), "true", StringComparison.CurrentCultureIgnoreCase);
                    if (data is Boolean)
                        return Convert.ToBoolean(data);
                    return data == 1;
                }
            }
            catch
            {
            }
            return def;
        }

        /// <summary>
        /// Gets an integer.
        /// </summary>
        public static int GetInt(dynamic data, int def = default)
        {
            try
            {
                if (data != null)
                    return Convert.ToInt32(data);
            }
            catch
            {
            }
            return def;
        }

        /// <summary>
        /// Gets a long.
        /// </summary>
        public static long GetLong(dynamic data, long def = default)
        {
            try
            {
                if (data != null)
                    return Convert.ToInt64(data);
            }
            catch
            {
            }
            return def;
        }

        /// <summary>
        /// Gets a short.
        /// </summary>
        public static short GetShort(dynamic data, short def = default)
        {
            try
            {
                if (data != null)
                    return Convert.ToInt16(data);
            }
            catch
            {
            }
            return def;
        }

        /// <summary>
        /// Gets an unsigned short.
        /// </summary>
        public static ushort GetUShort(dynamic data, ushort def = default)
        {
            try
            {
                if (data != null)
                    return Convert.ToUInt16(data);
            }
            catch
            {
            }
            return def;
        }

        /// <summary>
        /// Gets a double.
        /// </summary>
        public static double GetDouble(dynamic data, double def = default)
        {
            try
            {
                if (data != null)
                    return Convert.ToDouble(data);
            }
            catch
            {
            }
            return def;
        }

        /// <summary>
        /// Gets a decimal.
        /// </summary>
        public static decimal GetDecimal(dynamic data, decimal def = default)
        {
            try
            {
                if (data != null)
                    return Convert.ToDecimal(data);
            }
            catch
            {
            }
            return def;
        }

        /// <summary>
        /// Gets a date time.
        /// </summary>
        public static DateTime GetDateTime(dynamic data, DateTime def = default)
        {
            try
            {
                if (data != null)
                    return DateTime.Parse(GetString(data));
            }
            catch
            {
            }
            return def;
        }

        /// <summary>
        /// Gets a time span.
        /// </summary>
        public static TimeSpan GetTimeSpan(dynamic data, TimeSpan def = default)
        {
            try
            {
                if (data != null)
                    return TimeSpan.Parse(GetString(data));
            }
            catch
            {
            }
            return def;
        }

        /// <summary>
        /// Gets an IP address.
        /// </summary>
        public static IPAddress GetIPAddress(dynamic data, IPAddress def = default)
        {
            try
            {
                if (data != null)
                    return IPAddress.Parse(GetString(data));
            }
            catch
            {
            }
            return def;
        }






    }
}
