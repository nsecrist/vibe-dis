# EntityStatePdu Implementation Plan

## Overview
Implement `EntityStatePdu` per IEEE 1278.1-2012 §5.3.3.1 - the fundamental DIS PDU for entity state updates.

## Current Infrastructure Status ✓

### Core Classes (Complete)
- **IPdu** (`SisoDis/Core/Pdu/IPdu.cs`) - Interface contract with Magic, ProtocolVersion, PdType
- **PduBase** (`SisoDis/Core/Pdu/PduBase.cs`) - Abstract base with serialization infrastructure
- **PduHeader** (`SisoDis/Core/Pdu/PduHeader.cs`) - Header serialization/deserialization (6 bytes)
- **PduFactory** (`SisoDis/Core/Pdu/PduFactory.cs`) - Registry pattern for PDU instantiation
- **EntityId** (`SisoDis/Core/Common/EntityId.cs`) - Entity identifier with validation

### Supporting Classes (Complete)
- **SpanHelpers** - Zero-allocation serialization helpers using BinaryPrimitives
- **DisValidationException** / **DisUnknownPduException** - Exception types
- **Vector3Double** - 3D coordinate type for position data

### Unit Tests ✓
- PduHeaderTests: 8 tests (constructor, serialize, deserialize)
- EntityIdTests: 5 tests (factory methods, validation)
- All tests passing: 17/17 ✓

## IEEE 1278.1-2012 §5.3.3.1 EntityStatePdu Layout

```
Total Length: Variable (depends on articulated parts)
Fixed Fields: 14 bytes + variable for articulated parts
┌───────────────────────────────┬────────┐
│ EntityID                      │ 2 byte │
├───────────────────────────────┼────────┤
│ PAD1                          │ 1 byte │
├───────────────────────────────┼────────┤
│ Protocol ID                   │ 1 byte │ (fixed = 1)
├───────────────────────────────┼────────┤
│ EntityType                    │ 2 byte │
├───────────────────────────────┼────────┤
│ LinearVelocity                │ 6 byte │ (X, Y, Z doubles)
├───────────────────────────────┼────────┤
│ Position                      │ 24 byte│ (lat/long/alt doubles)
├───────────────────────────────┼────────┤
│ LinearAcceleration            │ 6 byte │
├───────────────────────────────┼────────┤
│ AngularVelocity               │ 6 byte │
├───────────────────────────────┼────────┤
│ AngularAcceleration             │ 6 byte │
├───────────────────────────────┼────────┤
│ Heading                       │ 2 byte │ (degrees, scaled)
├───────────────────────────────┼────────┤
│ Pitch                         │ 2 byte │
├───────────────────────────────┼────────┤
│ Roll                          │ 2 byte │
├───────────────────────────────┼────────┤
│ ScalarData                    │ 1 byte │ (flags)
├───────────────────────────────┼────────┤
│ VectorDataLength              │ 2 byte │
├───────────────────────────────┼────────┤
│ VectorData                    │ varies │ (VectorDatum array)
├───────────────────────────────┼────────┤
│ MarkedPduDummy                │ 1 byte │
├───────────────────────────────┼────────┤
│ NumberOfArticulationParameters│ 2 byte │
├───────────────────────────────┼────────┤
│ ArticulationParameters        │ varies │ (ArticulatedPart array)
└───────────────────────────────┴────────┘
```

## Implementation Steps

### Step 1: Create EntityStatePdu Record (Priority: HIGH)
**File:** `SisoDis/Core/Pdu/EntityStatePdu.cs`

```csharp
public record EntityStatePdu : PduBase, IPdu
{
    public override ushort PdType => 0x0004; // Entity State PDU type code
    
    // Required fields
    public EntityId EntityID { get; init; } = new(0);
    
    // Optional fields (per §5.3.3.1)
    public byte[]? Pad1 { get; init; }
    public byte ProtocolIdentifier { get; set; } = 1;
    public EntityType EntityType { get; set; }
    public Vector3Double LinearVelocity { get; set; }
    public Vector3Double Position { get; set; }
    
    // ComputedLength must account for all variable-length fields
    public override int ComputedLength() => 14 + (VectorData?.Length ?? 0) * 6 
        + (ArticulationParameters?.Length ?? 0) * 8;
}
```

