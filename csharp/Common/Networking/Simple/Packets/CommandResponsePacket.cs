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

        //public
        public ushort CommandType => _commandType;
        public ushort Sequence => _sequence;
        public CommandResult Result => _result;

        /// <summary>
        /// Class constructor.
        /// </summary>
        public CommandResponsePacket(string gameTitle, Version gameVersion, IPAddress sourceIP, IPAddress destinationIP,
            ushort destinationPort, ushort commandType, ushort sequence, CommandResult result)
            : base(PacketType.CommandResponse, gameTitle, gameVersion, sourceIP, destinationIP, destinationPort)
        {
            _commandType = commandType;
            _sequence = sequence;
            _result = result;
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
        }

        /// <summary>
        /// Adds instance bytes to packet builder.
        /// </summary>
        protected override void AddInstanceBytes(PacketBuilder builder)
        {
            builder.AddUInt16(_commandType);
            builder.AddUInt16(_sequence);
            builder.AddByte((byte)_result);
        }

        /// <summary>
        /// The result of a command request.
        /// </summary>
        public enum CommandResult : byte
        {
            Unspecified = 0,
            Timeout = 1,
            Reject = 2,
            Accept = 3
        }

    }
}
