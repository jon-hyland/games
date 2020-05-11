using Common.Standard.Error;
using Common.Standard.Logging;
using GameServer.Configuration;
using GameServer.Logging;
using GameServer.Networking;
using System;

namespace GameServer
{
    public class Main
    {
        //private
        private readonly Config _config;
        private readonly Logger _logger;
        private readonly Server _server;

        /// <summary>
        /// Class constructor.
        /// </summary>
        public Main()
        {
            try
            {
                //vars
                _config = new Config();
                _logger = new Logger(_config.LogFile);
                _server = new Server(_config.LocalIP, _config.ListenPort);

                //initialize
                Log.Initiallize(_logger);
                ErrorHandler.Initialize(_logger);

                //message
                Log.Write($"Game Server v{_config.DisplayVersion}  (c) 2020 John Hyland");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        /// <summary>
        /// Starts the listening service.
        /// </summary>
        public void Start()
        {
            //start server
            _server.Start();
        }
    }
}
