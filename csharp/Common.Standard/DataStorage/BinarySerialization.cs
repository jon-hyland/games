using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Common.Standard.DataStorage
{
    /// <summary>
    /// Simple class to serialize objects to binary (byte array), and back again.
    /// Result may be dependant on .NET version, x32 vs x64, etc.
    /// </summary>
    public static class BinarySerialization
    {
        /// <summary>
        /// Serialize object to binary.
        /// </summary>
        public static byte[] Serialize(object value)
        {
            if (value == null)
                return null;

            var formatter = new BinaryFormatter();
            using (var stream = new MemoryStream())
            {
                formatter.Serialize(stream, value);
                return stream.ToArray();
            }
        }

        /// <summary>
        /// Deserialize object from binary.
        /// </summary>
        public static object Deserialize(byte[] bytes)
        {
            if ((bytes == null) || (bytes.Length <= 0))
                return null;

            var formatter = new BinaryFormatter();
            using (var stream = new MemoryStream())
            {
                stream.Write(bytes, 0, bytes.Length);
                stream.Seek(0, SeekOrigin.Begin);
                object value = formatter.Deserialize(stream);
                return value;
            }
        }
    }
}
