using System.Buffers.Binary;

namespace SisoDis.Core.Pdu;

/// <summary>
/// Extension methods for <see cref="IPdu"/> providing full PDU serialization including header.
/// </summary>
public static class PduExtensions
{
    /// <summary>Serializes a PDU including header to a buffer per IEEE 1278.1-2012 §5.3.3.1.</summary>
    /// <param name="pdu">The PDU to serialize.</param>
    /// <param name="buffer">The output buffer to serialize into.</param>
    /// <param name="offset">The starting offset in the buffer.</param>
    public static void Serialize<T>(this T pdu, Span<byte> buffer, int offset = 0) where T : IPdu
    {
        int totalLength = pdu.ComputedLength();
        if (buffer.Length < offset + totalLength)
            throw new ArgumentException("Buffer too small for PDU", nameof(buffer));

        buffer[offset] = pdu.Magic;
        buffer[offset + 1] = pdu.ProtocolVersion;
        BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset + 2, 2), 0);
        BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset + 4, 2), pdu.PdType);

        pdu.SerializeBody(buffer, offset + PduHeader.HeaderLength);
    }
}
