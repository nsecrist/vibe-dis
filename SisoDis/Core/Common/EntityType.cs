namespace SisoDis.Core.Common;

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

/// <summary>
/// Extension methods for EntityType enumeration.
/// </summary>
public static class EntityTypeExtensions
{
    /// <summary>Converts EntityType to ushort value per IEEE standard.</summary>
    public static ushort ToUShort(this EntityType type) => (ushort)type;

    /// <summary>Parses EntityType from ushort value per IEEE §5.1 Table 5-3.</summary>
    public static EntityType FromUShort(ushort value) => 
        Enum.IsDefined(typeof(EntityType), value) 
            ? (EntityType)value 
            : EntityType.None;
}
