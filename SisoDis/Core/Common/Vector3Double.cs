using System.Buffers.Binary;

namespace SisoDis.Core.Common;

/// <summary>
/// Represents 3D vector coordinates with optional W component for normalized forms (IEEE 1278.1-2012 §5.7.4).
/// </summary>
/// <remarks>Per IEEE 1278.1-2012 §5.7.4: X and Y are doubles representing position coordinates, W is optional float for normalized representation.</remarks>
public record struct Vector3Double
{
    /// <summary>
    /// The X coordinate (per IEEE 1278.1-2012 §5.7.4).
    /// </summary>
    [Obsolete("Use X property directly from record")] 
    public double DoubleX { get; init; }

    /// <summary>
    /// The Y coordinate (per IEEE 1278.1-2012 §5.7.4).
    /// </summary>
    [Obsolete("Use Y property directly from record")] 
    public double DoubleY { get; init; }

    /// <summary>
    /// The Z coordinate (per IEEE 1278.1-2012 §5.7.4).
    /// </summary>
    [Obsolete("Use Z property directly from record")] 
    public double DoubleZ { get; init; }
}
