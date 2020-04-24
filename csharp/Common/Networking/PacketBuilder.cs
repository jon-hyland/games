using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Common.Networking
{
    /// <summary>
    /// Helper class to construct a network packet, encoding and adding various datatypes.
    /// </summary>
    public class PacketBuilder
    {
        //private
        private readonly List<byte> _bytes = new List<byte>();

        //public
        public int Length => _bytes.Count;

        /// <summary>
        /// Class constructor.
        /// </summary>
        public PacketBuilder()
        {
        }

        /// <summary>
        /// Adds Byte.
        /// </summary>
        public void AddByte(byte value)
        {
            _bytes.Add(value);
        }

        /// <summary>
        /// Adds Byte[] (ushort value length + byte array).
        /// </summary>
        public void AddBytes(byte[] value)
        {
            _bytes.AddRange(BitConverter.GetBytes((ushort)value.Length));
            _bytes.AddRange(value);
        }

        /// <summary>
        /// Adds Int16.
        /// </summary>
        public void AddInt16(short value)
        {
            _bytes.AddRange(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Adds UInt16.
        /// </summary>
        public void AddUInt16(ushort value)
        {
            _bytes.AddRange(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Adds Int32.
        /// </summary>
        public void AddInt32(int value)
        {
            _bytes.AddRange(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Adds UInt32.
        /// </summary>
        public void AddUInt32(uint value)
        {
            _bytes.AddRange(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Adds Int64.
        /// </summary>
        public void AddInt64(long value)
        {
            _bytes.AddRange(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Adds UInt64.
        /// </summary>
        public void AddUInt64(ulong value)
        {
            _bytes.AddRange(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Adds String (ushort value length + string as utf-8 bytes).
        /// </summary>        
        public void AddString(string value)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(value);
            _bytes.AddRange(BitConverter.GetBytes((ushort)bytes.Length));
            _bytes.AddRange(bytes);
        }

        /// <summary>
        /// Adds Version.
        /// </summary>
        public void AddVersion(Version value)
        {
            _bytes.AddRange(BitConverter.GetBytes((ushort)value.Major));
            _bytes.AddRange(BitConverter.GetBytes((ushort)value.Minor));
            _bytes.AddRange(BitConverter.GetBytes((ushort)value.Build));
            _bytes.AddRange(BitConverter.GetBytes((ushort)value.Revision));
        }

        /// <summary>
        /// Adds IPAddress.
        /// </summary>
        public void AddIPAddress(IPAddress value)
        {
            byte[] bytes = value.GetAddressBytes();
            _bytes.AddRange(BitConverter.GetBytes((ushort)bytes.Length));
            _bytes.AddRange(bytes);
        }

        /// <summary>
        /// Returns packet as byte array.
        /// </summary>
        public byte[] ToBytes()
        {
            ushort length = (ushort)(_bytes.Count + 2);
            byte[] lengthBytes = BitConverter.GetBytes(length);
            _bytes.InsertRange(0, lengthBytes);
            return _bytes.ToArray();
        }

        /// <summary>
        /// Returns base-64 encoded version of packet bytes.
        /// </summary>
        public override string ToString()
        {
            byte[] bytes = ToBytes();
            string str = Convert.ToBase64String(bytes);
            return str;
        }


    }
}
