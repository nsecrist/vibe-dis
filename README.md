# SisoDis.NET

A modern C# implementation of the IEEE 1278.1-2012 DIS (Distributed Interactive Simulation) protocol, targeting .NET 10+.

## Quick Start

```csharp
using SisoDis.Core.Common;
using SisoDis.Core.Pdu;

// Build an Entity State PDU
var pdu = EntityStatePdu.Create()
    .WithEntityId(EntityId.Relative(42))
    .WithEntityType(EntityType.PhysicalWithLocation)
    .WithLinearPosition(47.6062, -122.3321, 50.0)
    .WithAngularOrientation(0.0, 0.0, Math.PI / 4)
    .WithLinearVelocity(10.0, 5.0, 0.0)
    .WithAngularVelocity(0.0, 0.0, 0.01)
    .WithSimulationFederation(1, 1)
    .WithAdditionalState(new EntityStatePduAdditionalState())
    .WithNumberOfParts(0)
    .Build();

// Serialize to bytes (header + body)
byte[] buffer = new byte[pdu.ComputedLength()];
pdu.Serialize(buffer);

// Deserialize from bytes
var received = EntityStatePdu.Deserialize(buffer.AsSpan());
Console.WriteLine($"Entity {received.EntityId.Value} at ({received.LinearPosition.X}, {received.LinearPosition.Y}, {received.LinearPosition.Z})");
```

## Core Concepts

### Common Types

```csharp
// Entity identifier — 2-byte unsigned value
var absolute = EntityId.Absolute(12345);
var relative = EntityId.Relative(42);    // validated: 0..65535

// 3D vector — three doubles for position, velocity, orientation
var position = new Vector3Double(47.6062, -122.3321, 50.0);
var zero     = Vector3Double.Zero;
var manual   = Vector3Double.FromValues(1.0, 2.0, 3.0);

// Entity classification
EntityType type = EntityType.PhysicalWithLocation;
```

### Serialization

Every PDU implements `IPdu` which provides:
- `ComputedLength()` — total byte size including 6-byte header
- `SerializeBody(Span<byte>, int)` — writes body fields only
- `Serialize(Span<byte>, int)` — writes header + body (extension method)

All serialization uses big-endian byte order per IEEE 1278.1-2012 via `BinaryPrimitives`. Serialize methods operate on `Span<byte>` for zero-allocation hot paths.

```csharp
// Allocate exact buffer size
byte[] buffer = new byte[pdu.ComputedLength()];

// Full serialize (header + body) — use this for network transmission
pdu.Serialize(buffer);

// With offset into a larger buffer
byte[] networkBuffer = new byte[4096];
pdu.Serialize(networkBuffer, offset: 128);
```

### Deserialization

Each PDU type has a static `Deserialize` method that validates the header and parses the body:

```csharp
// Type-specific deserialization — validates magic, version, and PDU type
var entityState = EntityStatePdu.Deserialize(buffer.AsSpan());
var collision   = CollisionPdu.Deserialize(buffer.AsSpan());

// Throws DisValidationException on invalid magic, version, or type mismatch
// Throws ArgumentException if buffer is too small
```

### PDU Factory

`PduFactory` maps type codes to PDU types. All implemented PDUs are registered automatically.

```csharp
// Create a PDU instance by type code
IPdu pdu = PduFactory.CreatePdu(1); // EntityStatePdu

// Check if a type code is registered
bool exists = PduFactory.IsRegistered(1); // true

// List all registered types
var all = PduFactory.GetAllRegisteredPduTypes();
foreach (var (code, type) in all)
    Console.WriteLine($"Type {code}: {type.Name}");

// Throws DisUnknownPduException for unregistered type codes
```

## Implemented PDUs

<!-- UPDATE THIS TABLE WHEN ADDING NEW PDUs -->

