using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using SisoDis.Core.Pdu;

namespace SisoDis.Receiver;

/// <summary>
/// UDP multicast receiver for DIS PDUs per IEEE 1278.1-2012.
/// Joins a multicast group and receives PDU data on a background task.
/// </summary>
internal sealed class MulticastReceiver : IDisposable
{
    private readonly Socket _socket;
    private readonly byte[] _receiveBuffer;
    private readonly CancellationTokenSource _cts;
    private bool _disposed;

    /// <summary>Fired when a PDU is received from the network.</summary>
    public event Action<IPdu, ReadOnlySpan<byte>>? PduReceived;

    /// <summary>Total number of PDUs received.</summary>
    public long TotalPdusReceived { get; private set; }

    /// <summary>Total number of bytes received.</summary>
    public long TotalBytesReceived { get; private set; }

    /// <summary>Total number of receive errors.</summary>
    public long TotalErrors { get; private set; }

    public MulticastReceiver(string multicastAddress, int port, int bufferSize = 2048)
    {
        var multicastIp = IPAddress.Parse(multicastAddress);
        _receiveBuffer = new byte[bufferSize];
        _cts = new CancellationTokenSource();

        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        _socket.Bind(new IPEndPoint(IPAddress.Any, port));
        _socket.SetSocketOption(
            SocketOptionLevel.IP,
            SocketOptionName.AddMembership,
            new MulticastOption(multicastIp, IPAddress.Any));
    }

    /// <summary>
    /// Starts receiving PDUs on a background task.
    /// Returns immediately; use PduReceived event to handle incoming PDUs.
    /// </summary>
    public void Start()
    {
        _ = Task.Run(ReceiveLoop, _cts.Token);
    }

    private async Task ReceiveLoop()
    {
        var endPoint = new IPEndPoint(IPAddress.Any, 0);
        while (!_cts.Token.IsCancellationRequested)
        {
            try
            {
                var result = await _socket.ReceiveFromAsync(
                    _receiveBuffer.AsMemory(),
                    SocketFlags.None,
                    endPoint,
                    _cts.Token);

                int bytesRead = result.ReceivedBytes;
                if (bytesRead < PduHeader.HeaderLength)
                {
                    TotalErrors++;
                    continue;
                }

                var pduData = _receiveBuffer.AsSpan(0, bytesRead);
                var pdu = ParsePdu(pduData);
                
                if (pdu != null)
                {
                    TotalPdusReceived++;
                    TotalBytesReceived += bytesRead;
                    PduReceived?.Invoke(pdu, pduData);
                }
                else
                {
                    TotalErrors++;
                }
            }
            catch (OperationCanceledException) when (_cts.Token.IsCancellationRequested)
            {
                break;
            }
            catch (SocketException)
            {
                TotalErrors++;
            }
            catch (Exception)
            {
                TotalErrors++;
            }
        }
    }

    /// <summary>
    /// Parses the PDU from raw bytes.
    /// Returns null if the PDU type is unknown or parsing fails.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static IPdu? ParsePdu(ReadOnlySpan<byte> data)
    {
        try
        {
            ushort pduType = (ushort)((data[4] << 8) | data[5]);
            var pdu = PduFactory.CreatePdu(pduType);
            return DeserializePdu(pdu, data);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Deserializes the PDU body data into the given PDU instance.
    /// </summary>
    private static IPdu? DeserializePdu(IPdu pdu, ReadOnlySpan<byte> data)
    {
        try
        {
            return pdu.PdType switch
            {
                1 => EntityStatePdu.Deserialize(data, 0),
                2 => FirePdu.Deserialize(data, 0),
                3 => DetonationPdu.Deserialize(data, 0),
                4 => CollisionPdu.Deserialize(data, 0),
                5 => CollisionElasticPdu.Deserialize(data, 0),
                6 => EntityStateUpdatePdu.Deserialize(data, 0),
                7 => AttributePdu.Deserialize(data, 0),
                20 => MunitionPdu.Deserialize(data, 0),
                21 => DesignatorPdu.Deserialize(data, 0),
                22 => ElectromagneticEmissionPdu.Deserialize(data, 0),
                23 => CreateEntityPdu.Deserialize(data, 0),
                24 => RemoveEntityPdu.Deserialize(data, 0),
                25 => StartResumePdu.Deserialize(data, 0),
                26 => StopFreezePdu.Deserialize(data, 0),
                27 => AcknowledgePdu.Deserialize(data, 0),
                28 => ActionRequestPdu.Deserialize(data, 0),
                29 => ActionResponsePdu.Deserialize(data, 0),
                30 => DataQueryPdu.Deserialize(data, 0),
                _ => null
            };
        }
        catch
        {
            return null;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        
        _cts.Cancel();
        _cts.Dispose();
        _socket.Dispose();
    }
}