**Validation Rules:**
- ProtocolIdentifier must be 1 per IEEE spec
- Position lat/long in radians, alt in meters WGS-84
- Angular values in degrees × 100 (scaled)

### Step 2: Implement Serialize/DeserializeBody (Priority: HIGH)
Use `SpanHelpers` for zero-allocation serialization:
```csharp
public override void SerializeBody(Span<byte> buffer, int offset)
{
    SpanHelpers.WriteUInt16(buffer.Slice(offset), 0, (ushort)EntityID.Value);
    // Continue with all fixed fields...
}

public override void DeserializeBody(ReadOnlySpan<byte> buffer, int offset)
{
    EntityID = new EntityId(SpanHelpers.ReadUInt16(buffer.Slice(offset)));
    // Continue deserialization...
}
```

### Step 3: Create EntityStatePdu Builder (Priority: MEDIUM)
**File:** `SisoDis/Core/Pdu/EntityStatePdu+Builder.cs`

```csharp
public static class EntityStatePduBuilder 
{
    public static EntityStatePdu Create() => new();
    
    public static EntityStatePdu Build(EntityId id, EntityType type, Vector3Double position)
        => new() { EntityID = id, EntityType = type, Position = position };
}
```

### Step 4: Register with PduFactory (Priority: HIGH)
**File:** `SisoDis/Core/Pdu/PduFactory.cs` - Add registration in static constructor or initialization method

```csharp
static PduFactory()
{
    RegisterPduType(0x0004, typeof(EntityStatePdu));
}
```

### Step 5: Create Unit Tests (Priority: HIGH)
**File:** `SisoDis.Tests/Core/EntityStatePduTests.cs`

Test cases required:
- `[Fact] RoundTrip_PreservesAllFields()` - Build → Serialize → Deserialize → Compare
- `[Theory] ProtocolIdentifier_Invalid_ThrowsException(byte invalidValue)` - Validation test
- `[Fact] ComputedLength_CalculatesCorrectly()` - Length calculation verification
- `[Fact] VectorData_EmptyArray_NotIncludedInLength()` - Variable-length field handling

### Step 6: Implement Helper Types (Priority: LOW)
**File:** `SisoDis/Core/Common/VectorDatum.cs` - For vector data array elements
**File:** `SisoDis/Core/Common/ArticulatedPart.cs` - For articulated parts

## Testing Strategy

1. **Round-trip tests**: Build PDU → Serialize to byte[] → Deserialize → Deep equals check
2. **Validation tests**: Invalid values should throw DisValidationException
3. **Boundary tests**: Zero values, max values for all fields
4. **Variable-length handling**: Empty vs populated VectorData/ArticulationParameters

## Dependencies on IEEE Spec

Per §5.3.3.1 and Appendix B:
- Protocol identifier = 1 (fixed)
- Position coordinates in WGS-84 datum
- Heading/Pitch/Roll scaled by ×100
- Entity ID relative or absolute per reference type

## Acceptance Criteria

- [ ] EntityStatePdu compiles without warnings
- [ ] All unit tests pass (target: 90%+ code coverage)
- [ ] Round-trip serialization verified for all field combinations
- [ ] Validation exceptions thrown for invalid values
- [ ] PduFactory successfully instantiates EntityStatePdu from type code

## Timeline Estimate

| Task | Estimated Time |
|------|---------------|
| Create record + properties | 30 min |
| Serialize/DeserializeBody | 1.5 hours |
| Builder pattern | 30 min |
| Factory registration | 15 min |
| Unit tests (round-trip) | 1 hour |
| Unit tests (validation) | 45 min |
| **Total** | ~4 hours |

## Next Steps After EntityStatePdu

Once complete, implement additional PDUs in priority order:
1. **FireEventPdu** (§5.3.6) - Weapon firing events
2. **DetonationPdu** (§5.3.7) - Explosion effects  
3. **CollidePdu** (§5.3.8) - Entity collisions
4. **UpdateRemovePdu** (§5.3.9) - State updates/removals