| PDU | Type Code | Class | IEEE Section |
|-----|-----------|-------|-------------|
| Entity State | 1 | `EntityStatePdu` | §5.3.3.1 |
| Fire | 2 | `FirePdu` | §5.3.3 |
| Detonation | 3 | `DetonationPdu` | §5.3.4 |
| Collision | 4 | `CollisionPdu` | §5.3.4 |
| Collision-Elastic | 5 | `CollisionElasticPdu` | §5.3.5 |
| Entity State Update | 6 | `EntityStateUpdatePdu` | §5.3.6 |
| Attribute | 7 | `AttributePdu` | §5.3.7 |
| Create Entity | 23 | `CreateEntityPdu` | §5.3.6.1 |
| Remove Entity | 24 | `RemoveEntityPdu` | §5.3.6.2 |
| Start/Resume | 25 | `StartResumePdu` | §5.3.6.3 |
| Stop/Freeze | 26 | `StopFreezePdu` | §5.3.6.4 |
| Acknowledge | 27 | `AcknowledgePdu` | §5.3.6.5 |
| Action Request | 28 | `ActionRequestPdu` | §5.3.6.6 |
| Action Response | 29 | `ActionResponsePdu` | §5.3.6.7 |
| Data Query | 30 | `DataQueryPdu` | §5.3.6.8 |
| Munition | 20 | `MunitionPdu` | §5.3.10 |
| Designator | 21 | `DesignatorPdu` | §5.3.11 |
| Electromagnetic Emission | 22 | `ElectromagneticEmissionPdu` | §5.3.12 |

## PDU Examples

### Entity State PDU

The primary PDU for reporting entity position, orientation, and velocity.

```csharp
// Build with all fields
var pdu = EntityStatePdu.Create()
    .WithEntityId(EntityId.Relative(100))
    .WithEntityType(EntityType.PhysicalWithLocation)
    .WithLinearPosition(47.6062, -122.3321, 50.0)
    .WithAngularOrientation(0.1, 0.2, 0.3)
    .WithLinearVelocity(10.0, 5.0, 0.0)
    .WithAngularVelocity(0.01, 0.02, 0.03)
    .WithSimulationFederation(1, 2)
    .WithAdditionalState(new EntityStatePduAdditionalState(
        Flags: 0x01,           // killed flag
        AmmoState: 50,
        LaunchIndicator: 1234,
        EmitterState: 5678,
        ArticulationCount: 0,
        ArticulatedPartId: 0
    ))
    .WithNumberOfParts(0)
    .Build();

// Round-trip
byte[] buf = new byte[pdu.ComputedLength()];
pdu.Serialize(buf);
var rx = EntityStatePdu.Deserialize(buf.AsSpan());

// Inspect additional state flags
Console.WriteLine($"Killed: {rx.AdditionalState.IsKilled}");
Console.WriteLine($"Damaged: {rx.AdditionalState.IsDamaged}");
```

### Collision PDU

Reports a collision between two entities.

```csharp
var pdu = CollisionPdu.Create()
    .WithEntityId(EntityId.Relative(42))
    .WithImpactLocation(100.0, 200.0, 0.0)
    .WithVelocityBeforeImpact(15.0, -10.0, 0.0)
    .WithSimulationFederation(1, 1)
    .Build();

byte[] buf = new byte[pdu.ComputedLength()];
pdu.Serialize(buf);
var rx = CollisionPdu.Deserialize(buf);
```

### Collision-Elastic PDU

Reports an elastic collision with before/after velocities for both entities.

```csharp
var pdu = CollisionElasticPdu.Create()
    .WithEntityId(EntityId.Relative(42))
    .WithImpactLocation(100.0, 200.0, 0.0)
    .WithVelocityBeforeImpactA(15.0, -10.0, 0.0)
    .WithVelocityAfterImpactA(5.0, -3.0, 0.0)
    .WithVelocityBeforeImpactB(-8.0, 4.0, 0.0)
    .WithVelocityAfterImpactB(-2.0, 1.0, 0.0)
    .WithSimulationFederation(1, 1)
    .Build();

byte[] buf = new byte[pdu.ComputedLength()];
pdu.Serialize(buf);
var rx = CollisionElasticPdu.Deserialize(buf);
```

### Entity State Update PDU

Bandwidth-optimized update — only sends fields that changed, indicated by flags.

