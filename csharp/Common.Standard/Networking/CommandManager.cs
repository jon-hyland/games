using Common.Standard.Networking.Packets;
using Common.Standard.Threading;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Common.Standard.Networking
{
    /// <summary>
    /// Manages commands, sent from one game instance and replied to (or timeout) from other instance.
    /// Most commands from the sender will be considered a blocking call.
    /// </summary>
    public class CommandManager : IDisposable
    {
        //private
        private readonly List<Command> _commands;
        private readonly SimpleTimer _timer;

        /// <summary>
        /// Class constructor.
        /// </summary>
        public CommandManager()
        {
            _commands = new List<Command>();
            _timer = new SimpleTimer(Timer_Callback, 1000);
        }

        /// <summary>
        /// Dispose of resources.
        /// </summary>
        public void Dispose()
        {
            _timer.Dispose();
        }

        /// <summary>
        /// Records when a command request packet was sent, to create a state that can be tied back to a response.
        /// </summary>
        public void RequestSent(CommandRequestPacket packet)
        {
            lock (_commands)
            {
                Command command = _commands
                    .Where(c => c.Sequence == packet.Sequence)
                    .OrderBy(c => c.Elapsed)
                    .FirstOrDefault();

                if (command != null)
                {
                    command.RetryAttempt = packet.RetryAttempt;
                }
                else
                {
                    command = new Command(packet.CommandType, packet.Sequence, TimeSpan.FromMilliseconds(packet.TimeoutMs));
                    _commands.Add(command);
                }
            }
        }

        /// <summary>
        /// Records when a command response packet was received, to update the cooresponding state and release another blocking call.
        /// </summary>
        public void ResponseReceived(CommandResponsePacket packet)
        {
            lock (_commands)
            {
                Command command = _commands
                    .Where(c => c.Sequence == packet.Sequence)
                    .OrderBy(c => c.Elapsed)
                    .FirstOrDefault();

                if ((packet.Code == ResultCode.Accept) || (packet.Code == ResultCode.Reject))
                {
                    command.Code = packet.Code;
                    command.ResponsePacket = packet;
                }
                else if (packet.Code == ResultCode.Timeout)
                {
                    command.Code = ResultCode.Timeout;
                }
                else
                {
                    command.Code = ResultCode.Error;
                }
            }
        }

        /// <summary>
        /// Returns the status of the specified command.
        /// </summary>
        public CommandResult GetCommandStatus(ushort sequence)
        {
            lock (_commands)
            {
                Command command = _commands
                    .Where(c => c.Sequence == sequence)
                    .OrderBy(c => c.Elapsed)
                    .FirstOrDefault();

                if (command != null)
                {
                    if ((command.IsTimedOut) || (command.IsExpired))
                        return new CommandResult(ResultCode.Timeout);
                    return new CommandResult(command.Code, command.ResponsePacket);
                }
                else
                {
                    return new CommandResult(ResultCode.Error);
                }
            }
        }

        /// <summary>
        /// Fired by timer, removes expired commands.
        /// </summary>
        private void Timer_Callback()
        {
            lock (_commands)
            {
                List<Command> expired = _commands
                    .Where(c => c.IsExpired)
                    .ToList();

                expired.ForEach(c => _commands.Remove(c));
            }
        }

        /// <summary>
        /// Stores internal details about a command.
        /// </summary>
        private class Command
        {
            public CommandType CommandType { get; }
            public ushort Sequence { get; }
            public ResultCode Code { get; set; }
            public CommandResponsePacket ResponsePacket { get; set; }
            public ushort RetryAttempt { get; set; }
            public TimeSpan Timeout { get; }
            public DateTime StartTime { get; }
            public TimeSpan Elapsed => DateTime.Now - StartTime;
            public bool IsTimedOut => Elapsed > Timeout;
            public bool IsExpired => Elapsed > Timeout.Add(TimeSpan.FromSeconds(60));

            public Command(CommandType commandType, ushort sequence, TimeSpan timeout)
            {
                CommandType = commandType;
                Sequence = sequence;
                Code = ResultCode.Unspecified;
                ResponsePacket = null;
                RetryAttempt = 0;
                Timeout = timeout;
                StartTime = DateTime.Now;
            }
        }
    }


}
