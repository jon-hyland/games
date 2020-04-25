using System;
using System.Net;

namespace Common.Networking.Simple.Packets
{
    /// <summary>
    /// Represents a TCP packet used to respond to command requests.
    /// </summary>
    public class CommandResponsePacket : PacketBase
    {
        //private
        private readonly ushort _commandType;
        private readonly ushort _sequence;
        private readonly CommandResult _result;
        private readonly byte[] _data;

        //public
        public ushort CommandType => _commandType;
        public ushort Sequence => _sequence;
        public CommandResult Result => _result;
        public byte[] Data => _data;

        /// <summary>
        /// Class constructor.
        /// </summary>
        public CommandResponsePacket(string gameTitle, Version gameVersion, IPAddress sourceIP, IPAddress destinationIP,
            ushort destinationPort, ushort commandType, ushort sequence, CommandResult result, byte[] data)
            : base(PacketType.CommandResponse, gameTitle, gameVersion, sourceIP, destinationIP, destinationPort)
        {
            _commandType = commandType;
            _sequence = sequence;
            _result = result;
            _data = data ?? new byte[0];
        }

        /// <summary>
        /// Class constructor.
        /// </summary>
        public CommandResponsePacket(PacketParser parser)
            : base(PacketType.CommandResponse, parser)
        {
            _commandType = parser.GetUInt16();
            _sequence = parser.GetUInt16();
            _result = (CommandResult)parser.GetByte();
            _data = parser.GetBytes();
        }

        /// <summary>
        /// Adds instance bytes to packet builder.
        /// </summary>
        protected override void AddInstanceBytes(PacketBuilder builder)
        {
            builder.AddUInt16(_commandType);
            builder.AddUInt16(_sequence);
            builder.AddByte((byte)_result);
            builder.AddBytes(_data);
        }

    }
}