```csharp
// Only update position and velocity (other fields omitted from wire)
var pdu = EntityStateUpdatePdu.Create()
    .WithEntityId(EntityId.Relative(42))
    .WithLinearPosition(48.0, -123.0, 55.0)
    .WithLinearVelocity(12.0, 6.0, 0.5)
    .WithSimulationFederation(1, 1)
    .Build();

// Flags are set automatically by the builder
Console.WriteLine(pdu.Flags.HasFlag(EntityStateUpdateFlags.LinearPosition));  // true
Console.WriteLine(pdu.Flags.HasFlag(EntityStateUpdateFlags.EntityType));      // false

byte[] buf = new byte[pdu.ComputedLength()];
pdu.Serialize(buf);
var rx = EntityStateUpdatePdu.Deserialize(buf);
```

### Attribute PDU

Transmits variable-length entity attribute data.

```csharp
var pdu = AttributePdu.Create()
    .WithEntityId(EntityId.Relative(42))
    .WithSimulationFederation(1, 1)
    .WithAttribute(100, new byte[] { 0x01, 0x02, 0x03 })
    .WithAttribute(200, new byte[] { 0xFF })
    .Build();

byte[] buf = new byte[pdu.ComputedLength()];
pdu.Serialize(buf);
var rx = AttributePdu.Deserialize(buf);

foreach (var attr in rx.Attributes)
    Console.WriteLine($"Attribute {attr.AttributeNumber}: {attr.Data.Length} bytes");
```

### Fire PDU

Reports the firing of a weapon by an entity.

```csharp
var pdu = FirePdu.Create()
    .WithEntityId(EntityId.Relative(42))
    .WithTargetEntityId(EntityId.Relative(100))
    .WithMunitionId(EntityId.Relative(200))
    .WithEventId(EntityId.Relative(1))
    .WithFireMissionIndex(5)
    .WithLocation(10.5, 20.7, 30.9)
    .WithVelocity(1.0, -2.0, 3.0)
    .WithSimulationFederation(1, 1)
    .Build();

byte[] buf = new byte[pdu.ComputedLength()];
pdu.Serialize(buf);
var rx = FirePdu.Deserialize(buf);

Console.WriteLine($"Firing entity: {rx.EntityId.Value}");
Console.WriteLine($"Target: {rx.TargetEntityId.Value}");
Console.WriteLine($"Velocity: ({rx.Velocity.X}, {rx.Velocity.Y}, {rx.Velocity.Z})");
```

### Detonation PDU

Reports the detonation of a munition.

```csharp
var pdu = DetonationPdu.Create()
    .WithEntityId(EntityId.Relative(42))
    .WithTargetEntityId(EntityId.Relative(100))
    .WithMunitionId(EntityId.Relative(200))
    .WithEventId(EntityId.Relative(1))
    .WithVelocity(1.0, -2.0, 3.0)
    .WithLocation(10.5, 20.7, 30.9)
    .WithResult(DetonationResult.Impact)
    .WithSimulationFederation(1, 1)
    .Build();

byte[] buf = new byte[pdu.ComputedLength()];
pdu.Serialize(buf);
var rx = DetonationPdu.Deserialize(buf);

Console.WriteLine($"Detonation by: {rx.EntityId.Value}");
Console.WriteLine($"Result: {rx.Result}");
Console.WriteLine($"Location: ({rx.Location.X}, {rx.Location.Y}, {rx.Location.Z})");
```

### Munition PDU

Communicates the firing of a weapon (similar to Fire PDU but used in different contexts per IEEE 1278.1-2012 §5.3.10).

```csharp
var pdu = MunitionPdu.Create()
    .WithEntityId(EntityId.Relative(42))
    .WithTargetEntityId(EntityId.Relative(100))
    .WithMunitionId(EntityId.Relative(200))
    .WithEventId(EntityId.Relative(1))
    .WithFireMissionIndex(10)
    .WithLocation(5.0, 10.0, 15.0)
    .WithVelocity(0.5, 1.0, 0.0)
    .WithSimulationFederation(1, 1)
    .Build();

byte[] buf = new byte[pdu.ComputedLength()];
pdu.Serialize(buf);
var rx = MunitionPdu.Deserialize(buf);

Console.WriteLine($"Munition fired by: {rx.EntityId.Value}");
Console.WriteLine($"Fire mission: {rx.FireMissionIndex}");
```

