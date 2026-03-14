using System.Buffers.Binary;

namespace SisoDis.Core.Pdu;

/// <summary>
/// Represents a DIS Protocol Version per IEEE 1278.1-2012 §5.3.1, Appendix B.
/// </summary>
public sealed class ProtocolVersion
{
    private const byte DIS_MAGIC = 0x01;      // IEEE 1278.1 DIS magic number
    private const byte CURRENT_VERSION_MAJOR = 3;

    /// <param name="magic">DIS protocol identifier (always 1 or 0x01 per spec).</param>
    /// <param name="version">Protocol version: 3 for IEEE 1278.1-2012 compliant.</param>
    public ProtocolVersion(byte magic = 1, byte version = 3)
    {
        Magic = magic;
        VersionMajor = version;
    }

    /// <summary>The DIS protocol magic number (must be 1 or 0x01 per IEEE standard).</summary>
    public byte Magic { get; }

    /// <summary>Protocol version major number.</summary>
    /// <remarks>Must be 3 for IEEE 1278.1-2012 compliance (section 5.3.1).</remarks>
    public byte VersionMajor { get; }

    /// <summary>Returns true	if this is the IEEE 1278.1-2012 protocol.</summary>
    public bool IsIeee => VersionMajor == CURRENT_VERSION_MAJOR;
}
