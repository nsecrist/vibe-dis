using System;
using System.Buffers.Binary;
using SisoDis.Core.Common;
using SisoDis.Core.Serialization;

namespace SisoDis.Core.Pdu;

/// <summary>
/// Represents entity state information per DIS Entity State PDU (IEEE 1278.1-2012 §5.3.3.1).
/// </summary>
/// <remarks>
/// The Entity State PDU transmits the state of an entity in a distributed simulation.
/// Contains entity identification, type, linear and angular state information,
/// and optional additional state data including articulated parts.
/// 
/// IEEE 1278.1-2012 §5.3.3.1: Entity State PDU format includes:
/// - Entity ID (2 bytes)
/// - Entity Type (2 bytes)  
/// - Linear Position (24 bytes: 3 doubles)
/// - Angular Orientation (24 bytes: 3 doubles)
/// - Linear Velocity (24 bytes: 3 doubles)
/// - Angular Velocity (24 bytes: 3 doubles)
/// - Simulation Reference (1 byte)
/// - Federation Reference (1 byte)
/// - Additional State Data (variable + articulated parts)
/// </remarks>
public record struct EntityStatePdu(
    EntityId EntityId,
    EntityType EntityType,
    Vector3Double LinearPosition,
    Vector3Double AngularOrientation,
    Vector3Double LinearVelocity,
    Vector3Double AngularVelocity,
    byte SimulationReference,
    byte FederationReference,
    EntityStatePduAdditionalState AdditionalState,
    byte NumberOfParts) : IPdu
{
    /// <summary>PDU Type code for Entity State PDU per IEEE 1278.1-2012 Table 5-4.</summary>
    public const ushort PdTypeValue = 1;

    /// <inheritdoc/>
    public byte Magic => 1;

    /// <inheritdoc/>
    public byte ProtocolVersion => 3;

    /// <inheritdoc/>
    public ushort PdType => PdTypeValue;

    /// <summary>Total computed length including header and all body fields.</summary>
    public int ComputedLength() => PduHeader.HeaderLength + 110 + (NumberOfParts * 8);

    /// <inheritdoc/>
    public void SerializeBody(Span<byte> buffer, int offset)
    {
        // Entity ID (2 bytes) - IEEE §5.3.3.2
        BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, 2), (ushort)EntityId.Value);
        offset += 2;

        // Entity Type (2 bytes) - IEEE §5.3.3.2
        ushort entityTypeValue = (ushort)EntityType;
        BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, 2), entityTypeValue);
        offset += 2;

        // Linear Position (24 bytes: 3 doubles) - IEEE §5.3.3.3
        SerializeDouble(buffer.Slice(offset), LinearPosition.X);
        SerializeDouble(buffer.Slice(offset + 8), LinearPosition.Y);
        SerializeDouble(buffer.Slice(offset + 16), LinearPosition.Z);
        offset += 24;

        // Angular Orientation (24 bytes: 3 doubles) - IEEE §5.3.3.3
        SerializeDouble(buffer.Slice(offset), AngularOrientation.X);
        SerializeDouble(buffer.Slice(offset + 8), AngularOrientation.Y);
        SerializeDouble(buffer.Slice(offset + 16), AngularOrientation.Z);
        offset += 24;

        // Linear Velocity (24 bytes: 3 doubles) - IEEE §5.3.3.3
        SerializeDouble(buffer.Slice(offset), LinearVelocity.X);
        SerializeDouble(buffer.Slice(offset + 8), LinearVelocity.Y);
        SerializeDouble(buffer.Slice(offset + 16), LinearVelocity.Z);
        offset += 24;

        // Angular Velocity (24 bytes: 3 doubles) - IEEE §5.3.3.3
        SerializeDouble(buffer.Slice(offset), AngularVelocity.X);
        SerializeDouble(buffer.Slice(offset + 8), AngularVelocity.Y);
        SerializeDouble(buffer.Slice(offset + 16), AngularVelocity.Z);
        offset += 24;

        // Simulation Reference (1 byte) - IEEE §5.3.3.5
        buffer[offset] = SimulationReference;
        offset++;

        // Federation Reference (1 byte) - IEEE §5.3.3.5
        buffer[offset] = FederationReference;
        offset++;

        // Additional State and Fixed Data (12 bytes fixed + variable articulated parts)
        // Per IEEE §5.3.3.6: 1 byte flags, 1 byte ammo state, 8 bytes additional state data
        buffer[offset] = AdditionalState.Flags;
        offset++;
        buffer[offset] = AdditionalState.AmmoState;
        offset++;

        // Write Additional State Data (8 bytes) - IEEE §5.3.3.6
        BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, 2), AdditionalState.LaunchIndicator);
        offset += 2;
        BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, 2), AdditionalState.EmitterState);
        offset += 2;
        buffer[offset] = AdditionalState.ArticulationCount;
        offset++;
        buffer[offset] = AdditionalState.ArticulatedPartId;
        offset++;

        // Write Articulated Parts (10 bytes each) - IEEE §5.3.3.4
        int partsToWrite = NumberOfParts;
        for (int i = 0; i < partsToWrite && offset + 8 <= buffer.Length; i++)
        {
            BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, 2), (ushort)i); // Part ID
            offset += 2;
            WriteInt16(buffer.Slice(offset), AdditionalState.ArticulationPositions[i]); // Position
            offset += 2;
            WriteInt16(buffer.Slice(offset), AdditionalState.ArticulationDirections[i]); // Direction
            offset += 2;
            buffer[offset] = AdditionalState.ArticulatedPartStates[i]; // State
            offset++;
            buffer[offset] = AdditionalState.ArticulationOffsets[i]; // Offset (byte)
            offset++;
        }
    }

    /// <inheritdoc/>
    public static EntityStatePdu Deserialize(ReadOnlySpan<byte> buffer, int offset = 0)
    {
        if (buffer.Length < offset + PduHeader.HeaderLength) 
            throw new ArgumentException("Buffer too small for header", nameof(buffer));

        // Validate header magic and version
        byte magic = buffer[offset];
        byte versionMajor = buffer[offset + 1];
        
        if (magic != 1)
            throw new DisValidationException($"Invalid magic: expected 1, got {magic}");
            
        if (versionMajor != 3)
            throw new DisValidationException($"Invalid protocol version: expected 3, got {versionMajor}");

        // Validate PDU type
        ushort actualType = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(offset + 4, 2));
        if (actualType != PdTypeValue)
            throw new DisValidationException($"Invalid PDU type: expected {PdTypeValue}, got {actualType}");

        // Parse body starting after header
        int pos = offset + PduHeader.HeaderLength;

        // Entity ID (2 bytes) - IEEE §5.3.3.2
        ushort entityIdValue = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(pos, 2));
        EntityId entityId = new EntityId(entityIdValue);
        pos += 2;

        // Entity Type (2 bytes) - IEEE §5.3.3.2
        ushort entityTypeValue = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(pos, 2));
        EntityType entityType = (EntityType)(entityTypeValue & 0xFF);
        pos += 2;

        // Linear Position (24 bytes: 3 doubles) - IEEE §5.3.3.3
        Vector3Double linearPosition = new Vector3Double(
            ReadDouble(buffer.Slice(pos)),
            ReadDouble(buffer.Slice(pos + 8)),
            ReadDouble(buffer.Slice(pos + 16))
        );
        pos += 24;

        // Angular Orientation (24 bytes: 3 doubles) - IEEE §5.3.3.3
        Vector3Double angularOrientation = new Vector3Double(
            ReadDouble(buffer.Slice(pos)),
            ReadDouble(buffer.Slice(pos + 8)),
            ReadDouble(buffer.Slice(pos + 16))
        );
        pos += 24;

        // Linear Velocity (24 bytes: 3 doubles) - IEEE §5.3.3.3
        Vector3Double linearVelocity = new Vector3Double(
            ReadDouble(buffer.Slice(pos)),
            ReadDouble(buffer.Slice(pos + 8)),
            ReadDouble(buffer.Slice(pos + 16))
        );
        pos += 24;

        // Angular Velocity (24 bytes: 3 doubles) - IEEE §5.3.3.3
        Vector3Double angularVelocity = new Vector3Double(
            ReadDouble(buffer.Slice(pos)),
            ReadDouble(buffer.Slice(pos + 8)),
            ReadDouble(buffer.Slice(pos + 16))
        );
        pos += 24;

        // Simulation Reference (1 byte) - IEEE §5.3.3.5
        byte simulationRef = buffer[pos];
        pos++;

        // Federation Reference (1 byte) - IEEE §5.3.3.5
        byte federationRef = buffer[pos];
        pos++;

        // Additional State and Fixed Data (12 bytes total for base fields)
        // Per IEEE §5.3.3.6: 1 byte flags, 1 byte ammo state, 8 bytes additional state data
        EntityStatePduAdditionalState additionalState;
        
        byte flags = buffer[pos];
        pos++;
        byte ammoState = buffer[pos];
        pos++;

        ushort launchIndicator = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(pos, 2));
        pos += 2;
        ushort emitterState = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(pos, 2));
        pos += 2;
        byte articulationCount = buffer[pos];
        pos++;
        byte articulatedPartId = buffer[pos];
        pos++;

        // Read Articulated Parts (10 bytes each) - IEEE §5.3.3.4
        int partsToRead = Math.Min((int)articulationCount, 255);
        short[] articulationPositions = new short[partsToRead];
        short[] articulationDirections = new short[partsToRead];
        byte[] articulatedPartStates = new byte[partsToRead];
        byte[] articulationOffsets = new byte[partsToRead];

        for (int i = 0; i < partsToRead && pos + 8 <= buffer.Length; i++)
        {
            ushort partId = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(pos, 2));
            pos += 2;
            
            articulationPositions[i] = ReadInt16(buffer.Slice(pos));
            pos += 2;
            
            articulationDirections[i] = ReadInt16(buffer.Slice(pos));
            pos += 2;
            
            articulatedPartStates[i] = buffer[pos];
            pos++;
            
            articulationOffsets[i] = buffer[pos];
            pos++;
        }

        additionalState = new EntityStatePduAdditionalState(
            flags,
            ammoState,
            launchIndicator,
            emitterState,
            articulationCount,
            articulatedPartId,
            articulationPositions,
            articulationDirections,
            articulatedPartStates,
            articulationOffsets
        );

        return new EntityStatePdu(
            entityId,
            entityType,
            linearPosition,
            angularOrientation,
            linearVelocity,
            angularVelocity,
            simulationRef,
            federationRef,
            additionalState,
            (byte)partsToRead
        );
    }

    /// <summary>Creates a new builder for constructing EntityStatePdu instances.</summary>
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

    /// <summary>Builder for creating EntityStatePdu instances with fluent API.</summary>
    public class Builder
    {
        private EntityId _entityId = default!;
        private EntityType _entityType = EntityType.None;
        private Vector3Double _linearPosition = new();
        private Vector3Double _angularOrientation = new();
        private Vector3Double _linearVelocity = new();
        private Vector3Double _angularVelocity = new();
        private byte _simulationRef = 0;
        private byte _federationRef = 0;
        private EntityStatePduAdditionalState _additionalState = default!;
        private byte _numberOfParts = 0;

        public Builder WithEntityId(EntityId id)
        {
            _entityId = id;
            return this;
        }

        public Builder WithEntityType(EntityType type)
        {
            _entityType = type;
            return this;
        }

        public Builder WithLinearPosition(double x, double y, double z)
        {
            _linearPosition = new Vector3Double(x, y, z);
            return this;
        }

        public Builder WithAngularOrientation(double roll, double pitch, double yaw)
        {
            _angularOrientation = new Vector3Double(roll, pitch, yaw);
            return this;
        }

        public Builder WithLinearVelocity(double x, double y, double z)
        {
            _linearVelocity = new Vector3Double(x, y, z);
            return this;
        }

        public Builder WithAngularVelocity(double x, double y, double z)
        {
            _angularVelocity = new Vector3Double(x, y, z);
            return this;
        }

        public Builder WithSimulationFederation(byte simulationRef, byte federationRef)
        {
            _simulationRef = simulationRef;
            _federationRef = federationRef;
            return this;
        }

        public Builder WithAdditionalState(EntityStatePduAdditionalState state)
        {
            _additionalState = state;
            return this;
        }

        public Builder WithNumberOfParts(byte count)
        {
            _numberOfParts = count;
            return this;
        }

        public EntityStatePdu Build() => new(
            _entityId,
            _entityType,
            _linearPosition,
            _angularOrientation,
            _linearVelocity,
            _angularVelocity,
            _simulationRef,
            _federationRef,
            _additionalState,
            _numberOfParts
        );
    }
}

