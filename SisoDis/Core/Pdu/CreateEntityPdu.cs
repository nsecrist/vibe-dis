using System;
using System.Buffers.Binary;
using SisoDis.Core.Common;
using SisoDis.Core.Serialization;

namespace SisoDis.Core.Pdu;

/// <summary>
/// Represents DIS Create Entity PDU (IEEE 1278.1-2012 §5.3.6.1).
/// </summary>
public record struct CreateEntityPdu(
    uint RequestId,
    byte NumberOfParts,
    byte PartParameterIndex,
    EntityId EntityId,
    byte SimulationReference,
    byte FederationReference
) : IPdu
{
    public const ushort PdTypeValue = 11;

    public byte Magic => 1;
    public byte ProtocolVersion => 3;
    public ushort PdType => PdTypeValue;

    public int ComputedLength() => PduHeader.HeaderLength + 14;

    public void SerializeBody(Span<byte> buffer, int offset)
    {
        BinaryPrimitives.WriteUInt32BigEndian(buffer.Slice(offset, 4), RequestId);
        offset += 4;

        buffer[offset] = NumberOfParts;
        offset++;

        buffer[offset] = PartParameterIndex;
        offset++;

        BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, 2), (ushort)EntityId.Value);
        offset += 2;

        buffer[offset] = SimulationReference;
        offset++;

        buffer[offset] = FederationReference;
    }

    public static CreateEntityPdu Deserialize(ReadOnlySpan<byte> buffer, int offset = 0)
    {
        if (buffer.Length < offset + PduHeader.HeaderLength)
            throw new ArgumentException("Buffer too small for header", nameof(buffer));

        byte magic = buffer[offset];
        if (magic != 1)
            throw new DisValidationException($"Invalid magic: expected 1, got {magic}");

        byte versionMajor = buffer[offset + 1];
        if (versionMajor != 3)
            throw new DisValidationException($"Invalid protocol version: expected 3, got {versionMajor}");

        ushort actualType = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(offset + 4, 2));
        if (actualType != PdTypeValue)
            throw new DisValidationException($"Invalid PDU type: expected {PdTypeValue}, got {actualType}");

        int pos = offset + PduHeader.HeaderLength;

        uint requestId = BinaryPrimitives.ReadUInt32BigEndian(buffer.Slice(pos, 4));
        pos += 4;

        byte numberOfParts = buffer[pos];
        pos++;

        byte partParameterIndex = buffer[pos];
        pos++;

        ushort entityIdValue = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(pos, 2));
        EntityId entityId = new EntityId(entityIdValue);
        pos += 2;

        byte simulationRef = buffer[pos];
        pos++;

        byte federationRef = buffer[pos];

        return new CreateEntityPdu(
            requestId,
            numberOfParts,
            partParameterIndex,
            entityId,
            simulationRef,
            federationRef
        );
    }

    public static Builder Create() => new();

    public class Builder
    {
        private uint _requestId = 0;
        private byte _numberOfParts = 0;
        private byte _partParameterIndex = 0;
        private EntityId _entityId = new(0);
        private byte _simulationRef = 0;
        private byte _federationRef = 0;

        public Builder WithRequestId(uint id) { _requestId = id; return this; }
        public Builder WithNumberOfParts(byte count) { _numberOfParts = count; return this; }
        public Builder WithPartParameterIndex(byte index) { _partParameterIndex = index; return this; }
        public Builder WithEntityId(EntityId id) { _entityId = id; return this; }
        public Builder WithSimulationFederation(byte sim, byte fed) { _simulationRef = sim; _federationRef = fed; return this; }

        public CreateEntityPdu Build() => new(
            _requestId,
            _numberOfParts,
            _partParameterIndex,
            _entityId,
            _simulationRef,
            _federationRef
        );
    }
}
