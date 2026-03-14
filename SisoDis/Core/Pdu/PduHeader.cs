using System.Buffers.Binary;
using SisoDis.Core.Common;
using SisoDis.Core.Serialization;

namespace SisoDis.Core.Pdu;

/// <summary>
/// Represents the DIS protocol header per IEEE 1278.1-2012 §5.3.1, Appendix B.
/// </summary>
public sealed record PduHeader : IEquatable<PduHeader>
{
    private const byte DIS_MAGIC = 0x01;
    private const byte CURRENT_VERSION_MAJOR = 3;

    /// <param name="magic">DIS protocol identifier (always 1 or 0x01 per spec).</param>
    /// <param name="versionMajor">Protocol version: 3 for IEEE 1278.1-2012 compliant.</param>
    public PduHeader(byte magic = DIS_MAGIC, byte versionMajor = CURRENT_VERSION_MAJOR)
    {
        if (magic != DIS_MAGIC) throw new ArgumentException($"Magic must be {DIS_MAGIC}", nameof(magic));
        Magic = magic;
        VersionMajor = versionMajor;
    }

    /// <summary>The DIS protocol magic number (must be 1 or 0x01 per IEEE standard).</summary>
    public byte Magic { get; }

    /// <summary>Protocol version major number.</summary>
    /// <remarks>Must be 3 for IEEE 1278.1-2012 compliance (section 5.3.1).</remarks>
    public byte VersionMajor { get; }

    /// <summary>Returns true if this is the IEEE 1278.1-2012 protocol.</summary>
    public bool IsIeee => VersionMajor == CURRENT_VERSION_MAJOR;

    /// <summary>Total header length in bytes per IEEE standard (6 bytes).</summary>
    public const int HeaderLength = 6;

    /// <summary>Serializes the PDU header to a buffer (IEEE 1278.1-2012 §5.3.1). 
    /// Format: magic(1) + versionMajor(1) + reserved(2) + pduType(2).</summary>
    public void Serialize(Span<byte> buffer, int offset, ushort pduType)
    {
        if (buffer.Length < offset + HeaderLength) throw new ArgumentException("Buffer too small");
        
        SpanHelpers.SetByte(buffer.Slice(offset), 0, Magic);
        SpanHelpers.SetByte(buffer.Slice(offset), 1, VersionMajor);
        BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset + 2, 2), 0); // reserved = 0
        BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset + 4, 2), pduType);
    }

    /// <summary>Deserializes the PDU header from a buffer.</summary>
    public static PduHeader Deserialize(ReadOnlySpan<byte> buffer, int offset)
    {
        if (buffer.Length < offset + HeaderLength) throw new ArgumentException("Buffer too small");
        
        byte magic = buffer[offset];
        byte versionMajor = buffer[offset + 1];
        ushort pduType = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(offset, 2));

        return new PduHeader(magic, versionMajor);
    }

    private static void WritePduType(Span<byte> buffer, int offset, ushort value)
    {
        if (buffer.Length < offset + 2) throw new ArgumentException("Buffer too small");
        BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset), value);
    }
}
