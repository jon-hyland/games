using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Common.Standard.Networking
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
        /// Adds two-dimensional Byte[] (ushort length + ushort length + byte array).
        /// </summary>
        public void AddBytes2D(byte[,] value)
        {
            _bytes.AddRange(BitConverter.GetBytes((ushort)value.GetLength(0)));
            _bytes.AddRange(BitConverter.GetBytes((ushort)value.GetLength(1)));
            byte[] bytes = new byte[value.Length];
            Buffer.BlockCopy(value, 0, bytes, 0, value.Length);
            _bytes.AddRange(bytes);
        }

        /// <summary>
        /// Adds Boolean (as byte).
        /// </summary>
        public void AddBoolean(bool value)
        {
            _bytes.Add((byte)(value ? 1 : 0));
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
            byte[] bytes = Encoding.UTF8.GetBytes(value ?? String.Empty);
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

        /// <summary>
        /// Builds packet from list of supoprted types and returns bytes as a single operation.
        /// </summary>
        public static byte[] ToBytes(IEnumerable<object> values)
        {
            PacketBuilder builder = new PacketBuilder();
            foreach (object value in values)
            {
                if (value is byte b)
                    builder.AddByte(b);
                else if (value is byte[] bs)
                    builder.AddBytes(bs);
                else if (value is byte[,] bs2)
                    builder.AddBytes2D(bs2);
                else if (value is bool bl)
                    builder.AddBoolean(bl);
                else if (value is short i16)
                    builder.AddInt16(i16);
                else if (value is ushort ui16)
                    builder.AddUInt16(ui16);
                else if (value is int i32)
                    builder.AddInt32(i32);
                else if (value is uint ui32)
                    builder.AddUInt32(ui32);
                else if (value is long i64)
                    builder.AddInt64(i64);
                else if (value is ulong ui64)
                    builder.AddUInt64(ui64);
                else if (value is string str)
                    builder.AddString(str);
                else if (value is Version ver)
                    builder.AddVersion(ver);
                else if (value is IPAddress ip)
                    builder.AddIPAddress(ip);
                else
                    throw new Exception("Data type not supported");
            }
            return builder.ToBytes();
        }

    }
}
