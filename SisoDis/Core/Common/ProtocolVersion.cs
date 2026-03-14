namespace SipoDis.Core.Common;

/// <summary>
/// DIS protocol version enumeration per IEEE 1278.1-2012 §5.3.1, Appendix B.
/// </summary>
public static class ProtocolVersion
{
    /// <summary>The 2012 revision (DIS Protocol Version 0x03).</summary>
    public const byte Version2012 = 3;

    /// <summary>The 1990 revision.</summary>
    public const byte Version1990 = 2;

    /// <summary>The 1984 original protocol version.</summary>
    public const byte Version1984 = 1;
}
