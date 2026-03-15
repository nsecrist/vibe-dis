using System;
using System.Buffers.Binary;
using SisoDis.Core.Common;
using SisoDis.Core.Serialization;

namespace SisoDis.Core.Pdu;

/// <summary>
/// Represents DIS Service Request PDU (IEEE 1278.1-2012 §5.3.8.1).
/// </summary>
/// <remarks>
/// Used by entities to request logistic services (resupply, repair, etc.) from other entities.
/// Contains requesting entity ID, supply type, quantity, request ID, and service type requested.
/// </remarks>
public record struct ServiceRequestPdu(
    EntityId RequestingEntityId,
    ushort SupplyType,
    uint Quantity,
    uint RequestId,
    byte ServiceTypeRequested,
    ushort NumberOfFixedDatum,
    ushort NumberOfVariableDatum,
    byte SimulationReference,
    byte FederationReference
) : IPdu
{
    /// <summary>PDU Type code for Service Request PDU per IEEE 1278.1-2012 Table 5-4.</summary>
    public const ushort PdTypeValue = 40;

    public byte Magic => 1;
    public byte ProtocolVersion => 3;
    public ushort PdType => PdTypeValue;

    /// <summary>Total computed length including header.</summary>
    public int ComputedLength() => PduHeader.HeaderLength + 19;

    public void SerializeBody(Span<byte> buffer, int offset)
    {
        // Requesting Entity ID (2 bytes) - IEEE §5.3.8.1.2
        BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, 2), (ushort)RequestingEntityId.Value);
        offset += 2;

        // Supply Type (2 bytes) - IEEE §5.3.8.1.3
        BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, 2), SupplyType);
        offset += 2;

        // Quantity (4 bytes) - IEEE §5.3.8.1.4
        BinaryPrimitives.WriteUInt32BigEndian(buffer.Slice(offset, 4), Quantity);
        offset += 4;

        // Request ID (4 bytes) - IEEE §5.3.8.1.5
        BinaryPrimitives.WriteUInt32BigEndian(buffer.Slice(offset, 4), RequestId);
        offset += 4;

        // Service Type Requested (1 byte) - IEEE §5.3.8.1.6
        buffer[offset] = ServiceTypeRequested;
        offset++;

        // Number of Fixed Datum (2 bytes) - IEEE §5.3.8.1.7
        BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, 2), NumberOfFixedDatum);
        offset += 2;

        // Number of Variable Datum (2 bytes) - IEEE §5.3.8.1.8
        BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, 2), NumberOfVariableDatum);
        offset += 2;

        // Simulation Reference (1 byte) - IEEE §5.3.8.1.9
        buffer[offset] = SimulationReference;
        offset++;

        // Federation Reference (1 byte) - IEEE §5.3.8.1.10
        buffer[offset] = FederationReference;
    }

    public static ServiceRequestPdu Deserialize(ReadOnlySpan<byte> buffer, int offset = 0)
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

        // Requesting Entity ID (2 bytes)
        ushort requestingEntityIdValue = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(pos, 2));
        EntityId requestingEntityId = new EntityId(requestingEntityIdValue);
        pos += 2;

        // Supply Type (2 bytes)
        ushort supplyType = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(pos, 2));
        pos += 2;

        // Quantity (4 bytes)
        uint quantity = BinaryPrimitives.ReadUInt32BigEndian(buffer.Slice(pos, 4));
        pos += 4;

        // Request ID (4 bytes)
        uint requestId = BinaryPrimitives.ReadUInt32BigEndian(buffer.Slice(pos, 4));
        pos += 4;

        // Service Type Requested (1 byte)
        byte serviceTypeRequested = buffer[pos];
        pos++;

        // Number of Fixed Datum (2 bytes)
        ushort numberOfFixedDatum = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(pos, 2));
        pos += 2;

        // Number of Variable Datum (2 bytes)
        ushort numberOfVariableDatum = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(pos, 2));
        pos += 2;

        // Simulation Reference (1 byte)
        byte simulationRef = buffer[pos];
        pos++;

        // Federation Reference (1 byte)
        byte federationRef = buffer[pos];

        return new ServiceRequestPdu(
            requestingEntityId,
            supplyType,
            quantity,
            requestId,
            serviceTypeRequested,
            numberOfFixedDatum,
            numberOfVariableDatum,
            simulationRef,
            federationRef
        );
    }

    public static Builder Create() => new();

    public class Builder
    {
        private EntityId _requestingEntityId = new(0);
        private ushort _supplyType = 0;
        private uint _quantity = 0;
        private uint _requestId = 0;
        private byte _serviceTypeRequested = 0;
        private ushort _numberOfFixedDatum = 0;
        private ushort _numberOfVariableDatum = 0;
        private byte _simulationRef = 0;
        private byte _federationRef = 0;

        public Builder WithRequestingEntityId(EntityId id) { _requestingEntityId = id; return this; }
        public Builder WithSupplyType(ushort type) { _supplyType = type; return this; }
        public Builder WithQuantity(uint qty) { _quantity = qty; return this; }
        public Builder WithRequestId(uint id) { _requestId = id; return this; }
        public Builder WithServiceTypeRequested(byte type) { _serviceTypeRequested = type; return this; }
        public Builder WithNumberOfFixedDatum(ushort count) { _numberOfFixedDatum = count; return this; }
        public Builder WithNumberOfVariableDatum(ushort count) { _numberOfVariableDatum = count; return this; }
        public Builder WithSimulationFederation(byte sim, byte fed) { _simulationRef = sim; _federationRef = fed; return this; }

        public ServiceRequestPdu Build() => new(
            _requestingEntityId,
            _supplyType,
            _quantity,
            _requestId,
            _serviceTypeRequested,
            _numberOfFixedDatum,
            _numberOfVariableDatum,
            _simulationRef,
            _federationRef
        );
    }
}