using System;
using System.Buffers.Binary;
using SisoDis.Core.Common;
using SisoDis.Core.Serialization;

namespace SisoDis.Core.Pdu;

public record struct ResupplyOfferPdu(
    EntityId ReceivingEntityId,
    ushort SupplyType,
    uint Quantity,
    uint RequestId,
    byte NumberOfSupplyTypes,
    ushort NumberOfFixedDatum,
    ushort NumberOfVariableDatum,
    byte SimulationReference,
    byte FederationReference
) : IPdu
{
    public const ushort PdTypeValue = 6;

    public byte Magic => 1;
    public byte ProtocolVersion => 3;
    public ushort PdType => PdTypeValue;

    public int ComputedLength() => PduHeader.HeaderLength + 20;

    public void SerializeBody(Span<byte> buffer, int offset)
    {
        BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, 2), (ushort)ReceivingEntityId.Value);
        offset += 2;

        BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, 2), SupplyType);
        offset += 2;

        BinaryPrimitives.WriteUInt32BigEndian(buffer.Slice(offset, 4), Quantity);
        offset += 4;

        BinaryPrimitives.WriteUInt32BigEndian(buffer.Slice(offset, 4), RequestId);
        offset += 4;

        buffer[offset] = NumberOfSupplyTypes;
        offset++;

        offset++;

        BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, 2), NumberOfFixedDatum);
        offset += 2;

        BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, 2), NumberOfVariableDatum);
        offset += 2;

        buffer[offset] = SimulationReference;
        offset++;

        buffer[offset] = FederationReference;
    }

    public static ResupplyOfferPdu Deserialize(ReadOnlySpan<byte> buffer, int offset = 0)
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

        ushort receivingEntityIdValue = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(pos, 2));
        EntityId receivingEntityId = new EntityId(receivingEntityIdValue);
        pos += 2;

        ushort supplyType = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(pos, 2));
        pos += 2;

        uint quantity = BinaryPrimitives.ReadUInt32BigEndian(buffer.Slice(pos, 4));
        pos += 4;

        uint requestId = BinaryPrimitives.ReadUInt32BigEndian(buffer.Slice(pos, 4));
        pos += 4;

        byte numberOfSupplyTypes = buffer[pos];
        pos++;

        pos++;

        ushort numberOfFixedDatum = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(pos, 2));
        pos += 2;

        ushort numberOfVariableDatum = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(pos, 2));
        pos += 2;

        byte simulationRef = buffer[pos];
        pos++;

        byte federationRef = buffer[pos];

        return new ResupplyOfferPdu(
            receivingEntityId,
            supplyType,
            quantity,
            requestId,
            numberOfSupplyTypes,
            numberOfFixedDatum,
            numberOfVariableDatum,
            simulationRef,
            federationRef
        );
    }

    public static Builder Create() => new();

    public class Builder
    {
        private EntityId _receivingEntityId = new(0);
        private ushort _supplyType = 0;
        private uint _quantity = 0;
        private uint _requestId = 0;
        private byte _numberOfSupplyTypes = 0;
        private ushort _numberOfFixedDatum = 0;
        private ushort _numberOfVariableDatum = 0;
        private byte _simulationRef = 0;
        private byte _federationRef = 0;

        public Builder WithReceivingEntityId(EntityId id) { _receivingEntityId = id; return this; }
        public Builder WithSupplyType(ushort type) { _supplyType = type; return this; }
        public Builder WithQuantity(uint qty) { _quantity = qty; return this; }
        public Builder WithRequestId(uint id) { _requestId = id; return this; }
        public Builder WithNumberOfSupplyTypes(byte count) { _numberOfSupplyTypes = count; return this; }
        public Builder WithNumberOfFixedDatum(ushort count) { _numberOfFixedDatum = count; return this; }
        public Builder WithNumberOfVariableDatum(ushort count) { _numberOfVariableDatum = count; return this; }
        public Builder WithSimulationFederation(byte sim, byte fed) { _simulationRef = sim; _federationRef = fed; return this; }

        public ResupplyOfferPdu Build() => new(
            _receivingEntityId,
            _supplyType,
            _quantity,
            _requestId,
            _numberOfSupplyTypes,
            _numberOfFixedDatum,
            _numberOfVariableDatum,
            _simulationRef,
            _federationRef
        );
    }
}