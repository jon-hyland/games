using System.Net;

namespace Common.Standard.Networking
{
    public interface IClient
    {
        IPAddress ClientIP { get; }
        bool IsConnected { get; }
    }
}
