using System.Collections.ObjectModel;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using SisoDis.Core.Common;
using SisoDis.Core.Pdu;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

var pduLog = new ObservableCollection<PduEntry>();
var isRunning = false;
var multicastAddress = "239.255.0.1";
var port = 3000;
var totalReceived = 0L;
var totalBytes = 0L;
var totalErrors = 0L;

MulticastReceiver? receiver = null;

app.MapGet("/api/status", () => new
{
    isRunning,
    multicastAddress,
    port,
    totalReceived,
    totalBytes,
    totalErrors
});

app.MapPost("/api/start", () =>
{
    if (isRunning) return Results.Ok(new { status = "already running" });
    
    receiver = new MulticastReceiver(multicastAddress, port);
    receiver.PduReceived += OnPduReceived;
    receiver.Start();
    isRunning = true;
    pduLog.Insert(0, new PduEntry { Time = DateTime.Now, Type = "SYSTEM", Message = $"Started listening on {multicastAddress}:{port}" });
    return Results.Ok(new { status = "running" });
});

app.MapPost("/api/stop", () =>
{
    if (!isRunning) return Results.Ok(new { status = "already stopped" });
    
    receiver?.Dispose();
    receiver = null;
    isRunning = false;
    pduLog.Insert(0, new PduEntry { Time = DateTime.Now, Type = "SYSTEM", Message = "Stopped listening" });
    return Results.Ok(new { status = "stopped" });
});

app.MapPost("/api/config", (ConfigRequest req) =>
{
    if (!string.IsNullOrEmpty(req.MulticastAddress)) multicastAddress = req.MulticastAddress;
    if (req.Port > 0) port = req.Port;
    return Results.Ok(new { multicastAddress, port });
});

app.MapGet("/api/log", () => pduLog.Take(100).ToList());

app.MapDelete("/api/log", () =>
{
    pduLog.Clear();
    return Results.Ok();
});

app.MapGet("/api/pdu-types", () => new[]
{
    new { type = 1, name = "Entity State" },
    new { type = 2, name = "Fire" },
    new { type = 3, name = "Detonation" },
    new { type = 4, name = "Collision" },
    new { type = 5, name = "Collision-Elastic" },
    new { type = 6, name = "Entity State Update" },
    new { type = 7, name = "Attribute" },
    new { type = 20, name = "Munition" },
    new { type = 21, name = "Designator" },
    new { type = 22, name = "Electromagnetic Emission" },
    new { type = 23, name = "Create Entity" },
    new { type = 24, name = "Remove Entity" },
    new { type = 25, name = "Start/Resume" },
    new { type = 26, name = "Stop/Freeze" },
    new { type = 27, name = "Acknowledge" },
    new { type = 28, name = "Action Request" },
    new { type = 29, name = "Action Response" },
    new { type = 30, name = "Data Query" }
});

void OnPduReceived(IPdu pdu, ReadOnlySpan<byte> data)
{
    totalReceived++;
    totalBytes += data.Length;
    
    var entry = new PduEntry
    {
        Time = DateTime.Now,
        Type = GetPduTypeName(pdu.PdType),
        EntityId = GetEntityId(pdu),
        Size = data.Length,
        Message = GetPduMessage(pdu)
    };
    
    pduLog.Insert(0, entry);
    
    while (pduLog.Count > 500) pduLog.RemoveAt(pduLog.Count - 1);
}

string GetPduTypeName(ushort type) => type switch
{
    1 => "Entity State",
    2 => "Fire",
    3 => "Detonation",
    4 => "Collision",
    5 => "Collision-Elastic",
    6 => "Entity State Update",
    7 => "Attribute",
    20 => "Munition",
    21 => "Designator",
    22 => "Electromagnetic Emission",
    23 => "Create Entity",
    24 => "Remove Entity",
    25 => "Start/Resume",
    26 => "Stop/Freeze",
    27 => "Acknowledge",
    28 => "Action Request",
    29 => "Action Response",
    30 => "Data Query",
    _ => $"Type {type}"
};

string GetEntityId(IPdu pdu)
{
    var prop = pdu.GetType().GetProperty("EntityId");
    if (prop?.GetValue(pdu) is EntityId eid)
        return eid.Value.ToString();
    return "-";
}

string GetPduMessage(IPdu pdu) => pdu.PdType switch
{
    1 => "Position update",
    2 => "Weapon fired",
    3 => "Munition detonated",
    4 => "Collision detected",
    25 => "Simulation started/resumed",
    26 => "Simulation stopped/frozen",
    _ => "PDU received"
};

app.Run();

public class PduEntry
{
    public DateTime Time { get; set; }
    public string Type { get; set; } = "";
    public string EntityId { get; set; } = "";
    public int Size { get; set; }
    public string Message { get; set; } = "";
}

public class ConfigRequest
{
    public string? MulticastAddress { get; set; }
    public int Port { get; set; }
}

internal sealed class MulticastReceiver : IDisposable
{
    private readonly Socket _socket;
    private readonly byte[] _receiveBuffer;
    private readonly CancellationTokenSource _cts;
    private bool _disposed;

    public event Action<IPdu, ReadOnlySpan<byte>>? PduReceived;

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
                if (bytesRead < PduHeader.HeaderLength) continue;

                var pduData = _receiveBuffer.AsSpan(0, bytesRead);
                var pdu = ParsePdu(pduData);
                
                if (pdu != null)
                {
                    PduReceived?.Invoke(pdu, pduData);
                }
            }
            catch (OperationCanceledException) when (_cts.Token.IsCancellationRequested)
            {
                break;
            }
            catch
            {
            }
        }
    }

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
