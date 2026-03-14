using System.Buffers.Binary;

namespace SisoDis.Core.Common;

/// <summary>
/// Represents 3D vector coordinates with optional W component for normalized forms (IEEE 1278.1-2012 §5.7.4).
/// </summary>
/// <remarks>Per IEEE 1278.1-2012 §5.7.4: X and Y are doubles representing position coordinates, W is optional float for normalized representation.</remarks>
public record struct Vector3Double(double X, double Y, double Z)
{
    /// <summary>Default zero vector.</summary>
    public static Vector3Double Zero => new(0, 0, 0);

    /// <summary>Creates a vector from individual components.</summary>
    public static Vector3Double FromValues(double x, double y, double z) => new(x, y, z);
}
