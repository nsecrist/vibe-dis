using System.Buffers.Binary;

namespace SisoDis.Core.Serialization;

/// <summary>
/// Static helper methods for serialization without allocations.
/// </summary>
public static class SpanHelpers
{
    /// <summary>Sets a byte value at the given buffer position.</summary>
    public static void SetByte(Span<byte> buffer, int index, byte value)
        => buffer[index] = value;

    /// <summary>Serializes an unsigned short (big-endian per IEEE standard).</summary>
    public static void WriteUInt16(Span<byte> buffer, int offset, ushort value)
        => BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset), value);

    /// <summary>Reads a signed 16-bit integer (big-endian per IEEE).</summary>
    public static short ReadInt16(ReadOnlySpan<byte> buffer, int offset = 0)
        => BinaryPrimitives.ReadInt16BigEndian(buffer.Slice(offset));

    /// <summary>Reads a double precision floating point value.</summary>
    public static double ReadDouble(Span<byte> buffer, int offset = 0)
        => BinaryPrimitives.ReadDoubleBigEndian(buffer.Slice(offset));
}