### Designator PDU

Reports the designation of an entity or location by a designator (e.g., laser designator).

```csharp
var pdu = DesignatorPdu.Create()
    .WithEntityId(EntityId.Relative(42))
    .WithTargetEntityId(EntityId.Relative(100))
    .WithDesignatorLocation(1.0, 2.0, 3.0)
    .WithDesignatorOrientation(0.1, 0.2, 0.3)
    .WithEntityLocation(10.5, 20.7, 30.9)
    .WithDesignatorCode(5)
    .WithDesignatorOutput(200)
    .WithSimulationFederation(1, 1)
    .Build();

byte[] buf = new byte[pdu.ComputedLength()];
pdu.Serialize(buf);
var rx = DesignatorPdu.Deserialize(buf);

Console.WriteLine($"Designator: {rx.EntityId.Value}");
Console.WriteLine($"Target: {rx.TargetEntityId.Value}");
Console.WriteLine($"Designator code: {rx.DesignatorCode}");
```

### Electromagnetic Emission PDU

Reports electromagnetic emission data from an entity (e.g., radar, jammer).

```csharp
var pdu = ElectromagneticEmissionPdu.Create()
    .WithEntityId(EntityId.Relative(42))
    .WithEmitterNumber(5)
    .WithEmitterLocation(100)
    .WithSimulationFederation(1, 1)
    .WithNumberOfSystems(2)
    .WithNumberOfEmissionSystems(3)
    .Build();

byte[] buf = new byte[pdu.ComputedLength()];
pdu.Serialize(buf);
var rx = ElectromagneticEmissionPdu.Deserialize(buf);

Console.WriteLine($"Emitter: {rx.EntityId.Value}");
Console.WriteLine($"Emitter number: {rx.EmitterNumber}");
Console.WriteLine($"Systems: {rx.NumberOfSystems}");
```

### Simulation Management PDUs

The Simulation Management family (types 23-30) controls exercise state and entity lifecycle.

```csharp
// Create Entity - Request to create an entity
var createPdu = CreateEntityPdu.Create()
    .WithRequestId(1)
    .WithNumberOfParts(1)
    .WithEntityId(EntityId.Relative(100))
    .WithSimulationFederation(1, 1)
    .Build();

// Remove Entity - Request to remove an entity
var removePdu = RemoveEntityPdu.Create()
    .WithRequestId(2)
    .WithEntityId(EntityId.Relative(100))
    .WithSimulationFederation(1, 1)
    .Build();

// Start/Resume - Begin or resume simulation
var startPdu = StartResumePdu.Create()
    .WithRequestId(3)
    .WithRealTime(1000)
    .WithSimulationTime(500)
    .WithLevel(1)
    .WithSimulationFederation(1, 1)
    .Build();

// Stop/Freeze - Stop or freeze simulation
var stopPdu = StopFreezePdu.Create()
    .WithRequestId(4)
    .WithRealTime(2000)
    .WithSimulationTime(1000)
    .WithReason(1)  // 1 = stop, 2 = freeze
    .WithSimulationFederation(1, 1)
    .Build();

// Acknowledge - Respond to simulation management requests
var ackPdu = AcknowledgePdu.Create()
    .WithRequestId(1)
    .WithResponseFlag(1)  // 1 = received, 2 = understand
    .WithAcknowledgeFlag(1)
    .WithSimulationFederation(1, 1)
    .Build();

// Action Request - Request specific action
var actionReqPdu = ActionRequestPdu.Create()
    .WithRequestId(5)
    .WithActionId(100)
    .WithNumberOfFixedDatum(0)
    .WithNumberOfVariableDatum(0)
    .WithSimulationFederation(1, 1)
    .Build();

// Action Response - Response to action request
var actionRespPdu = ActionResponsePdu.Create()
    .WithRequestId(5)
    .WithActionId(100)
    .WithResponseFlag(1)
    .WithNumberOfFixedDatum(0)
    .WithNumberOfVariableDatum(0)
    .WithSimulationFederation(1, 1)
    .Build();

// Data Query - Request data from another application
var dataQueryPdu = DataQueryPdu.Create()
    .WithRequestId(6)
    .WithTimeInterval(100)
    .WithNumberOfFixedDatum(0)
    .WithNumberOfVariableDatum(0)
    .WithSimulationFederation(1, 1)
    .Build();
```

