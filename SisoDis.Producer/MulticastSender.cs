using System.Net;
using System.Net.Sockets;

namespace SisoDis.Producer;

internal sealed class MulticastSender : IDisposable
{
    private readonly Socket _socket;
    private readonly IPEndPoint _multicastEndpoint;

    public long TotalPdusSent { get; private set; }

    public long TotalBytesSent { get; private set; }

    public MulticastSender(string multicastAddress, int port)
    {
        _multicastEndpoint = new IPEndPoint(IPAddress.Parse(multicastAddress), port);

        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        _socket.Bind(new IPEndPoint(IPAddress.Any, 0));
        _socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 2);
        _socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastLoopback, true);
    }

    public void Send(ReadOnlySpan<byte> data)
    {
        _socket.SendTo(data, SocketFlags.None, _multicastEndpoint);
        TotalPdusSent++;
        TotalBytesSent += data.Length;
    }

    public void Dispose()
    {
        _socket.Dispose();
    }
}
