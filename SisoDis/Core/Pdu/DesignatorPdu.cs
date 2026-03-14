using System;
using System.Buffers.Binary;
using SisoDis.Core.Common;
using SisoDis.Core.Serialization;

namespace SisoDis.Core.Pdu;

/// <summary>
/// Represents DIS Designator PDU (IEEE 1278.1-2012 §5.3.11).
/// Used to communicate the designation of an entity or location by a designator.
/// </summary>
public record struct DesignatorPdu(
    EntityId EntityId,
    EntityId TargetEntityId,
    Vector3Double DesignatorLocation,
    Vector3Double DesignatorOrientation,
    Vector3Double EntityLocation,
    byte DesignatorCode,
    byte DesignatorOutput,
    byte SimulationReference,
    byte FederationReference
) : IPdu
{
    /// <summary>PDU Type code for Designator PDU per IEEE 1278.1-2012 Table 5-4.</summary>
    public const ushort PdTypeValue = 21;

    public byte Magic => 1;
    public byte ProtocolVersion => 3;
    public ushort PdType => PdTypeValue;

    public int ComputedLength() => PduHeader.HeaderLength + 80;

    public void SerializeBody(Span<byte> buffer, int offset)
    {
        BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, 2), (ushort)EntityId.Value);
        offset += 2;

        BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, 2), (ushort)TargetEntityId.Value);
        offset += 2;

        SerializeDouble(buffer.Slice(offset), DesignatorLocation.X);
        SerializeDouble(buffer.Slice(offset + 8), DesignatorLocation.Y);
        SerializeDouble(buffer.Slice(offset + 16), DesignatorLocation.Z);
        offset += 24;

        SerializeDouble(buffer.Slice(offset), DesignatorOrientation.X);
        SerializeDouble(buffer.Slice(offset + 8), DesignatorOrientation.Y);
        SerializeDouble(buffer.Slice(offset + 16), DesignatorOrientation.Z);
        offset += 24;

        SerializeDouble(buffer.Slice(offset), EntityLocation.X);
        SerializeDouble(buffer.Slice(offset + 8), EntityLocation.Y);
        SerializeDouble(buffer.Slice(offset + 16), EntityLocation.Z);
        offset += 24;

        buffer[offset] = DesignatorCode;
        offset++;

        buffer[offset] = DesignatorOutput;
        offset++;

        buffer[offset] = SimulationReference;
        offset++;

        buffer[offset] = FederationReference;
    }

    public static DesignatorPdu Deserialize(ReadOnlySpan<byte> buffer, int offset = 0)
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

        ushort targetIdValue = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(pos, 2));
        EntityId targetId = new EntityId(targetIdValue);
        pos += 2;

        Vector3Double designatorLocation = new Vector3Double(
            ReadDouble(buffer.Slice(pos)),
            ReadDouble(buffer.Slice(pos + 8)),
            ReadDouble(buffer.Slice(pos + 16))
        );
        pos += 24;

        Vector3Double designatorOrientation = new Vector3Double(
            ReadDouble(buffer.Slice(pos)),
            ReadDouble(buffer.Slice(pos + 8)),
            ReadDouble(buffer.Slice(pos + 16))
        );
        pos += 24;

        Vector3Double entityLocation = new Vector3Double(
            ReadDouble(buffer.Slice(pos)),
            ReadDouble(buffer.Slice(pos + 8)),
            ReadDouble(buffer.Slice(pos + 16))
        );
        pos += 24;

        byte designatorCode = buffer[pos];
        pos++;

        byte designatorOutput = buffer[pos];
        pos++;

        byte simulationRef = buffer[pos];
        pos++;

        byte federationRef = buffer[pos];

        return new DesignatorPdu(
            entityId,
            targetId,
            designatorLocation,
            designatorOrientation,
            entityLocation,
            designatorCode,
            designatorOutput,
            simulationRef,
            federationRef
        );
    }

    public static Builder Create() => new();

    private static void SerializeDouble(Span<byte> buffer, double value)
        => BinaryPrimitives.WriteDoubleBigEndian(buffer, value);

    private static double ReadDouble(ReadOnlySpan<byte> buffer)
        => BinaryPrimitives.ReadDoubleBigEndian(buffer.Slice(0, 8));

    public class Builder
    {
        private EntityId _entityId = new(0);
        private EntityId _targetEntityId = new(0);
        private Vector3Double _designatorLocation = new();
        private Vector3Double _designatorOrientation = new();
        private Vector3Double _entityLocation = new();
        private byte _designatorCode = 0;
        private byte _designatorOutput = 0;
        private byte _simulationRef = 0;
        private byte _federationRef = 0;

        public Builder WithEntityId(EntityId id) { _entityId = id; return this; }
        public Builder WithTargetEntityId(EntityId id) { _targetEntityId = id; return this; }
        public Builder WithDesignatorLocation(double x, double y, double z) { _designatorLocation = new Vector3Double(x, y, z); return this; }
        public Builder WithDesignatorOrientation(double x, double y, double z) { _designatorOrientation = new Vector3Double(x, y, z); return this; }
        public Builder WithEntityLocation(double x, double y, double z) { _entityLocation = new Vector3Double(x, y, z); return this; }
        public Builder WithDesignatorCode(byte code) { _designatorCode = code; return this; }
        public Builder WithDesignatorOutput(byte output) { _designatorOutput = output; return this; }
        public Builder WithSimulationFederation(byte sim, byte fed) { _simulationRef = sim; _federationRef = fed; return this; }

        public DesignatorPdu Build() => new(
            _entityId,
            _targetEntityId,
            _designatorLocation,
            _designatorOrientation,
            _entityLocation,
            _designatorCode,
            _designatorOutput,
            _simulationRef,
            _federationRef
        );
    }
}
