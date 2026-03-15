using System.Collections.ObjectModel;
using System.Net;
using System.Net.Sockets;
using System.Text;
using SisoDis.Core.Common;
using SisoDis.Core.Pdu;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

var entities = new ObservableCollection<WebEntity>();
var pduLog = new ObservableCollection<string>();
var isRunning = false;
var rate = 5;
var multicastAddress = "239.255.0.1";
var port = 3000;

using var sender = new UdpClient();
sender.JoinMulticastGroup(IPAddress.Parse(multicastAddress));
var buffer = new byte[1500];

string SerializePdu(IPdu pdu)
{
    int length = pdu.ComputedLength();
    pdu.Serialize(buffer.AsSpan(0, length));
    return Convert.ToBase64String(buffer.AsSpan(0, length));
}

app.MapGet("/api/entities", () => entities.ToList());

app.MapPost("/api/entities", (WebEntityRequest req) =>
{
    if (entities.Any(e => e.Id == req.Id))
        return Results.BadRequest(new { error = "Entity ID already exists" });

    var entity = new WebEntity
    {
        Id = req.Id,
        Pattern = req.Pattern,
        Speed = req.Speed,
        Position = new Position { X = req.Id * 100.0, Y = 0, Z = 0 }
    };
    entities.Add(entity);
    pduLog.Insert(0, $"[{DateTime.Now:HH:mm:ss}] Added entity {req.Id}");
    return Results.Ok(entity);
});

app.MapDelete("/api/entities/{id}", (int id) =>
{
    var entity = entities.FirstOrDefault(e => e.Id == id);
    if (entity == null) return Results.NotFound();
    entities.Remove(entity);
    pduLog.Insert(0, $"[{DateTime.Now:HH:mm:ss}] Removed entity {id}");
    return Results.Ok();
});

app.MapGet("/api/log", () => pduLog.Take(100).ToList());

app.MapPost("/api/start", () =>
{
    isRunning = true;
    pduLog.Insert(0, $"[{DateTime.Now:HH:mm:ss}] Started sending PDUs at {rate} Hz");
    return Results.Ok(new { status = "running" });
});

app.MapPost("/api/stop", () =>
{
    isRunning = false;
    pduLog.Insert(0, $"[{DateTime.Now:HH:mm:ss}] Stopped sending PDUs");
    return Results.Ok(new { status = "stopped" });
});

app.MapGet("/api/status", () => new
{
    isRunning,
    rate,
    multicastAddress,
    port,
    entityCount = entities.Count
});

app.MapPost("/api/config", (ConfigRequest req) =>
{
    if (req.Rate > 0) rate = req.Rate;
    if (!string.IsNullOrEmpty(req.MulticastAddress)) multicastAddress = req.MulticastAddress;
    if (req.Port > 0) port = req.Port;
    return Results.Ok(new { rate, multicastAddress, port });
});

app.MapPost("/api/fire", (FireRequest req) =>
{
    var pdu = FirePdu.Create()
        .WithEntityId(EntityId.Relative(req.EntityId))
        .WithTargetEntityId(EntityId.Relative(req.TargetId))
        .WithMunitionId(EntityId.Relative(0))
        .WithEventId(EntityId.Relative(1))
        .WithFireMissionIndex((uint)req.MissionIndex)
        .WithLocation(req.LocationX, req.LocationY, req.LocationZ)
        .WithVelocity(req.VelocityX, req.VelocityY, req.VelocityZ)
        .WithSimulationFederation(1, 1)
        .Build();

    var data = new byte[pdu.ComputedLength()];
    pdu.Serialize(data);
    var endpoint = new IPEndPoint(IPAddress.Parse(multicastAddress), port);
    sender.Send(data, data.Length, endpoint);
    
    pduLog.Insert(0, $"[{DateTime.Now:HH:mm:ss}] FIRE: Entity {req.EntityId} -> Target {req.TargetId}");
    return Results.Ok(new { message = "Fire PDU sent" });
});

