using System;
using System.Buffers.Binary;
using SisoDis.Core.Common;
using SisoDis.Core.Serialization;

namespace SisoDis.Core.Pdu;

/// <summary>
/// Represents DIS Acknowledge PDU (IEEE 1278.1-2012 §5.3.6.5).
/// </summary>
public record struct AcknowledgePdu(
    uint RequestId,
    ushort ResponseFlag,
    ushort AcknowledgeFlag,
    byte SimulationReference,
    byte FederationReference
) : IPdu
{
    public const ushort PdTypeValue = 27;

    public byte Magic => 1;
    public byte ProtocolVersion => 3;
    public ushort PdType => PdTypeValue;

    public int ComputedLength() => PduHeader.HeaderLength + 12;

    public void SerializeBody(Span<byte> buffer, int offset)
    {
        BinaryPrimitives.WriteUInt32BigEndian(buffer.Slice(offset, 4), RequestId);
        offset += 4;

        BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, 2), ResponseFlag);
        offset += 2;

        BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, 2), AcknowledgeFlag);
        offset += 2;

        buffer[offset] = SimulationReference;
        offset++;

        buffer[offset] = FederationReference;
    }

    public static AcknowledgePdu Deserialize(ReadOnlySpan<byte> buffer, int offset = 0)
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

        ushort responseFlag = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(pos, 2));
        pos += 2;

        ushort acknowledgeFlag = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(pos, 2));
        pos += 2;

        byte simulationRef = buffer[pos];
        pos++;

        byte federationRef = buffer[pos];

        return new AcknowledgePdu(
            requestId,
            responseFlag,
            acknowledgeFlag,
            simulationRef,
            federationRef
        );
    }

    public static Builder Create() => new();

    public class Builder
    {
        private uint _requestId = 0;
        private ushort _responseFlag = 0;
        private ushort _acknowledgeFlag = 0;
        private byte _simulationRef = 0;
        private byte _federationRef = 0;

        public Builder WithRequestId(uint id) { _requestId = id; return this; }
        public Builder WithResponseFlag(ushort flag) { _responseFlag = flag; return this; }
        public Builder WithAcknowledgeFlag(ushort flag) { _acknowledgeFlag = flag; return this; }
        public Builder WithSimulationFederation(byte sim, byte fed) { _simulationRef = sim; _federationRef = fed; return this; }

        public AcknowledgePdu Build() => new(
            _requestId,
            _responseFlag,
            _acknowledgeFlag,
            _simulationRef,
            _federationRef
        );
    }
}
