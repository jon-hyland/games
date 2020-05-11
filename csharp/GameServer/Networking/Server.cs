using Common.Standard.Error;
using Common.Standard.Game;
using Common.Standard.Logging;
using Common.Standard.Networking;
using Common.Standard.Networking.Packets;
using Common.Standard.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace GameServer.Networking
{
    public class Server
    {
        //private
        private readonly IPAddress _ip;
        private readonly TcpListener _listener;
        private readonly List<Client> _clients;
        private readonly List<Player> _players;
        private readonly List<Session> _sessions;
        private readonly Thread _listenThread;
        private readonly SimpleTimer _timer;

        #region Constructor

        /// <summary>
        /// Class constructor.
        /// </summary>
        public Server(IPAddress ip, int port)
        {
            _ip = ip;
            _listener = new TcpListener(ip, port);
            _clients = new List<Client>();
            _players = new List<Player>();
            _sessions = new List<Session>();
            _listenThread = new Thread(ListenThread);
            _listenThread.IsBackground = true;
            _timer = new SimpleTimer(Timer_Callback, 1000);
        }

        /// <summary>
        /// Starts the server.
        /// </summary>
        public void Start()
        {
            Log.Write("Starting TCP server..");
            _listener.Start();
            _listenThread.Start();
        }

        #endregion

        #region TCP Listener

        /// <summary>
        /// Listen thread, loops waiting for new client connections.
        /// </summary>
        private void ListenThread()
        {
            while (true)
            {
                try
                {
                    //create new game client (blocking call)
                    Client client = new Client(_listener.AcceptTcpClient());

                    //message
                    Log.Write($"Accepted TCP connection from '{client.RemoteIP}'..");

                    //add to list
                    lock (_clients)
                        _clients.Add(client);

                    //wire events
                    client.PacketReceived += PacketReceived;
                }
                catch (Exception ex)
                {
                    ErrorHandler.LogError(ex);
                }
            }
        }

        #endregion

        #region Players

        /// <summary>
        /// Returns list of matching players.
        /// </summary>
        private List<Player> GetPlayers(string gameTitle, Version gameVersion)
        {
            lock (_players)
            {
                RemoveExpiredPlayers();
                return _players
                    .Where(p => p.GameTitle.Equals(gameTitle))
                    .Where(p => p.GameVersion.Equals(gameVersion))
                    .OrderByDescending(p => p.LastDiscovery)
                    .ToList();
            }
        }

        /// <summary>
        /// Adds or updates a discovered player.
        /// </summary>
        private void AddOrUpdatePlayer(Player player)
        {
            lock (_players)
            {
                Player existing = _players
                    .Where(p => p.UniqueKey == player.UniqueKey)
                    .FirstOrDefault();

                if (existing != null)
                {
                    if (existing.Name != player.Name)
                    {
                        Log.Write($"Changing player name '{existing.Name}' to '{player.Name}'..");
                        existing.Name = player.Name;
                    }
                    TimeSpan sinceLastDiscovery = DateTime.Now - existing.LastDiscovery;
                    if (sinceLastDiscovery.TotalSeconds > 60)
                        existing.LastDiscovery = DateTime.Now;
                }
                else
                {
                    Log.Write($"Adding new player '{player.Name}' at '{player.IP}' playing '{player.GameTitle} v{player.GameVersion}'..");
                    _players.Add(player);
                }

                RemoveExpiredPlayers();
            }
        }

        /// <summary>
        /// Removes any players that haven't checked in within the past 5 minutes,
        /// or no longer have an established TCP connection.
        /// </summary>
        private void RemoveExpiredPlayers()
        {
            lock (_players)
            {
                List<Player> expired = _players
                    .Where(p => p.TimeSinceLastDiscovery.TotalMinutes > 5)
                    .Where(p => p.Client?.IsConnected != true)
                    .ToList();
                foreach (Player p in expired)
                {
                    Log.Write($"Removing expired/disconnected player [ip={p.IP}, name={p.Name}]..");
                    _players.Remove(p);
                }
            }
        }

        #endregion

        #region Sessions

        /// <summary>
        /// Creates a new session.
        /// </summary>
        private void CreateSession(ushort sessionID, Player player1, Player player2)
        {
            lock (_sessions)
                _sessions.Add(new Session(sessionID, player1, player2));
        }

        #endregion

        #region Incoming Packets

        /// <summary>
        /// Fired when packet received from a client.
        /// </summary>
        private void PacketReceived(Client client, PacketBase packet)
        {
            try
            {
                //heartbeat
                if (packet is HeartbeatPacket hp)
                {
                    Player player = Player.FromPacket(hp, client);
                    if (player != null)
                        AddOrUpdatePlayer(player);
                }

                //command request
                else if (packet is CommandRequestPacket req)
                {
                    switch (req.CommandType)
                    {
                        //get players
                        case CommandType.GetPlayers:
                            Answer_GetPlayers(client, req);
                            break;
                    }
                }

            }
            catch (Exception ex)
            {
                Log.Write("PacketReceived: Error processing client packet");
                ErrorHandler.LogError(ex);
            }
        }

        /// <summary>
        /// Answers a 'GetPlayers' request, responds with a list of connected players (of same game).
        /// </summary>
        private void Answer_GetPlayers(Client client, CommandRequestPacket packet)
        {
            try
            {
                //get requesting player
                Player player = Player.FromPacket(packet, null);

                //get list of connected players (not requesting player)
                List<Player> otherPlayers = GetPlayers(player.GameTitle, player.GameVersion)
                    .Where(p => p.UniqueKey != player.UniqueKey)
                    .ToList();

                //message
                Log.Write($"Responsing to 'GetPlayers' request from player '{player.Name}' with {otherPlayers.Count} other player..");

                //serialize list to bytes
                PacketBuilder builder = new PacketBuilder();
                builder.AddUInt16((ushort)otherPlayers.Count);
                foreach (Player p in otherPlayers)
                    builder.AddBytes(p.ToBytes());

                //create packet
                CommandResponsePacket response = new CommandResponsePacket(
                    gameTitle: player.GameTitle,
                    gameVersion: player.GameVersion,
                    sourceIP: _ip,
                    destinationIP: player.IP,
                    destinationPort: 0,
                    playerName: player.Name,
                    commandType: CommandType.GetPlayers,
                    sequence: packet.Sequence,
                    result: new CommandResult(
                        code: ResultCode.Accept,
                        data: builder.ToBytes()));

                //send response
                client.SendPacket(response);
            }
            catch (Exception ex)
            {
                Log.Write("Answer_GetPlayers: Error returning player list");
                ErrorHandler.LogError(ex);
            }
        }

        #endregion

        #region Health / Misc

        /// <summary>
        /// Maintenance timer.
        /// </summary>
        private void Timer_Callback()
        {
            try
            {
                //remove disconnected clients
                lock (_clients)
                {
                    List<Client> disconnected = _clients.Where(c => !c.IsConnected).ToList();
                    foreach (Client client in disconnected)
                    {
                        Log.Write($"Removing disconnected client '{client.RemoteIP}'..");
                        _clients.Remove(client);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Write("Timer_Callback: Unknown error");
                ErrorHandler.LogError(ex);
            }
        }

        #endregion
    }

}
