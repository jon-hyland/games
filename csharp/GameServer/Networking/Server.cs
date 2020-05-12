using Common.Standard.Error;
using Common.Standard.Extensions;
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
        private readonly Dictionary<int, Client> _clientsByPlayerKey;
        private readonly List<Session> _sessions;
        private readonly CommandManager _commandManager;
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
            _clientsByPlayerKey = new Dictionary<int, Client>();
            _sessions = new List<Session>();
            _commandManager = new CommandManager();
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
        private void AddOrUpdatePlayer(Player player, Client client)
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

            lock (_clientsByPlayerKey)
            {
                if (!_clientsByPlayerKey.ContainsKey(player.UniqueKey))
                    _clientsByPlayerKey.Add(player.UniqueKey, client);
                else if (_clientsByPlayerKey[player.UniqueKey] != client)
                    _clientsByPlayerKey[player.UniqueKey] = client;
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
                    .Where(p => p.TimeSinceLastDiscovery.TotalMinutes > 1)
                    .ToList();
                foreach (Player p in expired)
                {
                    Log.Write($"Removing expired/disconnected player [ip={p.IP}, name={p.Name}]..");
                    _players.Remove(p);
                }
            }
        }

        /// <summary>
        /// Returns matching player, or null.
        /// </summary>
        private Player GetMatchingPlayer(IPAddress ip, string gameTitle, Version gameVersion)
        {
            lock (_players)
            {
                return _players
                    .Where(p => p.IP.Equals(ip))
                    .Where(p => p.GameTitle.Equals(gameTitle))
                    .Where(p => p.GameVersion.Equals(gameVersion))
                    .FirstOrDefault();
            }
        }

        /// <summary>
        /// Gets TCP client last associated with specified player.
        /// Client can change if the player disconnects/reconnects, etc.
        /// </summary>
        private Client GetClientByPlayer(Player player)
        {
            lock (_clientsByPlayerKey)
            {
                if (_clientsByPlayerKey.ContainsKey(player.UniqueKey))
                    return _clientsByPlayerKey[player.UniqueKey];
                return null;
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
                    Player player = Player.FromPacket(hp);
                    if (player != null)
                        AddOrUpdatePlayer(player, client);
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

                        //connect to player
                        case CommandType.ConnectToPlayer:
                            Passthrough_Command(client, req);
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
        private void Answer_GetPlayers(Client sourceClient, CommandRequestPacket packet)
        {
            try
            {
                //get source player
                Player player = Player.FromPacket(packet);

                //get list of connected players (not source player)
                List<Player> otherPlayers = GetPlayers(player.GameTitle, player.GameVersion)
                    .Where(p => p.UniqueKey != player.UniqueKey)
                    .ToList();

                //message
                Log.Write($"Responding to command 'GetPlayers' request from [name={player.Name}, ip={player.IP}]..");

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
                    playerName: player.Name,
                    commandType: CommandType.GetPlayers,
                    sequence: packet.Sequence,
                    code: ResultCode.Accept,
                    data: builder.ToBytes());

                //send response back to source
                sourceClient.SendPacket(response);
            }
            catch (Exception ex)
            {
                Log.Write("Answer_GetPlayers: Error returning player list");
                ErrorHandler.LogError(ex);
            }
        }

        /// <summary>
        /// Forwards a command request to destination player, waits for response (or timeout),
        /// responds to source with answer.
        /// </summary>
        private void Passthrough_Command(Client sourceClient, CommandRequestPacket requestPacket)
        {
            CommandResult result = new CommandResult(ResultCode.Unspecified);
            Player sourcePlayer = null;
            Player destinationPlayer = null;

            try
            {
                //get source player
                sourcePlayer = Player.FromPacket(requestPacket);
                if (sourcePlayer == null)
                {
                    Log.Write($"Passthrough_Command: Unable to parse packet");
                    result.Code = ResultCode.Error;
                    return;
                }

                //get destination player
                destinationPlayer = GetMatchingPlayer(requestPacket.DestinationIP, requestPacket.GameTitle, requestPacket.GameVersion);
                if (destinationPlayer == null)
                {
                    Log.Write($"Passthrough_Command: Cannot find destination player with IP '{requestPacket.DestinationIP}'");
                    result.Code = ResultCode.Error;
                    return;
                }

                //get destination client
                Client destinationClient = GetClientByPlayer(destinationPlayer);
                if (destinationClient == null)
                {
                    Log.Write($"Passthrough_Command: Destination player does not have assigned TCP client");
                    result.Code = ResultCode.Error;
                    return;
                }

                //message
                Log.Write($"Forwarding command '{requestPacket.CommandType}' request from [name={sourcePlayer.Name}, ip={sourcePlayer.IP}] to [name={destinationPlayer.Name}, ip={destinationPlayer.IP}]..");

                //forward request to destination
                destinationClient.SendPacket(requestPacket);

                //record command request has been sent
                _commandManager.RequestSent(requestPacket);

                //wait for response or timeout
                while (true)
                {
                    //vars
                    DateTime start = DateTime.Now;

                    //get current status
                    result = _commandManager.GetCommandStatus(requestPacket.Sequence);

                    //have answer or timeout?  return!
                    if (result.Code != ResultCode.Unspecified)
                        return;

                    //sleep
                    Thread.Sleep(15);
                }
            }
            catch (Exception ex)
            {
                Log.Write("Passthrough_Command: Error passing command to destination");
                ErrorHandler.LogError(ex);
            }
            finally
            {
                try
                {
                    //create packet
                    CommandResponsePacket responsePacket;
                    if ((result.Code.In(ResultCode.Accept, ResultCode.Reject)) && (result.ResponsePacket != null))
                    {
                        //get original packet
                        responsePacket = result.ResponsePacket;

                        //message
                        Log.Write($"Forwarding command '{requestPacket.CommandType}' response from [name={destinationPlayer.Name}, ip={destinationPlayer.IP}] to [name={sourcePlayer.Name}, ip={sourcePlayer.IP}]..");
                    }
                    else
                    {
                        //create new timeout/error packet
                        responsePacket = new CommandResponsePacket(
                            gameTitle: requestPacket.GameTitle,
                            gameVersion: requestPacket.GameVersion,
                            sourceIP: requestPacket.DestinationIP,
                            destinationIP: requestPacket.SourceIP,
                            playerName: "",
                            commandType: requestPacket.CommandType,
                            sequence: requestPacket.Sequence,
                            code: result.Code,
                            data: null);

                        //message
                        Log.Write($"Sending command '{requestPacket.CommandType}' result of '{result.Code}' to [name={sourcePlayer.Name}, ip={sourcePlayer.IP}]..");
                    }

                    //send response to source
                    sourceClient.SendPacket(responsePacket);
                }
                catch (Exception ex)
                {
                    Log.Write("Passthrough_Command: Error sending command response to source");
                    ErrorHandler.LogError(ex);
                }
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
