using System.Buffers.Binary;
using SisoDis.Core.Common;
using SisoDis.Core.Serialization;

namespace SisoDis.Core.Pdu;

/// <summary>
/// Abstract base class for all DIS PDUs providing common serialization infrastructure.
/// Per IEEE 1278.1-2012 §5.3.3.1, all PDUs extend this with body-specific logic.
/// </summary>
public abstract record PduBase : IPdu
{
    /// <summary>The DIS protocol magic number (must be 1 or 0x01 per IEEE standard).</summary>
    public virtual byte Magic => 1;

    /// <summary>The DIS protocol version major number.</summary>
    public virtual byte ProtocolVersion => 3;

    /// <summary>The PDU type code (disambiguates PDU family per §5.3).</summary>
    public abstract ushort PdType { get; }

    /// <summary>Gets the total length of this PDU in bytes including header.</summary>
    public abstract int ComputedLength();

    /// <summary>Serializes only the body portion excluding header fields (IEEE 1278.1-2012 §5.3.3.1).</summary>
    /// <param name="buffer">The output buffer to serialize into.</param>
    /// <param name="offset">The starting offset in the buffer.</param>
    public virtual void SerializeBody(Span<byte> buffer, int offset) { }

    /// <summary>Deserializes only the body portion excluding header fields (IEEE 1278.1-2012 §5.3.3.1).</summary>
    /// <param name="buffer">The input buffer to deserialize from.</param>
    /// <param name="offset">The starting offset in the buffer.</param>
    public virtual void DeserializeBody(ReadOnlySpan<byte> buffer, int offset) { }

    /// <summary>Serializes this PDU including header to a buffer per IEEE 1278.1-2012 §5.3.3.1.</summary>
    public void Serialize(Span<byte> buffer, int offset = 0)
    {
        if (buffer.Length < offset + ComputedLength()) 
            throw new ArgumentException("Buffer too small for PDU", nameof(buffer));

        // Write header first
        SpanHelpers.SetByte(buffer.Slice(offset), 0, Magic);
        SpanHelpers.SetByte(buffer.Slice(offset), 1, ProtocolVersion);
        BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset + 2, 2), 0); // reserved = 0
        BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset + 4, 2), PdType);

        // Write body after header (starts at offset + PduHeader.HeaderLength)
        SerializeBody(buffer, offset + PduHeader.HeaderLength);
    }

    /// <summary>Deserializes a PDU from a buffer including header per IEEE 1278.1-2012 §5.3.3.1.</summary>
    public static T Deserialize<T>(ReadOnlySpan<byte> buffer, int offset = 0) where T : PduBase, new()
    {
        if (buffer.Length < offset + PduHeader.HeaderLength) 
            throw new ArgumentException("Buffer too small for header", nameof(buffer));

        var pdu = new T();
        
        // Validate header magic and version
        byte magic = buffer[offset];
        byte versionMajor = buffer[offset + 1];
        
        if (magic != pdu.Magic)
            throw new DisValidationException($"Invalid magic: expected {pdu.Magic}, got {magic}");
            
        if (versionMajor != pdu.ProtocolVersion)
            throw new DisValidationException($"Invalid protocol version: expected {pdu.ProtocolVersion}, got {versionMajor}");

        // Validate PDU type
        ushort actualType = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(offset + 4, 2));
        if (actualType != pdu.PdType)
            throw new DisValidationException($"Invalid PDU type: expected {pdu.PdType}, got {actualType}");

        // Deserialize body
        pdu.DeserializeBody(buffer, offset + PduHeader.HeaderLength);

        return pdu;
    }
}
