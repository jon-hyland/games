using System.Net;

namespace Common.Standard.Networking
{
    public interface IClient
    {
        IPAddress RemoteIP { get; }
        bool IsConnected { get; }
    }
}
