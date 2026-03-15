using System;
using System.Buffers.Binary;
using SisoDis.Core.Common;
using SisoDis.Core.Serialization;

namespace SisoDis.Core.Pdu;

public record struct ReceiverPdu(
    EntityId EntityId,
    ushort RadioId,
    ushort ReceiverState,
    byte Padding,
    Vector3Double AntennaLocation,
    ushort RadioSystem,
    byte SimulationReference,
    byte FederationReference
) : IPdu
{
    public const ushort PdTypeValue = 27;

    public byte Magic => 1;
    public byte ProtocolVersion => 3;
    public ushort PdType => PdTypeValue;

    public int ComputedLength() => PduHeader.HeaderLength + 35;

    public void SerializeBody(Span<byte> buffer, int offset)
    {
        BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, 2), (ushort)EntityId.Value);
        offset += 2;

        BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, 2), RadioId);
        offset += 2;

        BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, 2), ReceiverState);
        offset += 2;

        buffer[offset] = Padding;
        offset++;

        BinaryPrimitives.WriteDoubleBigEndian(buffer.Slice(offset, 8), AntennaLocation.X);
        offset += 8;
        BinaryPrimitives.WriteDoubleBigEndian(buffer.Slice(offset, 8), AntennaLocation.Y);
        offset += 8;
        BinaryPrimitives.WriteDoubleBigEndian(buffer.Slice(offset, 8), AntennaLocation.Z);
        offset += 8;

        BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, 2), RadioSystem);
        offset += 2;

        buffer[offset] = SimulationReference;
        offset++;

        buffer[offset] = FederationReference;
    }

    public static ReceiverPdu Deserialize(ReadOnlySpan<byte> buffer, int offset = 0)
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

        ushort receiverState = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(pos, 2));
        pos += 2;

        byte padding = buffer[pos];
        pos++;

        Vector3Double antennaLocation = new Vector3Double(
            BinaryPrimitives.ReadDoubleBigEndian(buffer.Slice(pos, 8)),
            BinaryPrimitives.ReadDoubleBigEndian(buffer.Slice(pos + 8, 8)),
            BinaryPrimitives.ReadDoubleBigEndian(buffer.Slice(pos + 16, 8))
        );
        pos += 24;

        ushort radioSystem = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(pos, 2));
        pos += 2;

        byte simulationRef = buffer[pos];
        pos++;

        byte federationRef = buffer[pos];

        return new ReceiverPdu(
            entityId,
            radioId,
            receiverState,
            padding,
            antennaLocation,
            radioSystem,
            simulationRef,
            federationRef
        );
    }

    public static Builder Create() => new();

    public class Builder
    {
        private EntityId _entityId = new(0);
        private ushort _radioId = 0;
        private ushort _receiverState = 0;
        private byte _padding = 0;
        private Vector3Double _antennaLocation = new();
        private ushort _radioSystem = 0;
        private byte _simulationRef = 0;
        private byte _federationRef = 0;

        public Builder WithEntityId(EntityId id) { _entityId = id; return this; }
        public Builder WithRadioId(ushort id) { _radioId = id; return this; }
        public Builder WithReceiverState(ushort state) { _receiverState = state; return this; }
        public Builder WithPadding(byte pad) { _padding = pad; return this; }
        public Builder WithAntennaLocation(Vector3Double loc) { _antennaLocation = loc; return this; }
        public Builder WithRadioSystem(ushort system) { _radioSystem = system; return this; }
        public Builder WithSimulationFederation(byte sim, byte fed) { _simulationRef = sim; _federationRef = fed; return this; }

        public ReceiverPdu Build() => new(
            _entityId,
            _radioId,
            _receiverState,
            _padding,
            _antennaLocation,
            _radioSystem,
            _simulationRef,
            _federationRef
        );
    }
}