## Applications

This solution includes three applications for DIS simulation:

### SisoDis.ConsoleProducer

A Terminal.Gui based console application for producing DIS PDUs.

**Run:**
```bash
dotnet run --project SisoDis.ConsoleProducer
```

**Features:**
- Entity management with movement patterns (Linear, Stationary, Circle)
- Real-time entity state PDU transmission
- Fire, Munition, Designator PDU dialogs
- Start/Resume and Stop/Freeze simulation control
- Configurable multicast address and port
- Auto-incrementing Entity IDs
- Duplicate entity validation

**Keyboard Shortcuts:**
| Key | Action |
|-----|--------|
| Enter | Add entity |
| Delete | Remove selected entity |
| F2 | Fire PDU dialog |
| F3 | Munition PDU dialog |
| F4 | Designator PDU dialog |
| F5 | Start simulation |
| F6 | Stop simulation |
| F7 | Start/Resume simulation PDU |
| F8 | Stop/Freeze simulation PDU |
| Ctrl+Q | Quit |

### SisoDis.WebProducer

A modern web-based application for producing DIS PDUs.

**Run:**
```bash
dotnet run --project SisoDis.WebProducer
```

Then open http://localhost:5000 in your browser.

**Features:**
- Modern dark-themed web interface
- Form controls for entity settings (dropdowns, inputs)
- Real-time entity list with position updates
- Start/Stop simulation control
- Start/Resume and Stop/Freeze PDU buttons
- Configurable multicast address, port, and rate
- Activity log with PDU history

**API Endpoints:**
| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/entities` | GET | List all entities |
| `/api/entities` | POST | Add new entity |
| `/api/entities/{id}` | DELETE | Remove entity |
| `/api/start` | POST | Start simulation |
| `/api/stop` | POST | Stop simulation |
| `/api/sim/start` | POST | Send Start/Resume PDU |
| `/api/sim/stop` | POST | Send Stop/Freeze PDU |
| `/api/config` | POST | Update network config |
| `/api/status` | GET | Get current status |
| `/api/log` | GET | Get activity log |

### SisoDis.Receiver

A UDP multicast receiver that listens for DIS PDUs.

**Run:**
```bash
dotnet run --project SisoDis.Receiver
```

**Features:**
- Subscribes to configurable multicast address/port
- Deserializes all implemented PDU types
- Displays PDU type, entity ID, and timestamp
- Auto-detects PDU type from header

### Integration Testing

Use the provided script to run Producer and Receiver together in tmux:

```bash
# Console producer
./test-integration.sh

# Web producer
./test-integration.sh web
```

## Error Handling

```csharp
try
{
    var pdu = EntityStatePdu.Deserialize(badBuffer.AsSpan());
}
catch (DisValidationException ex)
{
    // Invalid magic, protocol version, or PDU type
    Console.WriteLine(ex.Message);
}
catch (ArgumentException ex)
{
    // Buffer too small
    Console.WriteLine(ex.Message);
}

try
{
    IPdu pdu = PduFactory.CreatePdu(999);
}
catch (DisUnknownPduException ex)
{
    // Unregistered PDU type code
    Console.WriteLine($"Unknown PDU type: 0x{ex.PdType:X4}");
}
```

## Header Format

All PDUs share a 6-byte header per IEEE 1278.1-2012 §5.3.1:

```
Offset  Size  Field
0       1     Magic (always 0x01)
1       1     Protocol Version (3 = IEEE 1278.1-2012)
2       2     Reserved (0x0000)
4       2     PDU Type (big-endian unsigned short)
```
