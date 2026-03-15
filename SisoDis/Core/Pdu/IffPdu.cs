using System;
using System.Buffers.Binary;
using SisoDis.Core.Common;
using SisoDis.Core.Serialization;

namespace SisoDis.Core.Pdu;

public readonly record struct IffData(
    ushort Modes,
    ushort SystemId,
    ushort SystemType,
    ushort SystemSubtype,
    uint FFCode,
    ushort Status,
    ushort VariableParameterCount
);

public record struct IffPdu(
    EntityId EntityId,
    ushort EmitterNumber,
    ushort EmitterLocation,
    byte SystemDataCount,
    ushort NumberOfIFFFundamentalParameters,
    IffData[] SystemData,
    byte SimulationReference,
    byte FederationReference
) : IPdu
{
    public const ushort PdTypeValue = 28;

    public byte Magic => 1;
    public byte ProtocolVersion => 3;
    public ushort PdType => PdTypeValue;

    public int ComputedLength() => PduHeader.HeaderLength + 12 + (SystemData?.Length * 16 ?? 0);

    public void SerializeBody(Span<byte> buffer, int offset)
    {
        BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, 2), (ushort)EntityId.Value);
        offset += 2;

        BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, 2), EmitterNumber);
        offset += 2;

        BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, 2), EmitterLocation);
        offset += 2;

        buffer[offset] = SystemDataCount;
        offset++;

        offset++;

        BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, 2), NumberOfIFFFundamentalParameters);
        offset += 2;

        if (SystemData != null)
        {
            foreach (var data in SystemData)
            {
                BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, 2), data.Modes);
                offset += 2;

                BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, 2), data.SystemId);
                offset += 2;

                BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, 2), data.SystemType);
                offset += 2;

                BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, 2), data.SystemSubtype);
                offset += 2;

                BinaryPrimitives.WriteUInt32BigEndian(buffer.Slice(offset, 4), data.FFCode);
                offset += 4;

                BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, 2), data.Status);
                offset += 2;

                BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, 2), data.VariableParameterCount);
                offset += 2;
            }
        }

        buffer[offset] = SimulationReference;
        offset++;

        buffer[offset] = FederationReference;
    }

    public static IffPdu Deserialize(ReadOnlySpan<byte> buffer, int offset = 0)
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

        ushort emitterNumber = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(pos, 2));
        pos += 2;

        ushort emitterLocation = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(pos, 2));
        pos += 2;

        byte systemDataCount = buffer[pos];
        pos++;

        pos++;

        ushort numberOfIFFFundamentalParameters = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(pos, 2));
        pos += 2;

        IffData[] systemData = null;
        int maxSystemData = Math.Min(systemDataCount, (buffer.Length - pos) / 16);
        if (maxSystemData > 0)
        {
            systemData = new IffData[maxSystemData];
            for (int i = 0; i < maxSystemData; i++)
            {
                int remaining = buffer.Length - pos;
                if (remaining < 16) break;
                systemData[i] = new IffData(
                    BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(pos, 2)),
                    BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(pos + 2, 2)),
                    BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(pos + 4, 2)),
                    BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(pos + 6, 2)),
                    remaining >= 12 ? BinaryPrimitives.ReadUInt32BigEndian(buffer.Slice(pos + 8, 4)) : 0,
                    remaining >= 14 ? BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(pos + 12, 2)) : (ushort)0,
                    remaining >= 16 ? BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(pos + 14, 2)) : (ushort)0
                );
                pos += 16;
            }
        }

        byte simulationRef = buffer[pos];
        pos++;

        byte federationRef = buffer[pos];

        return new IffPdu(
            entityId,
            emitterNumber,
            emitterLocation,
            systemDataCount,
            numberOfIFFFundamentalParameters,
            systemData,
            simulationRef,
            federationRef
        );
    }

    public static Builder Create() => new();

    public class Builder
    {
        private EntityId _entityId = new(0);
        private ushort _emitterNumber = 0;
        private ushort _emitterLocation = 0;
        private byte _systemDataCount = 0;
        private ushort _numberOfIFFFundamentalParameters = 0;
        private IffData[] _systemData = Array.Empty<IffData>();
        private byte _simulationRef = 0;
        private byte _federationRef = 0;

        public Builder WithEntityId(EntityId id) { _entityId = id; return this; }
        public Builder WithEmitterNumber(ushort num) { _emitterNumber = num; return this; }
        public Builder WithEmitterLocation(ushort loc) { _emitterLocation = loc; return this; }
        public Builder WithSystemData(IffData[] data) { _systemData = data ?? Array.Empty<IffData>(); _systemDataCount = (byte)_systemData.Length; return this; }
        public Builder WithNumberOfIFFFundamentalParameters(ushort count) { _numberOfIFFFundamentalParameters = count; return this; }
        public Builder WithSimulationFederation(byte sim, byte fed) { _simulationRef = sim; _federationRef = fed; return this; }

        public IffPdu Build() => new(
            _entityId,
            _emitterNumber,
            _emitterLocation,
            _systemDataCount,
            _numberOfIFFFundamentalParameters,
            _systemData,
            _simulationRef,
            _federationRef
        );
    }
}