/// <summary>
/// Additional state information for Entity State PDU (IEEE §5.3.3.6).
/// </summary>
public record struct EntityStatePduAdditionalState(
    byte Flags,
    byte AmmoState,
    ushort LaunchIndicator,
    ushort EmitterState,
    byte ArticulationCount,
    byte ArticulatedPartId,
    short[] ArticulationPositions = null!,
    short[] ArticulationDirections = null!,
    byte[] ArticulatedPartStates = null!,
    byte[] ArticulationOffsets = null!
)
{
    /// <summary>Default constructor for creating additional state.</summary>
    public EntityStatePduAdditionalState() : this(0, 0, 0, 0, 0, 0, Array.Empty<short>(), Array.Empty<short>(), Array.Empty<byte>(), Array.Empty<byte>()) { }

    /// <summary>Kill flag (bit 0): Entity has been killed.</summary>
    public bool IsKilled => (Flags & 0x01) != 0;

    /// <summary>Damaged flag (bit 1): Entity is damaged but functional.</summary>
    public bool IsDamaged => (Flags & 0x02) != 0;

    /// <summary>Supplied ammo indicator (bits 2-5).</summary>
    public byte SuppliedAmmoIndicator => (byte)((Flags >> 2) & 0x0F);

    /// <summary>Received ammo indicator (bits 6-7).</summary>
    public byte ReceivedAmmoIndicator => (byte)((Flags >> 6) & 0x03);
}
