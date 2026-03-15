using System;
using System.Buffers.Binary;
using SisoDis.Core.Common;
using SisoDis.Core.Serialization;

namespace SisoDis.Core.Pdu;

/// <summary>
/// Represents DIS Attribute PDU (IEEE 1278.1-2012 §5.3.7).
/// </summary>
/// <remarks>
/// The Attribute PDU is used to transmit entity attribute information that doesn't fit in other PDUs.
/// Contains entity identification, attribute numbers, and variable-length attribute data.
/// 
/// IEEE 1278.1-2012 §5.3.7: Attribute PDU format includes:
/// - Entity ID (2 bytes)
/// - Simulation Reference (1 byte)
/// - Federation Reference (1 byte)
/// - Number of Attributes (1 byte)
/// - Variable-length attribute data (variable): each attribute contains:
///   - Attribute Number (2 bytes)
///   - Attribute Data Length (2 bytes)
///   - Attribute Data (N bytes)
/// </remarks>
public record struct AttributePdu(
    EntityId EntityId,
    byte SimulationReference,
    byte FederationReference,
    Core.Pdu.AttributePdu.AttributeData[] Attributes) : IPdu
{
    /// <summary>Data container for an attribute in the Attribute PDU.</summary>
    public readonly record struct AttributeData(ushort AttributeNumber, byte[] Data);

    /// <summary>PDU Type code for Attribute PDU per IEEE 1278.1-2012 Table 5-4.</summary>
    public const ushort PdTypeValue = 19;

    /// <inheritdoc/>
    public byte Magic => 1;

    /// <inheritdoc/>
    public byte ProtocolVersion => 3;

    /// <inheritdoc/>
    public ushort PdType => PdTypeValue;

    /// <summary>Total computed length including header and all body fields.</summary>
    public int ComputedLength()
    {
        if (Attributes == null || Attributes.Length == 0) 
            return PduHeader.HeaderLength + 5; // Entity ID (2) + SimulationRef (1) + FederationRef (1) + NumAttrs (1)

        int length = PduHeader.HeaderLength + 5;
        foreach (var attr in Attributes)
        {
            if (attr != null)
                length += 4 + attr.Data.Length; // AttributeNumber (2) + DataLength (2) + Data (N)
        }

        return length;
    }

    /// <inheritdoc/>
    public void SerializeBody(Span<byte> buffer, int offset)
    {
        // Entity ID (2 bytes) - IEEE §5.3.7.1
        BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, 2), (ushort)EntityId.Value);
        offset += 2;

        // Simulation Reference (1 byte) - IEEE §5.3.7.2
        buffer[offset] = SimulationReference;
        offset++;

        // Federation Reference (1 byte) - IEEE §5.3.7.3
        buffer[offset] = FederationReference;
        offset++;

        // Number of Attributes (1 byte) - IEEE §5.3.7.4
        ushort numAttributes = (ushort)(Attributes?.Length ?? 0);
        buffer[offset] = (byte)numAttributes;
        offset++;

        if (Attributes == null || Attributes.Length == 0) 
            return;

        // Variable-length attribute data - IEEE §5.3.7.5
        foreach (var attr in Attributes)
        {
            if (attr != null && attr.Data != null && attr.Data.Length > 0)
            {
                BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, 2), attr.AttributeNumber);
                offset += 2;
                
                BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, 2), (ushort)attr.Data.Length);
                offset += 2;

                attr.Data.AsSpan().CopyTo(buffer.Slice(offset, attr.Data.Length));
                offset += attr.Data.Length;
            }
        }
    }

    /// <inheritdoc/>
    public static AttributePdu Deserialize(ReadOnlySpan<byte> buffer, int offset = 0)
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

        // Entity ID (2 bytes) - IEEE §5.3.7.1
        ushort entityIdValue = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(pos, 2));
        EntityId entityId = new EntityId(entityIdValue);
        pos += 2;

        // Simulation Reference (1 byte) - IEEE §5.3.7.2
        byte simulationRef = buffer[pos];
        pos++;

        // Federation Reference (1 byte) - IEEE §5.3.7.3
        byte federationRef = buffer[pos];
        pos++;

        // Number of Attributes (1 byte) - IEEE §5.3.7.4
        byte numAttributes = buffer[pos];
        pos++;

        // Parse variable-length attribute data
        var attributesList = new List<Core.Pdu.AttributePdu.AttributeData>();

        for (int i = 0; i < numAttributes && pos + 4 <= buffer.Length; i++)
        {
            ushort attrNumber = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(pos, 2));
            pos += 2;

            if (pos + 2 > buffer.Length) 
                break;

            ushort dataLength = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(pos, 2));
            pos += 2;

            if (pos + dataLength > buffer.Length || dataLength == 0) 
                continue;

            byte[] attributeData = new byte[dataLength];
            for (int j = 0; j < dataLength && (pos + j) < buffer.Length; j++)
            {
                attributeData[j] = buffer[pos + j];
            }
            pos += dataLength;

            attributesList.Add(new Core.Pdu.AttributePdu.AttributeData(attrNumber, attributeData));
        }

        var attributes = attributesList.ToArray();

        return new AttributePdu(
            entityId,
            simulationRef,
            federationRef,
            attributes
        );
    }

    /// <summary>Creates a new builder for constructing AttributePdu instances.</summary>
    public static Builder Create() => new();

    /// <summary>Builder for creating AttributePdu instances with fluent API.</summary>
    public class Builder
    {
        private EntityId _entityId = default!;
        private byte _simulationRef = 0;
        private byte _federationRef = 0;
        private readonly List<Core.Pdu.AttributePdu.AttributeData> _attributes = new();

        public Builder WithEntityId(EntityId id)
        {
            _entityId = id;
            return this;
        }

        public Builder WithSimulationFederation(byte simulationRef, byte federationRef)
        {
            _simulationRef = simulationRef;
            _federationRef = federationRef;
            return this;
        }

        public Builder WithAttribute(ushort attributeNumber, byte[] data)
        {
            if (data == null || data.Length == 0) 
                throw new ArgumentException("Attribute data cannot be empty", nameof(data));
            
            _attributes.Add(new Core.Pdu.AttributePdu.AttributeData(attributeNumber, data));
            return this;
        }

        public Builder WithAttributes(Core.Pdu.AttributePdu.AttributeData[] attributes)
        {
            if (attributes == null) 
                throw new ArgumentNullException(nameof(attributes));
            
            foreach (var attr in attributes)
            {
                if (attr != null && attr.Data != null && attr.Data.Length > 0)
                    _attributes.Add(attr);
            }

            return this;
        }

        public AttributePdu Build() => new(
            _entityId,
            _simulationRef,
            _federationRef,
            _attributes.ToArray()
        );
    }
}
