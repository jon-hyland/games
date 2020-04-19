using Common.Logging;
using Common.Networking.Tcp;
using System;
using System.Linq;
using System.Text;

namespace TcpSendReceive
{
    public class Program
    {
        //private
        private static readonly ErrorHandler _errorHandler = new ErrorHandler();
        private static readonly Logger _logger = new Logger();
        private static readonly Random _random = new Random();

        /// <summary>
        /// Program entry point.
        /// </summary>
        public static void Main(string[] args)
        {
            //vars
            bool clientMode = true;

            //client mode
            if (clientMode)
            {
                const string CHARS = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
                const int TEST_SIZE = 20000;
                string[] samples = new string[TEST_SIZE];

                StringBuilder sample = new StringBuilder();
                for (int i = 0; i < TEST_SIZE; i++)
                {
                    char c = CHARS[_random.Next(CHARS.Length)];
                    sample.Append(c);
                    samples[i] = sample.ToString();
                }

                using (SimpleTcpClient client = new SimpleTcpClient("127.0.0.1", 8686, 1234567890, 1000, _errorHandler, _logger))
                {
                    client.Connect();
                    for (int i = 0; i < TEST_SIZE; i++)
                    {
                        byte[] payload1 = Encoding.UTF8.GetBytes(samples[i]);
                        client.SendPacket(payload1);
                        byte[] payload2 = client.WaitForPacket(60000);
                        if (payload2 == null)
                        {
                            WriteLog("Client receive timeout!");
                        }
                        else if (!payload2.SequenceEqual(payload1))
                        {
                            WriteLog("Payloads do not match!");
                        }
                    }
                }
            }

            //server mode
            else
            {
                using (SimpleTcpServer server = new SimpleTcpServer("127.0.0.1", 8686, 1234567890, 1000, _errorHandler, _logger))
                {
                    server.Start();
                    while (true)
                    {
                        using (SimpleTcpClient client = server.WaitForClient(60000))
                        {
                            if (client == null)
                                continue;                            

                            while (true)
                            {
                                byte[] payload = client.WaitForPacket(60000);
                                if (payload != null)
                                {
                                    WriteLog($"Echoing {payload.Length} bytes to client");
                                    client.SendPacket(payload);
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Writes a log message using class defaults.
        /// </summary>
        private static void WriteLog(string message)
        {
            _logger?.Write(LogLevel.Medium, "Tester", message);
        }


    }
}
