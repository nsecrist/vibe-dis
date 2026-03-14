using System;
using System.Buffers.Binary;
using SisoDis.Core.Common;
using SisoDis.Core.Serialization;

namespace SisoDis.Core.Pdu;

/// <summary>
/// Represents DIS Collision PDU (IEEE 1278.1-2012 §5.3.4).
/// </summary>
/// <remarks>
/// The Collision PDU is used to announce that a collision has occurred between two entities.
/// Contains information about the colliding entity, the impact location and velocity,
/// and optional additional data for articulated parts or effects.
/// 
/// IEEE 1278.1-2012 §5.3.4: Collision PDU format includes:
/// - Entity ID (2 bytes)
/// - Impact Location (24 bytes: 3 doubles)
/// - Velocity Before Impact (24 bytes: 3 doubles)
/// - Additional Data (variable + articulated parts)
/// </remarks>
public record struct CollisionPdu(
    EntityId EntityId,
    Vector3Double ImpactLocation,
    Vector3Double VelocityBeforeImpact,
    byte SimulationReference,
    byte FederationReference,
    CollisionPduAdditionalState AdditionalData,
    byte NumberOfParts) : IPdu
{
    /// <summary>PDU Type code for Collision PDU per IEEE 1278.1-2012 Table 5-4.</summary>
    public const ushort PdTypeValue = 4;

    /// <inheritdoc/>
    public byte Magic => 1;

    /// <inheritdoc/>
    public byte ProtocolVersion => 3;

    /// <inheritdoc/>
    public ushort PdType => PdTypeValue;

    /// <summary>Total computed length including header and all body fields.</summary>
    public int ComputedLength() => PduHeader.HeaderLength + 52 + (NumberOfParts * 8);

    /// <inheritdoc/>
    public void SerializeBody(Span<byte> buffer, int offset)
    {
        // Entity ID (2 bytes) - IEEE §5.3.4.1
        BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, 2), (ushort)EntityId.Value);
        offset += 2;

        // Impact Location (24 bytes: 3 doubles) - IEEE §5.3.4.2
        SerializeDouble(buffer.Slice(offset), ImpactLocation.X);
        SerializeDouble(buffer.Slice(offset + 8), ImpactLocation.Y);
        SerializeDouble(buffer.Slice(offset + 16), ImpactLocation.Z);
        offset += 24;

        // Velocity Before Impact (24 bytes: 3 doubles) - IEEE §5.3.4.3
        SerializeDouble(buffer.Slice(offset), VelocityBeforeImpact.X);
        SerializeDouble(buffer.Slice(offset + 8), VelocityBeforeImpact.Y);
        SerializeDouble(buffer.Slice(offset + 16), VelocityBeforeImpact.Z);
        offset += 24;

        // Simulation Reference (1 byte) - IEEE §5.3.4.4
        buffer[offset] = SimulationReference;
        offset++;

        // Federation Reference (1 byte) - IEEE §5.3.4.5
        buffer[offset] = FederationReference;
        offset++;

        // Write Additional Data and Articulated Parts
        int partsToWrite = NumberOfParts;
        for (int i = 0; i < partsToWrite && offset + 8 <= buffer.Length; i++)
        {
            BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, 2), (ushort)i); // Part ID
            offset += 2;
            WriteInt16(buffer.Slice(offset), AdditionalData.ArticulationPositions[i]);
            offset += 2;
            WriteInt16(buffer.Slice(offset), AdditionalData.ArticulationDirections[i]);
            offset += 2;
            buffer[offset] = AdditionalData.ArticulatedPartStates[i];
            offset++;
            buffer[offset] = AdditionalData.ArticulationOffsets[i];
            offset++;
        }
    }

    /// <inheritdoc/>
    public static CollisionPdu Deserialize(ReadOnlySpan<byte> buffer, int offset = 0)
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

        // Entity ID (2 bytes) - IEEE §5.3.4.1
        ushort entityIdValue = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(pos, 2));
        EntityId entityId = new EntityId(entityIdValue);
        pos += 2;

        // Impact Location (24 bytes: 3 doubles) - IEEE §5.3.4.2
        Vector3Double impactLocation = new Vector3Double(
            ReadDouble(buffer.Slice(pos)),
            ReadDouble(buffer.Slice(pos + 8)),
            ReadDouble(buffer.Slice(pos + 16))
        );
        pos += 24;

        // Velocity Before Impact (24 bytes: 3 doubles) - IEEE §5.3.4.3
        Vector3Double velocityBeforeImpact = new Vector3Double(
            ReadDouble(buffer.Slice(pos)),
            ReadDouble(buffer.Slice(pos + 8)),
            ReadDouble(buffer.Slice(pos + 16))
        );
        pos += 24;

        // Simulation Reference (1 byte) - IEEE §5.3.4.4
        byte simulationRef = buffer[pos];
        pos++;

        // Federation Reference (1 byte) - IEEE §5.3.4.5
        byte federationRef = buffer[pos];
        pos++;

        // Read Additional Data and Articulated Parts
        int partsToRead = 255; // Max possible parts, will be limited by buffer length
        short[] articulationPositions = new short[partsToRead];
        short[] articulationDirections = new short[partsToRead];
        byte[] articulatedPartStates = new byte[partsToRead];
        byte[] articulationOffsets = new byte[partsToRead];

        int actualPartsCount = 0;
        while (pos + 8 <= buffer.Length)
        {
            ushort partId = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(pos, 2));
            pos += 2;
            
            articulationPositions[actualPartsCount] = ReadInt16(buffer.Slice(pos));
            pos += 2;
            
            articulationDirections[actualPartsCount] = ReadInt16(buffer.Slice(pos));
            pos += 2;
            
            articulatedPartStates[actualPartsCount] = buffer[pos];
            pos++;
            
            articulationOffsets[actualPartsCount] = buffer[pos];
            pos++;

            actualPartsCount++;
        }

        var additionalData = new CollisionPduAdditionalState(
            articulationPositions,
            articulationDirections,
            articulatedPartStates,
            articulationOffsets
        );

        return new CollisionPdu(
            entityId,
            impactLocation,
            velocityBeforeImpact,
            simulationRef,
            federationRef,
            additionalData,
            (byte)actualPartsCount
        );
    }

    /// <summary>Creates a new builder for constructing CollisionPdu instances.</summary>
    public static Builder Create() => new();

    private static void SerializeDouble(Span<byte> buffer, double value)
    {
        BinaryPrimitives.WriteDoubleBigEndian(buffer, value);
    }

    private static void WriteInt16(Span<byte> buffer, short value)
    {
        BinaryPrimitives.WriteInt16BigEndian(buffer, value);
    }

    private static double ReadDouble(ReadOnlySpan<byte> buffer)
    {
        return BinaryPrimitives.ReadDoubleBigEndian(buffer.Slice(0, 8));
    }

    private static short ReadInt16(ReadOnlySpan<byte> buffer)
    {
        return BinaryPrimitives.ReadInt16BigEndian(buffer.Slice(0, 2));
    }

    /// <summary>Builder for creating CollisionPdu instances with fluent API.</summary>
    public class Builder
    {
        private EntityId _entityId = default!;
        private Vector3Double _impactLocation = new();
        private Vector3Double _velocityBeforeImpact = new();
        private byte _simulationRef = 0;
        private byte _federationRef = 0;
        private CollisionPduAdditionalState _additionalData = default!;
        private byte _numberOfParts = 0;

        public Builder WithEntityId(EntityId id)
        {
            _entityId = id;
            return this;
        }

        public Builder WithImpactLocation(double x, double y, double z)
        {
            _impactLocation = new Vector3Double(x, y, z);
            return this;
        }

        public Builder WithVelocityBeforeImpact(double x, double y, double z)
        {
            _velocityBeforeImpact = new Vector3Double(x, y, z);
            return this;
        }

        public Builder WithSimulationFederation(byte simulationRef, byte federationRef)
        {
            _simulationRef = simulationRef;
            _federationRef = federationRef;
            return this;
        }

        public Builder WithAdditionalData(CollisionPduAdditionalState data)
        {
            _additionalData = data;
            return this;
        }

        public Builder WithNumberOfParts(byte count)
        {
            _numberOfParts = count;
            return this;
        }

        public CollisionPdu Build() => new(
            _entityId,
            _impactLocation,
            _velocityBeforeImpact,
            _simulationRef,
            _federationRef,
            _additionalData,
            _numberOfParts
        );
    }
}

/// <summary>
/// Additional state data for Collision PDU (IEEE §5.3.4).
/// </summary>
public record struct CollisionPduAdditionalState(
    short[] ArticulationPositions = null!,
    short[] ArticulationDirections = null!,
    byte[] ArticulatedPartStates = null!,
    byte[] ArticulationOffsets = null!
)
{
    /// <summary>Default constructor for creating empty additional state.</summary>
    public CollisionPduAdditionalState() : this(Array.Empty<short>(), Array.Empty<short>(), Array.Empty<byte>(), Array.Empty<byte>()) { }
}
