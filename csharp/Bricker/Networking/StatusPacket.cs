using Common.Networking;

namespace Bricker.Networking
{
    /// <summary>
    /// Represents a packet that gets sent to, or received by, the opponent.
    /// </summary>
    public class StatusPacket
    {
        //public
        public byte[,] Matrix { get; }
        public ushort Level { get; }
        public ushort Lines { get; }
        public ushort Score { get; }
        public ushort LinesSent { get; }

        /// <summary>
        /// Class constructor.
        /// </summary>
        public StatusPacket(byte[,] matrix, ushort level, ushort lines, ushort score, ushort linesSent)
        {
            Matrix = matrix;
            Level = level;
            Lines = lines;
            Score = score;
            LinesSent = linesSent;
        }

        /// <summary>
        /// Class constructor.
        /// </summary>
        public StatusPacket(byte[] bytes)
        {
            PacketParser parser = new PacketParser(bytes);
            Matrix = parser.GetBytes2D();
            Level = parser.GetUInt16();
            Lines = parser.GetUInt16();
            Score = parser.GetUInt16();
            LinesSent = parser.GetUInt16();
        }

        /// <summary>
        /// Serializes values to bytes.
        /// </summary>
        public byte[] ToBytes()
        {
            PacketBuilder builder = new PacketBuilder();
            builder.AddBytes2D(Matrix);
            builder.AddUInt16(Level);
            builder.AddUInt16(Lines);
            builder.AddUInt16(Score);
            builder.AddUInt16(LinesSent);
            byte[] bytes = builder.ToBytes();
            return bytes;
        }

    }
}
