using System;
using System.Buffers.Binary;
using SisoDis.Core.Common;
using SisoDis.Core.Serialization;

namespace SisoDis.Core.Pdu;

/// <summary>
/// Represents DIS Stop/Freeze PDU (IEEE 1278.1-2012 §5.3.6.4).
/// </summary>
public record struct StopFreezePdu(
    uint RequestId,
    uint RealTime,
    uint SimulationTime,
    byte Reason,
    byte Padding,
    byte SimulationReference,
    byte FederationReference
) : IPdu
{
    public const ushort PdTypeValue = 26;

    public byte Magic => 1;
    public byte ProtocolVersion => 3;
    public ushort PdType => PdTypeValue;

    public int ComputedLength() => PduHeader.HeaderLength + 20;

    public void SerializeBody(Span<byte> buffer, int offset)
    {
        BinaryPrimitives.WriteUInt32BigEndian(buffer.Slice(offset, 4), RequestId);
        offset += 4;

        BinaryPrimitives.WriteUInt32BigEndian(buffer.Slice(offset, 4), RealTime);
        offset += 4;

        BinaryPrimitives.WriteUInt32BigEndian(buffer.Slice(offset, 4), SimulationTime);
        offset += 4;

        buffer[offset] = Reason;
        offset++;

        buffer[offset] = Padding;
        offset += 3;

        buffer[offset] = SimulationReference;
        offset++;

        buffer[offset] = FederationReference;
    }

    public static StopFreezePdu Deserialize(ReadOnlySpan<byte> buffer, int offset = 0)
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

        uint realTime = BinaryPrimitives.ReadUInt32BigEndian(buffer.Slice(pos, 4));
        pos += 4;

        uint simulationTime = BinaryPrimitives.ReadUInt32BigEndian(buffer.Slice(pos, 4));
        pos += 4;

        byte reason = buffer[pos];
        pos++;

        byte padding = buffer[pos];
        pos += 3;

        byte simulationRef = buffer[pos];
        pos++;

        byte federationRef = buffer[pos];

        return new StopFreezePdu(
            requestId,
            realTime,
            simulationTime,
            reason,
            padding,
            simulationRef,
            federationRef
        );
    }

    public static Builder Create() => new();

    public class Builder
    {
        private uint _requestId = 0;
        private uint _realTime = 0;
        private uint _simulationTime = 0;
        private byte _reason = 0;
        private byte _padding = 0;
        private byte _simulationRef = 0;
        private byte _federationRef = 0;

        public Builder WithRequestId(uint id) { _requestId = id; return this; }
        public Builder WithRealTime(uint time) { _realTime = time; return this; }
        public Builder WithSimulationTime(uint time) { _simulationTime = time; return this; }
        public Builder WithReason(byte reason) { _reason = reason; return this; }
        public Builder WithSimulationFederation(byte sim, byte fed) { _simulationRef = sim; _federationRef = fed; return this; }

        public StopFreezePdu Build() => new(
            _requestId,
            _realTime,
            _simulationTime,
            _reason,
            _padding,
            _simulationRef,
            _federationRef
        );
    }
}
