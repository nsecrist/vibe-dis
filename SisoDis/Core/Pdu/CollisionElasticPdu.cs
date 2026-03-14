using System;
using System.Buffers.Binary;
using SisoDis.Core.Common;
using SisoDis.Core.Serialization;

namespace SisoDis.Core.Pdu;

/// <summary>
/// Represents DIS Collision-Elastic PDU (IEEE 1278.1-2012 §5.3.5).
/// </summary>
/// <remarks>
/// The Collision-Elastic PDU is used to announce that a collision has occurred between two entities,
/// with the result being an elastic collision where no damage or kill occurs.
/// Contains information about both colliding entities and their velocities before impact.
/// 
/// IEEE 1278.1-2012 §5.3.5: Collision-Elastic PDU format includes:
/// - Entity ID (2 bytes)
/// - Impact Location (24 bytes: 3 doubles)
/// - Velocity Before Impact for Entity A (24 bytes: 3 doubles)
/// - Velocity After Impact for Entity A (24 bytes: 3 doubles)
/// - Velocity Before Impact for Entity B (24 bytes: 3 doubles)
/// - Velocity After Impact for Entity B (24 bytes: 3 doubles)
/// - Additional Data (variable + articulated parts)
/// </remarks>
public record struct CollisionElasticPdu(
    EntityId EntityId,
    Vector3Double ImpactLocation,
    Vector3Double VelocityBeforeImpactA,
    Vector3Double VelocityAfterImpactA,
    Vector3Double VelocityBeforeImpactB,
    Vector3Double VelocityAfterImpactB,
    byte SimulationReference,
    byte FederationReference,
    CollisionElasticPduAdditionalState AdditionalData,
    byte NumberOfParts) : IPdu
{
    /// <summary>PDU Type code for Collision-Elastic PDU per IEEE 1278.1-2012 Table 5-4.</summary>
    public const ushort PdTypeValue = 5;

    /// <inheritdoc/>
    public byte Magic => 1;

    /// <inheritdoc/>
    public byte ProtocolVersion => 3;

    /// <inheritdoc/>
    public ushort PdType => PdTypeValue;

    /// <summary>Total computed length including header and all body fields.</summary>
    public int ComputedLength() => PduHeader.HeaderLength + 124 + (NumberOfParts * 8);

    /// <inheritdoc/>
    public void SerializeBody(Span<byte> buffer, int offset)
    {
        // Entity ID (2 bytes) - IEEE §5.3.5.1
        BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, 2), (ushort)EntityId.Value);
        offset += 2;

        // Impact Location (24 bytes: 3 doubles) - IEEE §5.3.5.2
        SerializeDouble(buffer.Slice(offset), ImpactLocation.X);
        SerializeDouble(buffer.Slice(offset + 8), ImpactLocation.Y);
        SerializeDouble(buffer.Slice(offset + 16), ImpactLocation.Z);
        offset += 24;

        // Velocity Before Impact for Entity A (24 bytes: 3 doubles) - IEEE §5.3.5.3
        SerializeDouble(buffer.Slice(offset), VelocityBeforeImpactA.X);
        SerializeDouble(buffer.Slice(offset + 8), VelocityBeforeImpactA.Y);
        SerializeDouble(buffer.Slice(offset + 16), VelocityBeforeImpactA.Z);
        offset += 24;

        // Velocity After Impact for Entity A (24 bytes: 3 doubles) - IEEE §5.3.5.4
        SerializeDouble(buffer.Slice(offset), VelocityAfterImpactA.X);
        SerializeDouble(buffer.Slice(offset + 8), VelocityAfterImpactA.Y);
        SerializeDouble(buffer.Slice(offset + 16), VelocityAfterImpactA.Z);
        offset += 24;

        // Velocity Before Impact for Entity B (24 bytes: 3 doubles) - IEEE §5.3.5.5
        SerializeDouble(buffer.Slice(offset), VelocityBeforeImpactB.X);
        SerializeDouble(buffer.Slice(offset + 8), VelocityBeforeImpactB.Y);
        SerializeDouble(buffer.Slice(offset + 16), VelocityBeforeImpactB.Z);
        offset += 24;

        // Velocity After Impact for Entity B (24 bytes: 3 doubles) - IEEE §5.3.5.6
        SerializeDouble(buffer.Slice(offset), VelocityAfterImpactB.X);
        SerializeDouble(buffer.Slice(offset + 8), VelocityAfterImpactB.Y);
        SerializeDouble(buffer.Slice(offset + 16), VelocityAfterImpactB.Z);
        offset += 24;

        // Simulation Reference (1 byte) - IEEE §5.3.5.7
        buffer[offset] = SimulationReference;
        offset++;

        // Federation Reference (1 byte) - IEEE §5.3.5.8
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
    public static CollisionElasticPdu Deserialize(ReadOnlySpan<byte> buffer, int offset = 0)
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

        // Entity ID (2 bytes) - IEEE §5.3.5.1
        ushort entityIdValue = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(pos, 2));
        EntityId entityId = new EntityId(entityIdValue);
        pos += 2;

        // Impact Location (24 bytes: 3 doubles) - IEEE §5.3.5.2
        Vector3Double impactLocation = new Vector3Double(
            ReadDouble(buffer.Slice(pos)),
            ReadDouble(buffer.Slice(pos + 8)),
            ReadDouble(buffer.Slice(pos + 16))
        );
        pos += 24;

        // Velocity Before Impact for Entity A (24 bytes: 3 doubles) - IEEE §5.3.5.3
        Vector3Double velocityBeforeImpactA = new Vector3Double(
            ReadDouble(buffer.Slice(pos)),
            ReadDouble(buffer.Slice(pos + 8)),
            ReadDouble(buffer.Slice(pos + 16))
        );
        pos += 24;

        // Velocity After Impact for Entity A (24 bytes: 3 doubles) - IEEE §5.3.5.4
        Vector3Double velocityAfterImpactA = new Vector3Double(
            ReadDouble(buffer.Slice(pos)),
            ReadDouble(buffer.Slice(pos + 8)),
            ReadDouble(buffer.Slice(pos + 16))
        );
        pos += 24;

        // Velocity Before Impact for Entity B (24 bytes: 3 doubles) - IEEE §5.3.5.5
        Vector3Double velocityBeforeImpactB = new Vector3Double(
            ReadDouble(buffer.Slice(pos)),
            ReadDouble(buffer.Slice(pos + 8)),
            ReadDouble(buffer.Slice(pos + 16))
        );
        pos += 24;

        // Velocity After Impact for Entity B (24 bytes: 3 doubles) - IEEE §5.3.5.6
        Vector3Double velocityAfterImpactB = new Vector3Double(
            ReadDouble(buffer.Slice(pos)),
            ReadDouble(buffer.Slice(pos + 8)),
            ReadDouble(buffer.Slice(pos + 16))
        );
        pos += 24;

        // Simulation Reference (1 byte) - IEEE §5.3.5.7
        byte simulationRef = buffer[pos];
        pos++;

        // Federation Reference (1 byte) - IEEE §5.3.5.8
        byte federationRef = buffer[pos];
        pos++;

        // Read Additional Data and Articulated Parts
        int partsToRead = 255;
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

        var additionalData = new CollisionElasticPduAdditionalState(
            articulationPositions,
            articulationDirections,
            articulatedPartStates,
            articulationOffsets
        );

        return new CollisionElasticPdu(
            entityId,
            impactLocation,
            velocityBeforeImpactA,
            velocityAfterImpactA,
            velocityBeforeImpactB,
            velocityAfterImpactB,
            simulationRef,
            federationRef,
            additionalData,
            (byte)actualPartsCount
        );
    }

    /// <summary>Creates a new builder for constructing CollisionElasticPdu instances.</summary>
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

    /// <summary>Builder for creating CollisionElasticPdu instances with fluent API.</summary>
    public class Builder
    {
        private EntityId _entityId = default!;
        private Vector3Double _impactLocation = new();
        private Vector3Double _velocityBeforeImpactA = new();
        private Vector3Double _velocityAfterImpactA = new();
        private Vector3Double _velocityBeforeImpactB = new();
        private Vector3Double _velocityAfterImpactB = new();
        private byte _simulationRef = 0;
        private byte _federationRef = 0;
        private CollisionElasticPduAdditionalState _additionalData = default!;
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

        public Builder WithVelocityBeforeImpactA(double x, double y, double z)
        {
            _velocityBeforeImpactA = new Vector3Double(x, y, z);
            return this;
        }

        public Builder WithVelocityAfterImpactA(double x, double y, double z)
        {
            _velocityAfterImpactA = new Vector3Double(x, y, z);
            return this;
        }

        public Builder WithVelocityBeforeImpactB(double x, double y, double z)
        {
            _velocityBeforeImpactB = new Vector3Double(x, y, z);
            return this;
        }

        public Builder WithVelocityAfterImpactB(double x, double y, double z)
        {
            _velocityAfterImpactB = new Vector3Double(x, y, z);
            return this;
        }

        public Builder WithSimulationFederation(byte simulationRef, byte federationRef)
        {
            _simulationRef = simulationRef;
            _federationRef = federationRef;
            return this;
        }

        public Builder WithAdditionalData(CollisionElasticPduAdditionalState data)
        {
            _additionalData = data;
            return this;
        }

        public Builder WithNumberOfParts(byte count)
        {
            _numberOfParts = count;
            return this;
        }

        public CollisionElasticPdu Build() => new(
            _entityId,
            _impactLocation,
            _velocityBeforeImpactA,
            _velocityAfterImpactA,
            _velocityBeforeImpactB,
            _velocityAfterImpactB,
            _simulationRef,
            _federationRef,
            _additionalData,
            _numberOfParts
        );
    }
}

/// <summary>
/// Additional state data for Collision-Elastic PDU (IEEE §5.3.5).
/// </summary>
public record struct CollisionElasticPduAdditionalState(
    short[] ArticulationPositions = null!,
    short[] ArticulationDirections = null!,
    byte[] ArticulatedPartStates = null!,
    byte[] ArticulationOffsets = null!
)
{
    /// <summary>Default constructor for creating empty additional state.</summary>
    public CollisionElasticPduAdditionalState() : this(Array.Empty<short>(), Array.Empty<short>(), Array.Empty<byte>(), Array.Empty<byte>()) { }
}
