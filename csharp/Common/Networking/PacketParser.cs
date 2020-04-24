using Common.Extensions;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Common.Networking
{
    /// <summary>
    /// Helper class to parse a network packet, retrieving and decoding various datatypes.
    /// </summary>
    public class PacketParser
    {
        //private
        private readonly List<byte> _bytes = null;

        /// <summary>
        /// Class constructor.
        /// </summary>
        public PacketParser(byte[] bytes)
        {
            if (bytes.Length < 4)
                throw new ArgumentException("Invalid packet length");
            ushort length = BitConverter.ToUInt16(bytes, 0);
            if (bytes.Length != length)
                throw new ArgumentException("Invalid packet length");
            _bytes = new List<byte>(bytes);
            _bytes.Dequeue(2);
        }

        /// <summary>
        /// Class constructor.
        /// </summary>
        public PacketParser(string str)
        {
            byte[] bytes = Convert.FromBase64String(str);
            if (bytes.Length < 4)
                throw new ArgumentException("Invalid packet length");
            ushort length = BitConverter.ToUInt16(bytes, 0);
            if (bytes.Length != length)
                throw new ArgumentException("Invalid packet length");
            _bytes = new List<byte>(bytes);
            _bytes.Dequeue(2);
        }

        /// <summary>
        /// Gets Byte.
        /// </summary>
        public byte GetByte()
        {
            byte value = _bytes.Dequeue();
            return value;
        }

        /// <summary>
        /// Gets Byte[].
        /// </summary>
        public byte[] GetBytes()
        {
            ushort length = BitConverter.ToUInt16(_bytes.Dequeue(2), 0);
            byte[] value = _bytes.Dequeue(length);
            return value;
        }

        /// <summary>
        /// Gets Int16.
        /// </summary>
        public short GetInt16()
        {
            short value = BitConverter.ToInt16(_bytes.Dequeue(2), 0);
            return value;
        }

        /// <summary>
        /// Gets UInt16.
        /// </summary>
        public ushort GetUInt16()
        {
            ushort value = BitConverter.ToUInt16(_bytes.Dequeue(2), 0);
            return value;
        }

        /// <summary>
        /// Gets Int32.
        /// </summary>
        public int GetInt32()
        {
            int value = BitConverter.ToInt32(_bytes.Dequeue(4), 0);
            return value;
        }

        /// <summary>
        /// Gets UInt32.
        /// </summary>
        public uint GetUInt32()
        {
            uint value = BitConverter.ToUInt32(_bytes.Dequeue(4), 0);
            return value;
        }

        /// <summary>
        /// Gets Int64.
        /// </summary>
        public long GetInt64()
        {
            long value = BitConverter.ToInt64(_bytes.Dequeue(8), 0);
            return value;
        }

        /// <summary>
        /// Gets UInt64.
        /// </summary>
        public ulong GetUInt64()
        {
            ulong value = BitConverter.ToUInt64(_bytes.Dequeue(8), 0);
            return value;
        }

        /// <summary>
        /// Gets String.
        /// </summary>
        public string GetString()
        {
            ushort length = BitConverter.ToUInt16(_bytes.Dequeue(2), 0);
            byte[] bytes = _bytes.Dequeue(length);
            string value = Encoding.UTF8.GetString(bytes);
            return value;
        }

        /// <summary>
        /// Gets Version.
        /// </summary>
        public Version GetVersion()
        {
            ushort major = BitConverter.ToUInt16(_bytes.Dequeue(2), 0);
            ushort minor = BitConverter.ToUInt16(_bytes.Dequeue(2), 0);
            ushort build = BitConverter.ToUInt16(_bytes.Dequeue(2), 0);
            ushort revision = BitConverter.ToUInt16(_bytes.Dequeue(2), 0);
            Version value = new Version(major, minor, build, revision);
            return value;
        }

        /// <summary>
        /// Gets IPAddress.
        /// </summary>
        public IPAddress GetIPAddress()
        {
            ushort length = BitConverter.ToUInt16(_bytes.Dequeue(2), 0);
            byte[] bytes = _bytes.Dequeue(length);
            IPAddress value = new IPAddress(bytes);
            return value;
        }


    }
}
