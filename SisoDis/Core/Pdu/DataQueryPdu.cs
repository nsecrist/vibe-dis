using System;
using System.Buffers.Binary;
using SisoDis.Core.Common;
using SisoDis.Core.Serialization;

namespace SisoDis.Core.Pdu;

/// <summary>
/// Represents DIS Data Query PDU (IEEE 1278.1-2012 §5.3.6.8).
/// </summary>
public record struct DataQueryPdu(
    uint RequestId,
    ushort TimeInterval,
    ushort NumberOfFixedDatum,
    ushort NumberOfVariableDatum,
    byte SimulationReference,
    byte FederationReference
) : IPdu
{
    public const ushort PdTypeValue = 18;

    public byte Magic => 1;
    public byte ProtocolVersion => 3;
    public ushort PdType => PdTypeValue;

    public int ComputedLength() => PduHeader.HeaderLength + 14;

    public void SerializeBody(Span<byte> buffer, int offset)
    {
        BinaryPrimitives.WriteUInt32BigEndian(buffer.Slice(offset, 4), RequestId);
        offset += 4;

        BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, 2), TimeInterval);
        offset += 2;

        BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, 2), NumberOfFixedDatum);
        offset += 2;

        BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, 2), NumberOfVariableDatum);
        offset += 2;

        buffer[offset] = SimulationReference;
        offset++;

        buffer[offset] = FederationReference;
    }

    public static DataQueryPdu Deserialize(ReadOnlySpan<byte> buffer, int offset = 0)
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

        ushort timeInterval = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(pos, 2));
        pos += 2;

        ushort numberOfFixedDatum = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(pos, 2));
        pos += 2;

        ushort numberOfVariableDatum = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(pos, 2));
        pos += 2;

        byte simulationRef = buffer[pos];
        pos++;

        byte federationRef = buffer[pos];

        return new DataQueryPdu(
            requestId,
            timeInterval,
            numberOfFixedDatum,
            numberOfVariableDatum,
            simulationRef,
            federationRef
        );
    }

    public static Builder Create() => new();

    public class Builder
    {
        private uint _requestId = 0;
        private ushort _timeInterval = 0;
        private ushort _numberOfFixedDatum = 0;
        private ushort _numberOfVariableDatum = 0;
        private byte _simulationRef = 0;
        private byte _federationRef = 0;

        public Builder WithRequestId(uint id) { _requestId = id; return this; }
        public Builder WithTimeInterval(ushort interval) { _timeInterval = interval; return this; }
        public Builder WithNumberOfFixedDatum(ushort count) { _numberOfFixedDatum = count; return this; }
        public Builder WithNumberOfVariableDatum(ushort count) { _numberOfVariableDatum = count; return this; }
        public Builder WithSimulationFederation(byte sim, byte fed) { _simulationRef = sim; _federationRef = fed; return this; }

        public DataQueryPdu Build() => new(
            _requestId,
            _timeInterval,
            _numberOfFixedDatum,
            _numberOfVariableDatum,
            _simulationRef,
            _federationRef
        );
    }
}