app.MapPost("/api/sim/start", () =>
{
    var pdu = StartResumePdu.Create()
        .WithRequestId(1)
        .WithRealTime(0)
        .WithSimulationTime(0)
        .WithLevel(1)
        .WithSimulationFederation(1, 1)
        .Build();

    var data = new byte[pdu.ComputedLength()];
    pdu.Serialize(data);
    var endpoint = new IPEndPoint(IPAddress.Parse(multicastAddress), port);
    sender.Send(data, data.Length, endpoint);
    
    pduLog.Insert(0, $"[{DateTime.Now:HH:mm:ss}] START/RESUME simulation");
    return Results.Ok(new { message = "Start PDU sent" });
});

app.MapPost("/api/sim/stop", () =>
{
    var pdu = StopFreezePdu.Create()
        .WithRequestId(1)
        .WithRealTime(0)
        .WithSimulationTime(0)
        .WithReason(1)
        .WithSimulationFederation(1, 1)
        .Build();

    var data = new byte[pdu.ComputedLength()];
    pdu.Serialize(data);
    var endpoint = new IPEndPoint(IPAddress.Parse(multicastAddress), port);
    sender.Send(data, data.Length, endpoint);
    
    pduLog.Insert(0, $"[{DateTime.Now:HH:mm:ss}] STOP/FREEZE simulation");
    return Results.Ok(new { message = "Stop PDU sent" });
});

var timer = new System.Threading.Timer(_ =>
{
    if (!isRunning || entities.Count == 0) return;
    
    var endpoint = new IPEndPoint(IPAddress.Parse(multicastAddress), port);
    
    foreach (var entity in entities)
    {
        entity.Tick(1.0 / rate);
        
        var pdu = EntityStatePdu.Create()
            .WithEntityId(EntityId.Relative(entity.Id))
            .WithEntityType(EntityType.PhysicalWithLocation)
            .WithLinearPosition(entity.Position.X, entity.Position.Y, entity.Position.Z)
            .WithLinearVelocity(
                entity.Pattern == "Linear" ? entity.Speed : 0,
                entity.Pattern == "Circle" ? entity.Speed : 0,
                0)
            .WithSimulationFederation(1, 1)
            .Build();

        var data = new byte[pdu.ComputedLength()];
        pdu.Serialize(data);
        
        try { sender.Send(data, data.Length, endpoint); }
        catch { }
    }
}, null, 0, 1000 / rate);

app.Run();

public class WebEntity
{
    public int Id { get; set; }
    public string Pattern { get; set; } = "Linear";
    public double Speed { get; set; } = 10;
    public Position Position { get; set; } = new();
    private double _angle;
    private double _circleCenterX;
    private double _circleCenterY;

    public void Tick(double deltaSeconds)
    {
        switch (Pattern)
        {
            case "Linear":
                Position.X += Speed * deltaSeconds;
                break;
            case "Circle":
                const double radius = 50.0;
                _angle += (Speed / radius) * deltaSeconds;
                Position.X = _circleCenterX + Math.Cos(_angle) * radius;
                Position.Y = _circleCenterY + Math.Sin(_angle) * radius;
                break;
        }
    }
}

public class Position { public double X, Y, Z; }

public class WebEntityRequest
{
    public int Id { get; set; }
    public string Pattern { get; set; } = "Linear";
    public double Speed { get; set; } = 10;
}

public class ConfigRequest
{
    public int Rate { get; set; }
    public string? MulticastAddress { get; set; }
    public int Port { get; set; }
}

public class FireRequest
{
    public int EntityId { get; set; }
    public int TargetId { get; set; }
    public int MissionIndex { get; set; }
    public double LocationX { get; set; }
    public double LocationY { get; set; }
    public double LocationZ { get; set; }
    public double VelocityX { get; set; }
    public double VelocityY { get; set; }
    public double VelocityZ { get; set; }
}
