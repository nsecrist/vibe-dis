namespace SipoDis.Core.Common;

/// <summary>
/// DIS entity type codes per IEEE 1278.1-2012 §5.1, Table 5-3.
/// </summary>
public enum EntityType : byte
{
    /// <summary>Not assigned (type code = 0x0).</summary>
    None = 0,

    /// <summary>Generic entity (placeholder).</summary>
    Generic = 1,

    /// <summary>Physical entity with location (primary use per spec).</summary>
    PhysicalWithLocation = 2,

    /// <summary>Mixed entity type.</summary>
    Mixed = 3
}
