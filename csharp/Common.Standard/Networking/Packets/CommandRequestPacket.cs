﻿using System;
using System.Net;

namespace Common.Standard.Networking.Packets
{
    /// <summary>
    /// Represents a TCP packet used to send command requests.  Target should reply with a response.
    /// </summary>
    public class CommandRequestPacket : PacketBase
    {
        //private
        private readonly CommandType _commandType;
        private readonly int _sequence;
        private readonly ushort _retryAttempt;
        private readonly uint _timeoutMs;
        private readonly byte[] _data;


        //public
        public CommandType CommandType => _commandType;
        public int Sequence => _sequence;
        public ushort RetryAttempt => _retryAttempt;
        public uint TimeoutMs => _timeoutMs;
        public byte[] Data => _data;

        /// <summary>
        /// Class constructor.
        /// </summary>
        public CommandRequestPacket(string gameTitle, Version gameVersion, IPAddress sourceIP, IPAddress destinationIP,
            string playerName, CommandType commandType, int sequence, ushort retryAttempt, uint timeoutMs, byte[] data)
            : base(PacketType.CommandRequest, gameTitle, gameVersion, sourceIP, destinationIP, playerName)
        {
            _commandType = commandType;
            _sequence = sequence;
            _retryAttempt = retryAttempt;
            _timeoutMs = timeoutMs;
            _data = data ?? new byte[0];
        }

        /// <summary>
        /// Class constructor.
        /// </summary>
        public CommandRequestPacket(PacketParser parser)
            : base(PacketType.CommandRequest, parser)
        {
            _commandType = (CommandType)parser.GetUInt16();
            _sequence = parser.GetInt32();
            _retryAttempt = parser.GetUInt16();
            _timeoutMs = parser.GetUInt32();
            _data = parser.GetBytes();
        }

        /// <summary>
        /// Adds instance bytes to packet builder.
        /// </summary>
        protected override void AddInstanceBytes(PacketBuilder builder)
        {
            builder.AddUInt16((ushort)_commandType);
            builder.AddInt32(_sequence);
            builder.AddUInt16(_retryAttempt);
            builder.AddUInt32(_timeoutMs);
            builder.AddBytes(_data);
        }

    }
}
