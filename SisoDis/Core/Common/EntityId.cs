using System.Buffers.Binary;

namespace SisoDis.Core.Common;

/// <summary>
/// Represents the DIS entity ID (IEEE 1278.1-2012 §5.3.1).
/// </summary>
/// <remarks>Per IEEE 1278.1-2012 §5.3.1 and Section 7.2.2: Entity IDs use ENTITY_REFERENCE_TYPE to determine format:
///     - SHORT_ENTITY_ID (relative): 2 bytes as unsigned short, range 0..65535
///     - ABSOLUTE_ENTITY_ID: full IEEE address stored externally or in extended forms</remarks>
public record struct EntityId(int Value)
{
    /// <summary>The entity reference type of this ID (Absolute, Relative, or Short Handle).</summary>
    public const byte DefaultType = 2;

    /// <summary>Creates an absolute entity identifier per IEEE 1278.1-2012 §5.3.1.</summary>
    public static EntityId Absolute(int value) => new(value);

    /// <summary>Creates a relative entity identifier (unsigned short, range 0..65535).</summary>
    public static EntityId Relative(int value)
    {
        if (value < 0 || value > ushort.MaxValue) throw new ArgumentOutOfRangeException(nameof(value), "Relative ID must be 0..65535");
        return new EntityId(value);
    }
}
