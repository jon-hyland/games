using Bricker.Configuration;
using Bricker.Game;
using Common.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bricker.Networking
{
    /// <summary>
    /// Represents a packet that gets sent to, or received by, the opponent.
    /// </summary>
    public class Packet
    {
        //const
        private const uint PACKET_HEADER = 8710437;

        //public
        public string SourceIP { get; }
        public string TargetIP { get; }
        public string Initials { get; }
        public byte[,] Matrix { get; }
        public ushort Level { get; }
        public ushort Lines { get; }
        public ushort Score { get; }
        public bool InGame { get; }
        public bool Pause { get; }
        public bool Quit { get; }
        public ushort LinesToSend { get; }
        public ushort LinesReceived { get; }

        /// <summary>
        /// Class constructor.
        /// </summary>
        public Packet(string sourceIP, string targetIP, string initals, byte[,] matrix, ushort level, ushort lines, ushort score, bool inGame, bool pause, bool quit, ushort linesToSend, ushort linesReceived)
        {
            SourceIP = sourceIP;
            TargetIP = targetIP;
            Initials = initals;
            Matrix = matrix;
            Level = level;
            Lines = lines;
            Score = score;
            InGame = inGame;
            Pause = pause;
            Quit = quit;
            LinesToSend = linesToSend;
            LinesReceived = linesReceived;
        }

        /// <summary>
        /// Class constructor.
        /// </summary>
        public Packet(string sourceIP, string initals)
        {
            SourceIP = sourceIP;
            TargetIP = "";
            Initials = initals;
            Matrix = new byte[10, 20];
            Level = 0;
            Lines = 0;
            Score = 0;
            InGame = false;
            Pause = false;
            Quit = false;
            LinesToSend = 0;
            LinesReceived = 0;
        }

        /// <summary>
        /// Creates packet from game objects.
        /// </summary>
        public static Packet FromGame(string sourceIP, Matrix matrix, GameStats gameStats)
        {
            string targetIP = "";
            string initals = Config.Initials;
            byte[,] matrx = new byte[10, 20];
            for (int x = 1; x <= 10; x++)
                for (int y = 1; y <= 20; y++)
                    if (matrix.Grid[x, y] > 0)
                        matrx[x - 1, y - 1] = matrix.Grid[x, y];
            ushort level = (ushort)gameStats.Level;
            ushort lines = (ushort)gameStats.Lines;
            ushort score = (ushort)gameStats.Score;
            bool inGame = false;
            bool pause = false;
            bool quit = false;
            ushort linesToSend = 0;
            ushort linesReceived = 0;
            return new Packet(sourceIP, targetIP, initals, matrx, level, lines, score, inGame, pause, quit, linesToSend, linesReceived);
        }

        /// <summary>
        /// Serializes packet to bytes.
        /// </summary>
        public byte[] ToBytes()
        {
            byte[] bytes;
            List<byte> list = new List<byte>();
            list.AddRange(BitConverter.GetBytes(PACKET_HEADER));            // header
            list.AddRange(BitConverter.GetBytes((ushort)0));                // length
            list.AddRange(BitConverter.GetBytes((ushort)1));                // version
            bytes = Encoding.UTF8.GetBytes(SourceIP ?? "");
            list.AddRange(BitConverter.GetBytes((ushort)bytes.Length));
            list.AddRange(bytes);
            bytes = Encoding.UTF8.GetBytes(TargetIP ?? "");
            list.AddRange(BitConverter.GetBytes((ushort)bytes.Length));
            list.AddRange(bytes);
            bytes = Encoding.UTF8.GetBytes(Initials ?? "");
            list.AddRange(BitConverter.GetBytes((ushort)bytes.Length));
            list.AddRange(bytes);
            bytes = new byte[10 * 20];
            Buffer.BlockCopy(Matrix, 0, bytes, 0, 10 * 20);
            list.AddRange(bytes);
            list.AddRange(BitConverter.GetBytes(Level));
            list.AddRange(BitConverter.GetBytes(Lines));
            list.AddRange(BitConverter.GetBytes(Score));
            list.Add((byte)(InGame ? 1 : 0));
            list.Add((byte)(Pause ? 1 : 0));
            list.Add((byte)(Quit ? 1 : 0));
            list.AddRange(BitConverter.GetBytes(LinesToSend));
            list.AddRange(BitConverter.GetBytes(LinesReceived));
            bytes = list.ToArray();
            byte[] length = BitConverter.GetBytes((ushort)bytes.Length);
            Array.Copy(length, 0, bytes, 4, 2);
            return bytes;
        }

        /// <summary>
        /// Deserializes packet from bytes.  Returns null if data invalid.
        /// </summary>
        public static Packet FromBytes(byte[] bytes)
        {
            try
            {
                List<byte> list = new List<byte>(bytes);
                uint header = BitConverter.ToUInt32(list.Dequeue(4), 0);
                if (header != PACKET_HEADER)
                    return null;
                ushort length = BitConverter.ToUInt16(list.Dequeue(2), 0);
                if (length != bytes.Length)
                    return null;
                ushort version = BitConverter.ToUInt16(list.Dequeue(2), 0);
                if (version != 1)
                    return null;
                length = BitConverter.ToUInt16(list.Dequeue(2), 0);
                string sourceIP = Encoding.UTF8.GetString(list.Dequeue(length));
                length = BitConverter.ToUInt16(list.Dequeue(2), 0);
                string targetIP = Encoding.UTF8.GetString(list.Dequeue(length));
                length = BitConverter.ToUInt16(list.Dequeue(2), 0);
                string initials = Encoding.UTF8.GetString(list.Dequeue(length));
                if (String.IsNullOrWhiteSpace(initials))
                    initials = "BOB";
                byte[] flat = list.Dequeue(10 * 20);
                byte[,] matrix = new byte[10, 20];
                Buffer.BlockCopy(flat, 0, matrix, 0, 10 * 20);
                ushort level = BitConverter.ToUInt16(list.Dequeue(2), 0);
                ushort lines = BitConverter.ToUInt16(list.Dequeue(2), 0);
                ushort score = BitConverter.ToUInt16(list.Dequeue(2), 0);
                bool inGame = list.Dequeue() != 0;
                bool pause = list.Dequeue() != 0;
                bool quit = list.Dequeue() != 0;
                ushort linesToSend = BitConverter.ToUInt16(list.Dequeue(2), 0);
                ushort linesReceived = BitConverter.ToUInt16(list.Dequeue(2), 0);
                return new Packet(sourceIP, targetIP, initials, matrix, level, lines, score, inGame, pause, quit, linesToSend, linesReceived);
            }
            catch
            {
                return null;
            }
        }






    }
}
