using System;
using System.Buffers.Binary;
using SisoDis.Core.Common;
using SisoDis.Core.Serialization;

namespace SisoDis.Core.Pdu;

/// <summary>
/// Represents DIS Entity State Update PDU (IEEE 1278.1-2012 §5.3.6).
/// </summary>
/// <remarks>
/// The Entity State Update PDU is used to update the state of an existing entity without sending all fields.
/// This compact form reduces bandwidth by transmitting only changed fields.
/// Contains selective updates for entity identification, type, position, orientation, velocity, and additional data.
/// 
/// IEEE 1278.1-2012 §5.3.6: Entity State Update PDU format includes:
/// - Entity ID (2 bytes)
/// - Fixed Data Flags (1 byte): bitmap indicating which fields are included
/// - Variable Data (conditional based on flags):
///   - Entity Type (2 bytes, if flag set)
///   - Linear Position (24 bytes, if flag set)
///   - Angular Orientation (24 bytes, if flag set)
///   - Linear Velocity (24 bytes, if flag set)
///   - Angular Velocity (24 bytes, if flag set)
///   - Simulation Reference (1 byte)
///   - Federation Reference (1 byte)
///   - Additional State (variable + articulated parts)
/// </remarks>
public record struct EntityStateUpdatePdu(
    EntityId EntityId,
    EntityStateUpdateFlags Flags,
    EntityType? EntityType,
    Vector3Double? LinearPosition,
    Vector3Double? AngularOrientation,
    Vector3Double? LinearVelocity,
    Vector3Double? AngularVelocity,
    byte SimulationReference,
    byte FederationReference,
    EntityStateUpdatePduAdditionalState AdditionalData) : IPdu
{
    /// <summary>PDU Type code for Entity State Update PDU per IEEE 1278.1-2012 Table 5-4.</summary>
    public const ushort PdTypeValue = 6;

    /// <inheritdoc/>
    public byte Magic => 1;

    /// <inheritdoc/>
    public byte ProtocolVersion => 3;

    /// <inheritdoc/>
    public ushort PdType => PdTypeValue;

    /// <summary>Total computed length including header and all body fields.</summary>
    public int ComputedLength()
    {
        int length = PduHeader.HeaderLength + 13; // Entity ID (2) + Flags (1) + SimulationRef (1) + FederationRef (1) + Additional base (8)
        
        if ((Flags & EntityStateUpdateFlags.EntityType) != 0) length += 2;
        if ((Flags & EntityStateUpdateFlags.LinearPosition) != 0) length += 24;
        if ((Flags & EntityStateUpdateFlags.AngularOrientation) != 0) length += 24;
        if ((Flags & EntityStateUpdateFlags.LinearVelocity) != 0) length += 24;
        if ((Flags & EntityStateUpdateFlags.AngularVelocity) != 0) length += 24;
        
        return length + (AdditionalData.NumberOfParts * 8);
    }

    /// <inheritdoc/>
    public void SerializeBody(Span<byte> buffer, int offset)
    {
        // Entity ID (2 bytes) - IEEE §5.3.6.1
        BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, 2), (ushort)EntityId.Value);
        offset += 2;

        // Fixed Data Flags (1 byte) - IEEE §5.3.6.2
        buffer[offset] = (byte)Flags;
        offset++;

        // Conditional fields based on flags
        if ((Flags & EntityStateUpdateFlags.EntityType) != 0 && EntityType.HasValue)
        {
            ushort entityTypeValue = (ushort)EntityType.Value;
            BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, 2), entityTypeValue);
            offset += 2;
        }

        if ((Flags & EntityStateUpdateFlags.LinearPosition) != 0 && LinearPosition.HasValue)
        {
            SerializeDouble(buffer.Slice(offset), LinearPosition.Value.X);
            SerializeDouble(buffer.Slice(offset + 8), LinearPosition.Value.Y);
            SerializeDouble(buffer.Slice(offset + 16), LinearPosition.Value.Z);
            offset += 24;
        }

        if ((Flags & EntityStateUpdateFlags.AngularOrientation) != 0 && AngularOrientation.HasValue)
        {
            SerializeDouble(buffer.Slice(offset), AngularOrientation.Value.X);
            SerializeDouble(buffer.Slice(offset + 8), AngularOrientation.Value.Y);
            SerializeDouble(buffer.Slice(offset + 16), AngularOrientation.Value.Z);
            offset += 24;
        }

        if ((Flags & EntityStateUpdateFlags.LinearVelocity) != 0 && LinearVelocity.HasValue)
        {
            SerializeDouble(buffer.Slice(offset), LinearVelocity.Value.X);
            SerializeDouble(buffer.Slice(offset + 8), LinearVelocity.Value.Y);
            SerializeDouble(buffer.Slice(offset + 16), LinearVelocity.Value.Z);
            offset += 24;
        }

        if ((Flags & EntityStateUpdateFlags.AngularVelocity) != 0 && AngularVelocity.HasValue)
        {
            SerializeDouble(buffer.Slice(offset), AngularVelocity.Value.X);
            SerializeDouble(buffer.Slice(offset + 8), AngularVelocity.Value.Y);
            SerializeDouble(buffer.Slice(offset + 16), AngularVelocity.Value.Z);
            offset += 24;
        }

        // Simulation Reference (1 byte) - IEEE §5.3.6.7
        buffer[offset] = SimulationReference;
        offset++;

        // Federation Reference (1 byte) - IEEE §5.3.6.8
        buffer[offset] = FederationReference;
        offset++;

        // Additional State Data (8 bytes fixed + articulated parts)
        buffer[offset] = AdditionalData.Flags;
        offset++;
        buffer[offset] = AdditionalData.AmmoState;
        offset++;
        
        BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, 2), AdditionalData.LaunchIndicator);
        offset += 2;
        BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, 2), AdditionalData.EmitterState);
        offset += 2;
        buffer[offset] = AdditionalData.ArticulationCount;
        offset++;
        buffer[offset] = AdditionalData.ArticulatedPartId;
        offset++;

        // Articulated Parts (10 bytes each) - IEEE §5.3.6.9
        int partsToWrite = Math.Min((int)AdditionalData.ArticulationCount, AdditionalData.NumberOfParts);
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
    public static EntityStateUpdatePdu Deserialize(ReadOnlySpan<byte> buffer, int offset = 0)
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

        // Entity ID (2 bytes) - IEEE §5.3.6.1
        ushort entityIdValue = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(pos, 2));
        EntityId entityId = new EntityId(entityIdValue);
        pos += 2;

        // Fixed Data Flags (1 byte) - IEEE §5.3.6.2
        EntityStateUpdateFlags flags = (EntityStateUpdateFlags)buffer[pos];
        pos++;

        // Parse conditional fields based on flags
        EntityType? entityType = null;
        Vector3Double? linearPosition = null;
        Vector3Double? angularOrientation = null;
        Vector3Double? linearVelocity = null;
        Vector3Double? angularVelocity = null;

        if ((flags & EntityStateUpdateFlags.EntityType) != 0 && pos + 2 <= buffer.Length)
        {
            ushort entityTypeValue = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(pos, 2));
            entityType = (EntityType)(entityTypeValue & 0xFF);
            pos += 2;
        }

        if ((flags & EntityStateUpdateFlags.LinearPosition) != 0 && pos + 24 <= buffer.Length)
        {
            linearPosition = new Vector3Double(
                ReadDouble(buffer.Slice(pos)),
                ReadDouble(buffer.Slice(pos + 8)),
                ReadDouble(buffer.Slice(pos + 16))
            );
            pos += 24;
        }

        if ((flags & EntityStateUpdateFlags.AngularOrientation) != 0 && pos + 24 <= buffer.Length)
        {
            angularOrientation = new Vector3Double(
                ReadDouble(buffer.Slice(pos)),
                ReadDouble(buffer.Slice(pos + 8)),
                ReadDouble(buffer.Slice(pos + 16))
            );
            pos += 24;
        }

        if ((flags & EntityStateUpdateFlags.LinearVelocity) != 0 && pos + 24 <= buffer.Length)
        {
            linearVelocity = new Vector3Double(
                ReadDouble(buffer.Slice(pos)),
                ReadDouble(buffer.Slice(pos + 8)),
                ReadDouble(buffer.Slice(pos + 16))
            );
            pos += 24;
        }

        if ((flags & EntityStateUpdateFlags.AngularVelocity) != 0 && pos + 24 <= buffer.Length)
        {
            angularVelocity = new Vector3Double(
                ReadDouble(buffer.Slice(pos)),
                ReadDouble(buffer.Slice(pos + 8)),
                ReadDouble(buffer.Slice(pos + 16))
            );
            pos += 24;
        }

        // Simulation Reference (1 byte) - IEEE §5.3.6.7
        byte simulationRef = buffer[pos];
        pos++;

        // Federation Reference (1 byte) - IEEE §5.3.6.8
        byte federationRef = buffer[pos];
        pos++;

        // Additional State Data
        EntityStateUpdatePduAdditionalState additionalData;
        
        if (pos + 12 <= buffer.Length)
        {
            byte addFlags = buffer[pos];
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

            // Read articulated parts (limited by available data)
            int maxParts = Math.Min((int)articulationCount, 100);
            short[] positions = new short[maxParts];
            short[] directions = new short[maxParts];
            byte[] states = new byte[maxParts];
            byte[] offsets = new byte[maxParts];

            for (int i = 0; i < maxParts && pos + 8 <= buffer.Length; i++)
            {
                _ = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(pos, 2)); // Part ID - skip validation
                pos += 2;
                
                positions[i] = ReadInt16(buffer.Slice(pos));
                pos += 2;
                
                directions[i] = ReadInt16(buffer.Slice(pos));
                pos += 2;
                
                states[i] = buffer[pos];
                pos++;
                
                offsets[i] = buffer[pos];
                pos++;
            }

            additionalData = new EntityStateUpdatePduAdditionalState(
                addFlags, ammoState, launchIndicator, emitterState, articulationCount, articulatedPartId, positions, directions, states, offsets
            );
        }
        else
        {
            additionalData = new EntityStateUpdatePduAdditionalState();
        }

        return new EntityStateUpdatePdu(
            entityId,
            flags,
            entityType,
            linearPosition,
            angularOrientation,
            linearVelocity,
            angularVelocity,
            simulationRef,
            federationRef,
            additionalData
        );
    }

    /// <summary>Creates a new builder for constructing EntityStateUpdatePdu instances.</summary>
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

    /// <summary>Builder for creating EntityStateUpdatePdu instances with fluent API.</summary>
    public class Builder
    {
        private EntityId _entityId = default!;
        private EntityStateUpdateFlags _flags = EntityStateUpdateFlags.None;
        private EntityType? _entityType;
        private Vector3Double? _linearPosition;
        private Vector3Double? _angularOrientation;
        private Vector3Double? _linearVelocity;
        private Vector3Double? _angularVelocity;
        private byte _simulationRef = 0;
        private byte _federationRef = 0;
        private EntityStateUpdatePduAdditionalState _additionalData = default!;

        public Builder WithEntityId(EntityId id)
        {
            _entityId = id;
            return this;
        }

        public Builder WithFlags(EntityStateUpdateFlags flags)
        {
            _flags = flags;
            return this;
        }

        public Builder WithEntityType(EntityType type)
        {
            _entityType = type;
            _flags |= EntityStateUpdateFlags.EntityType;
            return this;
        }

        public Builder WithLinearPosition(double x, double y, double z)
        {
            _linearPosition = new Vector3Double(x, y, z);
            _flags |= EntityStateUpdateFlags.LinearPosition;
            return this;
        }

        public Builder WithAngularOrientation(double roll, double pitch, double yaw)
        {
            _angularOrientation = new Vector3Double(roll, pitch, yaw);
            _flags |= EntityStateUpdateFlags.AngularOrientation;
            return this;
        }

        public Builder WithLinearVelocity(double x, double y, double z)
        {
            _linearVelocity = new Vector3Double(x, y, z);
            _flags |= EntityStateUpdateFlags.LinearVelocity;
            return this;
        }

        public Builder WithAngularVelocity(double x, double y, double z)
        {
            _angularVelocity = new Vector3Double(x, y, z);
            _flags |= EntityStateUpdateFlags.AngularVelocity;
            return this;
        }

        public Builder WithSimulationFederation(byte simulationRef, byte federationRef)
        {
            _simulationRef = simulationRef;
            _federationRef = federationRef;
            return this;
        }

        public Builder WithAdditionalData(EntityStateUpdatePduAdditionalState data)
        {
            _additionalData = data;
            return this;
        }

        public EntityStateUpdatePdu Build() => new(
            _entityId,
            _flags,
            _entityType,
            _linearPosition,
            _angularOrientation,
            _linearVelocity,
            _angularVelocity,
            _simulationRef,
            _federationRef,
            _additionalData
        );
    }
}

