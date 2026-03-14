using System;
using System.Buffers.Binary;
using SisoDis.Core.Common;
using SisoDis.Core.Serialization;

namespace SisoDis.Core.Pdu;

/// <summary>
/// Represents DIS Munition PDU (IEEE 1278.1-2012 §5.3.10).
/// Used to communicate the firing of a weapon.
/// </summary>
public record struct MunitionPdu(
    EntityId EntityId,
    EntityId TargetEntityId,
    EntityId MunitionId,
    EntityId EventId,
    uint FireMissionIndex,
    Vector3Double Location,
    Vector3Double Velocity,
    byte SimulationReference,
    byte FederationReference
) : IPdu
{
    /// <summary>PDU Type code for Munition PDU per IEEE 1278.1-2012 Table 5-4.</summary>
    public const ushort PdTypeValue = 20;

    public byte Magic => 1;
    public byte ProtocolVersion => 3;
    public ushort PdType => PdTypeValue;

    public int ComputedLength() => PduHeader.HeaderLength + 62;

    public void SerializeBody(Span<byte> buffer, int offset)
    {
        BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, 2), (ushort)EntityId.Value);
        offset += 2;

        BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, 2), (ushort)TargetEntityId.Value);
        offset += 2;

        BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, 2), (ushort)MunitionId.Value);
        offset += 2;

        BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, 2), (ushort)EventId.Value);
        offset += 2;

        BinaryPrimitives.WriteUInt32BigEndian(buffer.Slice(offset, 4), FireMissionIndex);
        offset += 4;

        SerializeDouble(buffer.Slice(offset), Location.X);
        SerializeDouble(buffer.Slice(offset + 8), Location.Y);
        SerializeDouble(buffer.Slice(offset + 16), Location.Z);
        offset += 24;

        SerializeDouble(buffer.Slice(offset), Velocity.X);
        SerializeDouble(buffer.Slice(offset + 8), Velocity.Y);
        SerializeDouble(buffer.Slice(offset + 16), Velocity.Z);
        offset += 24;

        buffer[offset] = SimulationReference;
        offset++;

        buffer[offset] = FederationReference;
    }

    public static MunitionPdu Deserialize(ReadOnlySpan<byte> buffer, int offset = 0)
    {
        if (buffer.Length < offset + PduHeader.HeaderLength)
            throw new ArgumentException("Buffer too small for header", nameof(buffer));

        byte magic = buffer[offset];
        byte versionMajor = buffer[offset + 1];

        if (magic != 1)
            throw new DisValidationException($"Invalid magic: expected 1, got {magic}");

        if (versionMajor != 3)
            throw new DisValidationException($"Invalid protocol version: expected 3, got {versionMajor}");

        ushort actualType = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(offset + 4, 2));
        if (actualType != PdTypeValue)
            throw new DisValidationException($"Invalid PDU type: expected {PdTypeValue}, got {actualType}");

        int pos = offset + PduHeader.HeaderLength;

        ushort entityIdValue = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(pos, 2));
        EntityId entityId = new EntityId(entityIdValue);
        pos += 2;

        ushort targetIdValue = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(pos, 2));
        EntityId targetId = new EntityId(targetIdValue);
        pos += 2;

        ushort munitionIdValue = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(pos, 2));
        EntityId munitionId = new EntityId(munitionIdValue);
        pos += 2;

        ushort eventIdValue = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(pos, 2));
        EntityId eventId = new EntityId(eventIdValue);
        pos += 2;

        uint fireMissionIndex = BinaryPrimitives.ReadUInt32BigEndian(buffer.Slice(pos, 4));
        pos += 4;

        Vector3Double location = new Vector3Double(
            ReadDouble(buffer.Slice(pos)),
            ReadDouble(buffer.Slice(pos + 8)),
            ReadDouble(buffer.Slice(pos + 16))
        );
        pos += 24;

        Vector3Double velocity = new Vector3Double(
            ReadDouble(buffer.Slice(pos)),
            ReadDouble(buffer.Slice(pos + 8)),
            ReadDouble(buffer.Slice(pos + 16))
        );
        pos += 24;

        byte simulationRef = buffer[pos];
        pos++;

        byte federationRef = buffer[pos];

        return new MunitionPdu(
            entityId,
            targetId,
            munitionId,
            eventId,
            fireMissionIndex,
            location,
            velocity,
            simulationRef,
            federationRef
        );
    }

    public static Builder Create() => new();

    private static void SerializeDouble(Span<byte> buffer, double value)
        => BinaryPrimitives.WriteDoubleBigEndian(buffer, value);

    private static double ReadDouble(ReadOnlySpan<byte> buffer)
        => BinaryPrimitives.ReadDoubleBigEndian(buffer.Slice(0, 8));

    public class Builder
    {
        private EntityId _entityId = new(0);
        private EntityId _targetEntityId = new(0);
        private EntityId _munitionId = new(0);
        private EntityId _eventId = new(0);
        private uint _fireMissionIndex = 0;
        private Vector3Double _location = new();
        private Vector3Double _velocity = new();
        private byte _simulationRef = 0;
        private byte _federationRef = 0;

        public Builder WithEntityId(EntityId id) { _entityId = id; return this; }
        public Builder WithTargetEntityId(EntityId id) { _targetEntityId = id; return this; }
        public Builder WithMunitionId(EntityId id) { _munitionId = id; return this; }
        public Builder WithEventId(EntityId id) { _eventId = id; return this; }
        public Builder WithFireMissionIndex(uint index) { _fireMissionIndex = index; return this; }
        public Builder WithLocation(double x, double y, double z) { _location = new Vector3Double(x, y, z); return this; }
        public Builder WithVelocity(double x, double y, double z) { _velocity = new Vector3Double(x, y, z); return this; }
        public Builder WithSimulationFederation(byte sim, byte fed) { _simulationRef = sim; _federationRef = fed; return this; }

        public MunitionPdu Build() => new(
            _entityId,
            _targetEntityId,
            _munitionId,
            _eventId,
            _fireMissionIndex,
            _location,
            _velocity,
            _simulationRef,
            _federationRef
        );
    }
}
