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
        private readonly int _port;
        private readonly TcpListener _listener;
        private readonly List<Client> _clients;
        private readonly List<Player> _players;
        private readonly Dictionary<int, Client> _clientsByPlayerKey;
        private readonly List<Session> _sessions;
        private readonly CommandManager _commandManager;
        private readonly Thread _listenThread;
        private readonly SimpleTimer _timer;
        private readonly Random _random;

        #region Constructor

        /// <summary>
        /// Class constructor.
        /// </summary>
        public Server(IPAddress ip, int port)
        {
            _ip = ip;
            _port = port;
            _listener = new TcpListener(ip, port);
            _clients = new List<Client>();
            _players = new List<Player>();
            _clientsByPlayerKey = new Dictionary<int, Client>();
            _sessions = new List<Session>();
            _commandManager = new CommandManager();
            _listenThread = new Thread(ListenThread);
            _listenThread.IsBackground = true;
            _timer = new SimpleTimer(Timer_Callback, 1000);
            _random = new Random();
        }

        /// <summary>
        /// Starts the server.
        /// </summary>
        public void Start()
        {
            Log.Write($"Starting TCP server on port '{_port}'");
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
                    Log.Write($"Accepting new TCP connection from '{client.RemoteIP}'");

                    //add to list
                    lock (_clients)
                        _clients.Add(client);

                    //wire events
                    client.PacketReceived += PacketReceived;
                }
                catch (Exception ex)
                {
                    Log.Write("ListenThread: Error occurred in listen thread loop");
                    ErrorHandler.LogError(ex);
                }
            }
        }

        #endregion

        #region Players

        /// <summary>
        /// Returns list of matching players.
        /// </summary>
        private List<Player> GetMatchingPlayers(string gameTitle, Version gameVersion)
        {
            lock (_players)
            {
                RemoveExpiredPlayers();
                return _players
                    .Where(p => p.GameTitle.Equals(gameTitle))
                    .Where(p => p.GameVersion.Equals(gameVersion))
                    .OrderByDescending(p => p.FirstHeartbeat)
                    .ToList();
            }
        }

        /// <summary>
        /// Returns player with matching key, or null if not found.
        /// </summary>
        private Player GetPlayerByKey(int key)
        {
            lock (_players)
            {
                Player existing = _players
                    .Where(p => p.UniqueKey == key)
                    .FirstOrDefault();
                return existing;
            }
        }

        /// <summary>
        /// Adds or updates a discovered player.
        /// </summary>
        private void AddOrUpdatePlayer(Player player, Client client)
        {
            lock (_players)
            {
                Player existing = GetPlayerByKey(player.UniqueKey);

                if (existing != null)
                {
                    if (existing.Name != player.Name)
                    {
                        Log.Write($"Changing player name '{existing.Name}' to '{player.Name}' at '{player.IP}'");
                        existing.Name = player.Name;
                    }
                    //Log.Write($"Updating last heartbeat for player '{existing.IP}'");
                    existing.LastHeartbeat = DateTime.Now;
                }
                else
                {
                    Log.Write($"Adding new player '{player.Name}' at '{player.IP}' playing '{player.GameTitle} v{player.GameVersion}'");
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
        /// Removes any players that haven't checked in within the past minute.
        /// </summary>
        private void RemoveExpiredPlayers()
        {
            lock (_players)
            {
                List<Player> expired = _players
                    .Where(p => (p.TimeSinceLastHeartbeat.TotalMinutes > 1) || (p.QuitGame))
                    .ToList();
                foreach (Player p in expired)
                {
                    Log.Write($"Removing disconnected player '{p.Name}' at '{p.IP}'");
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
        /// Creates a new unconfirmed session.
        /// </summary>
        private void CreateSession(int playerKey1, int playerKey2)
        {
            lock (_sessions)
            {
                //get players
                Player player1 = GetPlayerByKey(playerKey1);
                if (player1 == null)
                    Log.Write($"Cannot create session.. player '{player1.IP}' does not exist or match");
                Player player2 = GetPlayerByKey(playerKey2);
                if (player2 == null)
                    Log.Write($"Cannot create session.. player '{player2.IP}' does not exist or match");

                //session already exists?  do nothing..
                Session existing = _sessions.Where(s => s.ContainsBothPlayers(player1, player2)).FirstOrDefault();
                if (existing != null)
                {
                    Log.Write($"Session with '{player1.IP}' and '{player2.IP}' already exists");
                    existing.CreateTime = DateTime.Now;
                    return;
                }

                //sessions with only one player match?  remove it!
                List<Session> oneMatch = _sessions.Where(s => s.ContainsEitherPlayer(player1, player2)).ToList();
                foreach (Session session in oneMatch)
                {
                    Log.Write($"Removing conflicting session with '{player1.IP}' and '{player2.IP}'");
                    _sessions.Remove(session);
                }

                //create new session
                Log.Write($"Creating unconfirmed session between '{player1.IP}' and '{player2.IP}'");
                _sessions.Add(new Session(player1, player2));
            }
        }

        /// <summary>
        /// Confirms a session between two players.
        /// </summary>
        private void ConfirmSession(int player1Key, int player2Key)
        {
            Session session = GetSession(player1Key, player2Key);
            if (session == null)
                return;
            Log.Write($"Confirming session between '{session.Player1.IP}' and '{session.Player2.IP}'");
            session.ConfirmSession();
        }

        /// <summary>
        /// Returns matching session, or null if no session (expired, etc).
        /// </summary>
        private Session GetSession(int player1Key, int player2Key)
        {
            lock (_sessions)
            {
                return _sessions
                    .Where(s => s.ContainsBothPlayers(player1Key, player2Key))
                    .FirstOrDefault();
            }
        }

        /// <summary>
        /// Removes any session where one or more players haven't sent heatbeat packets
        /// in over 10 seconds, or a session isn't confirmed within 30 seconds.
        /// </summary>
        private void RemoveExpiredSessions()
        {
            try
            {
                lock (_sessions)
                {
                    List<Session> expired = new List<Session>();
                    foreach (Session session in _sessions)
                    {
                        Player expiredPlayer = session.GetTimedoutPlayer(timeoutMs: 10000);
                        if (expiredPlayer != null)
                        {
                            if (!expiredPlayer.QuitGame)
                                Log.Write($"Player '{expiredPlayer.IP}' hasn't send heartbeat in {expiredPlayer.TimeSinceLastHeartbeat.TotalSeconds.ToString("0.0")} seconds");
                            expired.Add(session);
                            continue;
                        }
                        if ((!session.IsConfirmed) && (session.TimeSinceCreated.TotalSeconds > 30))
                        {
                            Log.Write($"Session was not confirmed within 30 seconds");
                            expired.Add(session);
                            continue;
                        }
                    }
                    foreach (Session session in expired)
                    {
                        Log.Write($"Removing expired session between '{session.Player1.IP}' and '{session.Player2.IP}'");
                        _sessions.Remove(session);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Write("RemoveExpiredSessions: Error processing expired session");
                ErrorHandler.LogError(ex);
            }
        }

        /// <summary>
        /// Sends 'EndSession' command to client to inform them of opponent disconnect.
        /// Waits for client acknowledgment, but doesn't do anything with it.
        /// </summary>
        private void SendEndSessionCommand(Client client, Player player)
        {
            try
            {
                //vars
                uint timeoutMs = 1000;
                ushort sequence = (ushort)(UInt32.MaxValue - _random.Next(1000));
                CommandResult result;

                //message
                Log.Write($"Sending 'EndSession' request to '{client.RemoteIP}'");

                //create packet
                CommandRequestPacket packet = new CommandRequestPacket(
                    gameTitle: player.GameTitle,
                    gameVersion: player.GameVersion,
                    sourceIP: _ip,
                    destinationIP: player.IP,
                    playerName: player.Name,
                    commandType: CommandType.EndSession,
                    sequence: sequence,
                    retryAttempt: 0,
                    timeoutMs: timeoutMs,
                    data: null);

                //record command request being sent
                _commandManager.RequestSent(packet);

                //forward request to destination
                client.SendPacket(packet);

                //wait for response or timeout
                while (true)
                {
                    //get current status
                    result = _commandManager.GetCommandStatus(packet.Sequence);

                    //have answer or timeout?  return!
                    if (result.Code != ResultCode.Unspecified)
                        return;

                    //sleep
                    Thread.Sleep(15);
                }
            }
            catch (Exception ex)
            {
                Log.Write("SendEndSessionCommand: Error processing expired session");
                ErrorHandler.LogError(ex);
            }
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
                    Log.Write($"Received '{req.CommandType}' request from '{client.RemoteIP}'");
                    switch (req.CommandType)
                    {
                        //get-players command
                        case CommandType.GetPlayers:
                            Answer_GetPlayers(client, req);
                            break;

                        //quit-game command
                        case CommandType.QuitGame:
                            Answer_QuitGame(client, req);
                            break;

                        //all other commands
                        default:
                            Passthrough_Command(client, req);
                            break;
                    }
                }

                //command response
                else if (packet is CommandResponsePacket resp)
                {
                    Log.Write($"Received '{resp.CommandType}' ({resp.Code}) response from '{client.RemoteIP}'");
                    _commandManager.ResponseReceived(resp);
                }

                //data
                else if (packet is DataPacket dp)
                {
                    Passthrough_Data(client, dp);
                }

            }
            catch (Exception ex)
            {
                Log.Write("PacketReceived: Error processing packet");
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
                List<Player> otherPlayers = GetMatchingPlayers(player.GameTitle, player.GameVersion)
                    .Where(p => p.UniqueKey != player.UniqueKey)
                    .ToList();

                //message
                Log.Write($"Sending '{packet.CommandType}' ({otherPlayers.Count} player{(otherPlayers.Count != 1 ? "s" : "")}) response to '{player.IP}'");

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
        /// Answers a 'QuitGame' request, responds with acceptance.
        /// </summary>
        private void Answer_QuitGame(Client sourceClient, CommandRequestPacket packet)
        {
            try
            {
                //get source player
                Player player = Player.FromPacket(packet);
                player = GetPlayerByKey(player.UniqueKey);

                //set quit flag
                player.QuitGame = true;

                //remove expired players
                RemoveExpiredPlayers();

                //remove expired sessions
                RemoveExpiredSessions();

                //message
                Log.Write($"Sending '{packet.CommandType}' ({ResultCode.Accept}) response to '{player.IP}'");

                //create packet
                CommandResponsePacket response = new CommandResponsePacket(
                    gameTitle: player.GameTitle,
                    gameVersion: player.GameVersion,
                    sourceIP: _ip,
                    destinationIP: player.IP,
                    playerName: player.Name,
                    commandType: CommandType.QuitGame,
                    sequence: packet.Sequence,
                    code: ResultCode.Accept,
                    data: null);

                //send response back to source
                sourceClient.SendPacket(response);
            }
            catch (Exception ex)
            {
                Log.Write("Answer_QuitGame: Unknown error");
                ErrorHandler.LogError(ex);
            }
        }

        /// <summary>
        /// Forwards a command request packet to destination player, waits for response (or timeout),
        /// responds to source player with answer.
        /// </summary>
        private void Passthrough_Command(Client sourceClient, CommandRequestPacket requestPacket)
        {
            CommandResult result = new CommandResult(ResultCode.Unspecified);
            Player sourcePlayer = null;
            Player destinationPlayer = null;
            bool sessionEnded = false;

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
                    Log.Write($"Passthrough_Command: Cannot find destination player at '{requestPacket.DestinationIP}'");
                    result.Code = ResultCode.Error;
                    return;
                }

                //create session?
                if (requestPacket.CommandType == CommandType.ConnectToPlayer)
                    CreateSession(sourcePlayer.UniqueKey, destinationPlayer.UniqueKey);

                //get session
                Session session = GetSession(sourcePlayer.UniqueKey, destinationPlayer.UniqueKey);
                if ((session == null) && (requestPacket.CommandType != CommandType.ConnectToPlayer))
                {
                    Log.Write($"Passthrough_Command: Live session does not exist between '{sourcePlayer.IP}' and '{destinationPlayer.IP}'");
                    result.Code = ResultCode.Error;
                    sessionEnded = true;
                    return;
                }

                //get destination client
                Client destinationClient = GetClientByPlayer(destinationPlayer);
                if (destinationClient == null)
                {
                    Log.Write($"Passthrough_Command: Destination player at {destinationClient.RemoteIP} does not have assigned TCP client");
                    result.Code = ResultCode.Error;
                    return;
                }

                //message
                Log.Write($"Forwarding '{requestPacket.CommandType}' request from '{sourcePlayer.IP}' to '{destinationPlayer.IP}'");

                //record command request being sent
                _commandManager.RequestSent(requestPacket);

                //forward request to destination
                destinationClient.SendPacket(requestPacket);

                //wait for response or timeout
                while (true)
                {
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
                Log.Write("Passthrough_Command: Error forwarding request or waiting for response");
                ErrorHandler.LogError(ex);
            }
            finally
            {
                try
                {
                    //create response packet
                    CommandResponsePacket responsePacket;
                    if ((result.Code.In(ResultCode.Accept, ResultCode.Reject)) && (result.ResponsePacket != null))
                    {
                        ////create session?
                        //if ((requestPacket.CommandType == CommandType.ConnectToPlayer) && (result.Code == ResultCode.Accept))
                        //    CreateSession(sourcePlayer.UniqueKey, destinationPlayer.UniqueKey);

                        //confirm session?
                        if ((requestPacket.CommandType == CommandType.ConnectToPlayer) && (result.Code == ResultCode.Accept))
                            ConfirmSession(sourcePlayer.UniqueKey, destinationPlayer.UniqueKey);

                        //get original packet
                        responsePacket = result.ResponsePacket;

                        //message
                        Log.Write($"Forwarding '{result.ResponsePacket.CommandType}' ({result.ResponsePacket.Code}) response from '{destinationPlayer.IP}' to '{sourcePlayer.IP}'");
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
                        Log.Write($"Sending '{responsePacket.CommandType}' ({responsePacket.Code}) response to '{sourceClient.RemoteIP}'");
                    }

                    //send response to source
                    sourceClient.SendPacket(responsePacket);

                    //send session-ended command?
                    if (sessionEnded)
                        SendEndSessionCommand(sourceClient, sourcePlayer);
                }
                catch (Exception ex)
                {
                    Log.Write("Passthrough_Command: Error sending command response to source");
                    ErrorHandler.LogError(ex);
                }
            }
        }

        /// <summary>
        /// Forwards a data packet to destination player.  No waiting or response.
        /// </summary>
        private void Passthrough_Data(Client sourceClient, DataPacket dataPacket)
        {
            Player sourcePlayer = null;
            Player destinationPlayer;
            bool sessionEnded = false;

            try
            {
                //get source player
                sourcePlayer = Player.FromPacket(dataPacket);
                if (sourcePlayer == null)
                {
                    Log.Write($"Passthrough_Data: Unable to parse packet");
                    return;
                }

                //get destination player
                destinationPlayer = GetMatchingPlayer(dataPacket.DestinationIP, dataPacket.GameTitle, dataPacket.GameVersion);
                if (destinationPlayer == null)
                {
                    Log.Write($"Passthrough_Data: Cannot find destination player at '{dataPacket.DestinationIP}'");
                    sessionEnded = true;
                    return;
                }

                //get session
                Session session = GetSession(sourcePlayer.UniqueKey, destinationPlayer.UniqueKey);
                if (session == null)
                {
                    Log.Write($"Passthrough_Data: Live session does not exist between '{sourcePlayer.IP}' and '{destinationPlayer.IP}'");
                    sessionEnded = true;
                    return;
                }

                //get destination client
                Client destinationClient = GetClientByPlayer(destinationPlayer);
                if (destinationClient == null)
                {
                    Log.Write($"Passthrough_Data: Destination player at {destinationClient.RemoteIP} does not have assigned TCP client");
                    sessionEnded = true;
                    return;
                }

                ////message (keep)
                //Log.Write($"Forwarding data packet from '{sourcePlayer.IP}' to '{destinationPlayer.IP}'");

                //forward request to destination
                destinationClient.SendPacket(dataPacket);
            }
            catch (Exception ex)
            {
                Log.Write("Passthrough_Data: Error forwarding data packet");
                ErrorHandler.LogError(ex);
            }
            finally
            {
                try
                {
                    //send session-ended command?
                    if (sessionEnded)
                        SendEndSessionCommand(sourceClient, sourcePlayer);
                }
                catch (Exception ex)
                {
                    Log.Write("Passthrough_Data: Finalizer error");
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
                        Log.Write($"Removing disconnected client '{client.RemoteIP}'");
                        _clients.Remove(client);
                    }
                }

                //remove expired players
                RemoveExpiredPlayers();

                //remove expired sessions
                RemoveExpiredSessions();
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
