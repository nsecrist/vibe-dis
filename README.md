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
| Collision | 4 | `CollisionPdu` | §5.3.4 |
| Collision-Elastic | 5 | `CollisionElasticPdu` | §5.3.5 |
| Entity State Update | 6 | `EntityStateUpdatePdu` | §5.3.6 |
| Attribute | 7 | `AttributePdu` | §5.3.7 |

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
