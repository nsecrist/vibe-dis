using System;
using System.Buffers.Binary;
using SisoDis.Core.Common;
using SisoDis.Core.Serialization;

namespace SisoDis.Core.Pdu;

/// <summary>
/// Represents DIS Electromagnetic Emission PDU (IEEE 1278.1-2012 §5.3.12).
/// Used to communicate electromagnetic emission data from an entity.
/// </summary>
public record struct ElectromagneticEmissionPdu(
    EntityId EntityId,
    ushort EmitterNumber,
    ushort EmitterLocation,
    byte SimulationReference,
    byte FederationReference,
    byte NumberOfSystems,
    byte NumberOfEmissionSystems
) : IPdu
{
    /// <summary>PDU Type code for Electromagnetic Emission PDU per IEEE 1278.1-2012 Table 5-4.</summary>
    public const ushort PdTypeValue = 23;

    public byte Magic => 1;
    public byte ProtocolVersion => 3;
    public ushort PdType => PdTypeValue;

    public int ComputedLength() => PduHeader.HeaderLength + 10;

    public void SerializeBody(Span<byte> buffer, int offset)
    {
        BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, 2), (ushort)EntityId.Value);
        offset += 2;

        BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, 2), EmitterNumber);
        offset += 2;

        BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, 2), EmitterLocation);
        offset += 2;

        buffer[offset] = SimulationReference;
        offset++;

        buffer[offset] = FederationReference;
        offset++;

        buffer[offset] = NumberOfSystems;
        offset++;

        buffer[offset] = NumberOfEmissionSystems;
    }

    public static ElectromagneticEmissionPdu Deserialize(ReadOnlySpan<byte> buffer, int offset = 0)
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

        ushort emitterNumber = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(pos, 2));
        pos += 2;

        ushort emitterLocation = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(pos, 2));
        pos += 2;

        byte simulationRef = buffer[pos];
        pos++;

        byte federationRef = buffer[pos];
        pos++;

        byte numberOfSystems = buffer[pos];
        pos++;

        byte numberOfEmissionSystems = buffer[pos];

        return new ElectromagneticEmissionPdu(
            entityId,
            emitterNumber,
            emitterLocation,
            simulationRef,
            federationRef,
            numberOfSystems,
            numberOfEmissionSystems
        );
    }

    public static Builder Create() => new();

    public class Builder
    {
        private EntityId _entityId = new(0);
        private ushort _emitterNumber = 0;
        private ushort _emitterLocation = 0;
        private byte _simulationRef = 0;
        private byte _federationRef = 0;
        private byte _numberOfSystems = 0;
        private byte _numberOfEmissionSystems = 0;

        public Builder WithEntityId(EntityId id) { _entityId = id; return this; }
        public Builder WithEmitterNumber(ushort num) { _emitterNumber = num; return this; }
        public Builder WithEmitterLocation(ushort loc) { _emitterLocation = loc; return this; }
        public Builder WithSimulationFederation(byte sim, byte fed) { _simulationRef = sim; _federationRef = fed; return this; }
        public Builder WithNumberOfSystems(byte num) { _numberOfSystems = num; return this; }
        public Builder WithNumberOfEmissionSystems(byte num) { _numberOfEmissionSystems = num; return this; }

        public ElectromagneticEmissionPdu Build() => new(
            _entityId,
            _emitterNumber,
            _emitterLocation,
            _simulationRef,
            _federationRef,
            _numberOfSystems,
            _numberOfEmissionSystems
        );
    }
}
