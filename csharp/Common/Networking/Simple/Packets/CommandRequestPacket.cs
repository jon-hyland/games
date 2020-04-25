using System;
using System.Net;

namespace Common.Networking.Simple.Packets
{
    /// <summary>
    /// Represents a TCP packet used to send command requests.  Target should reply with a response.
    /// </summary>
    public class CommandRequestPacket : PacketBase
    {
        //private
        private readonly ushort _commandType;
        private readonly ushort _sequence;
        private readonly ushort _retryAttempt;
        private readonly byte[] _data;


        //public
        public ushort CommandType => _commandType;
        public ushort Sequence => _sequence;
        public ushort RetryAttempt => _retryAttempt;
        public byte[] Data => _data;

        /// <summary>
        /// Class constructor.
        /// </summary>
        public CommandRequestPacket(string gameTitle, Version gameVersion, IPAddress sourceIP, IPAddress destinationIP,
            ushort destinationPort, ushort commandType, ushort sequence, ushort retryAttempt, byte[] data)
            : base(PacketType.CommandRequest, gameTitle, gameVersion, sourceIP, destinationIP, destinationPort)
        {
            _commandType = commandType;
            _sequence = sequence;
            _retryAttempt = retryAttempt;
            _data = data ?? new byte[0];
        }

        /// <summary>
        /// Class constructor.
        /// </summary>
        public CommandRequestPacket(PacketParser parser)
            : base(PacketType.CommandRequest, parser)
        {
            _commandType = parser.GetUInt16();
            _sequence = parser.GetUInt16();
            _retryAttempt = parser.GetUInt16();
            _data = parser.GetBytes();
        }

        /// <summary>
        /// Adds instance bytes to packet builder.
        /// </summary>
        protected override void AddInstanceBytes(PacketBuilder builder)
        {
            builder.AddUInt16(_commandType);
            builder.AddUInt16(_sequence);
            builder.AddUInt16(_retryAttempt);
            builder.AddBytes(_data);
        }

    }
}
