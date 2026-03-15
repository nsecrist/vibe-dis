using System;
using System.Buffers.Binary;
using SisoDis.Core.Common;
using SisoDis.Core.Serialization;

namespace SisoDis.Core.Pdu;

public record struct SignalPdu(
    EntityId EntityId,
    ushort RadioId,
    ushort EncodingScheme,
    ushort EncodingType,
    uint DataLength,
    byte[] Data,
    byte SimulationReference,
    byte FederationReference
) : IPdu
{
    public const ushort PdTypeValue = 26;

    public byte Magic => 1;
    public byte ProtocolVersion => 3;
    public ushort PdType => PdTypeValue;

    public int ComputedLength() => PduHeader.HeaderLength + 16 + (Data?.Length ?? 0);

    public void SerializeBody(Span<byte> buffer, int offset)
    {
        BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, 2), (ushort)EntityId.Value);
        offset += 2;

        BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, 2), RadioId);
        offset += 2;

        BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, 2), EncodingScheme);
        offset += 2;

        BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, 2), EncodingType);
        offset += 2;

        BinaryPrimitives.WriteUInt32BigEndian(buffer.Slice(offset, 4), DataLength);
        offset += 4;

        if (Data != null && Data.Length > 0)
        {
            for (int i = 0; i < Data.Length && offset + i < buffer.Length; i++)
            {
                buffer[offset + i] = Data[i];
            }
            offset += Data.Length;
        }

        buffer[offset] = SimulationReference;
        offset++;

        buffer[offset] = FederationReference;
    }

    public static SignalPdu Deserialize(ReadOnlySpan<byte> buffer, int offset = 0)
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

        ushort entityIdValue = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(pos, 2));
        EntityId entityId = new EntityId(entityIdValue);
        pos += 2;

        ushort radioId = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(pos, 2));
        pos += 2;

        ushort encodingScheme = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(pos, 2));
        pos += 2;

        ushort encodingType = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(pos, 2));
        pos += 2;

        uint dataLength = BinaryPrimitives.ReadUInt32BigEndian(buffer.Slice(pos, 4));
        pos += 4;

        int dataLengthInt = (int)Math.Min(dataLength, (uint)(buffer.Length - pos));
        byte[] data = new byte[dataLengthInt];
        for (int i = 0; i < dataLengthInt; i++)
        {
            data[i] = buffer[pos + i];
        }
        pos += dataLengthInt;

        byte simulationRef = buffer[pos];
        pos++;

        byte federationRef = buffer[pos];

        return new SignalPdu(
            entityId,
            radioId,
            encodingScheme,
            encodingType,
            dataLength,
            data,
            simulationRef,
            federationRef
        );
    }

    public static Builder Create() => new();

    public class Builder
    {
        private EntityId _entityId = new(0);
        private ushort _radioId = 0;
        private ushort _encodingScheme = 0;
        private ushort _encodingType = 0;
        private byte[] _data = Array.Empty<byte>();
        private byte _simulationRef = 0;
        private byte _federationRef = 0;

        public Builder WithEntityId(EntityId id) { _entityId = id; return this; }
        public Builder WithRadioId(ushort id) { _radioId = id; return this; }
        public Builder WithEncodingScheme(ushort scheme) { _encodingScheme = scheme; return this; }
        public Builder WithEncodingType(ushort type) { _encodingType = type; return this; }
        public Builder WithData(byte[] data) { _data = data ?? Array.Empty<byte>(); return this; }
        public Builder WithSimulationFederation(byte sim, byte fed) { _simulationRef = sim; _federationRef = fed; return this; }

        public SignalPdu Build() => new(
            _entityId,
            _radioId,
            _encodingScheme,
            _encodingType,
            (uint)_data.Length,
            _data,
            _simulationRef,
            _federationRef
        );
    }
}