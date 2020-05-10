using Common.Standard.Game;
using Common.Standard.Logging;
using Common.Standard.Networking.Packets;
using GameServer.Error;
using GameServer.Game;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace GameServer.Networking
{
    public class Server
    {
        //private
        private readonly ConnectedPlayers _players;
        private readonly TcpListener _listener;
        private readonly Thread _listenThread;

        /// <summary>
        /// Class constructor.
        /// </summary>
        public Server(IPAddress ip, int port, ConnectedPlayers players)
        {
            _players = players;
            _listener = new TcpListener(ip, port);
            _listenThread = new Thread(ListenThread);
            _listenThread.IsBackground = true;
        }

        /// <summary>
        /// Starts the server.
        /// </summary>
        public void Start()
        {
            WriteToLog("Starting TCP server..");
            _listener.Start();
            _listenThread.Start();
        }

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
                    Client gameClient = new Client(_listener.AcceptTcpClient());

                    //wire events
                    gameClient.PacketReceived += PacketReceived;
                }
                catch (Exception ex)
                {
                    ErrorHandler.LogError(ex);
                }
            }
        }

        /// <summary>
        /// Fired when packet received from a client.
        /// </summary>
        private void PacketReceived(Client client, PacketBase packet)
        {
            try
            {
                if (packet is HeartbeatPacket hp)
                {
                    Player player = Player.FromPacket(hp, client);
                    if (player != null)
                        _players.AddOrUpdatePlayer(player);
                }

            }
            catch (Exception ex)
            {
                WriteToLog("PacketReceived: Error processing client packet");
                ErrorHandler.LogError(ex);
            }
        }



        /// <summary>
        /// Writes a log entry.
        /// </summary>
        private void WriteToLog(string message)
        {
            Log.Write(LogLevel.Medium, "GameServer", message);
        }
    }

}
