using System.Net.Sockets;

namespace Common.Networking.Tcp
{
    /// <summary>
    /// State object for receiving data from remote device.
    /// </summary>
    public class ReceiveState
    {
        public const int BUFFER_SIZE = 16384;
        public Socket ClientSocket = null;  //rename?
        public byte[] Buffer = new byte[BUFFER_SIZE];
    }
}