/// <summary>
/// Flags indicating which fields are present in Entity State Update PDU (IEEE §5.3.6).
/// </summary>
[Flags]
public enum EntityStateUpdateFlags : byte
{
    /// <summary>No optional fields included.</summary>
    None = 0x00,

    /// <summary>Entity Type field is present.</summary>
    EntityType = 0x01,

    /// <summary>Linear Position field is present.</summary>
    LinearPosition = 0x02,

    /// <summary>Angular Orientation field is present.</summary>
    AngularOrientation = 0x04,

    /// <summary>Linear Velocity field is present.</summary>
    LinearVelocity = 0x08,

    /// <summary>Angular Velocity field is present.</summary>
    AngularVelocity = 0x10
}

/// <summary>
/// Additional state data for Entity State Update PDU (IEEE §5.3.6).
/// </summary>
public record struct EntityStateUpdatePduAdditionalState(
    byte Flags,
    byte AmmoState,
    ushort LaunchIndicator,
    ushort EmitterState,
    byte ArticulationCount,
    byte ArticulatedPartId,
    short[] ArticulationPositions = null!,
    short[] ArticulationDirections = null!,
    byte[] ArticulatedPartStates = null!,
    byte[] ArticulationOffsets = null!,
    byte NumberOfParts = 0)
{
    /// <summary>Default constructor for creating empty additional state.</summary>
    public EntityStateUpdatePduAdditionalState() : this(0, 0, 0, 0, 0, 0, Array.Empty<short>(), Array.Empty<short>(), Array.Empty<byte>(), Array.Empty<byte>(), 0) { }

    /// <summary>Kill flag (bit 0): Entity has been killed.</summary>
    public bool IsKilled => (Flags & 0x01) != 0;

    /// <summary>Damaged flag (bit 1): Entity is damaged but functional.</summary>
    public bool IsDamaged => (Flags & 0x02) != 0;

    /// <summary>Supplied ammo indicator (bits 2-5).</summary>
    public byte SuppliedAmmoIndicator => (byte)((Flags >> 2) & 0x0F);

    /// <summary>Received ammo indicator (bits 6-7).</summary>
    public byte ReceivedAmmoIndicator => (byte)((Flags >> 6) & 0x03);
}
