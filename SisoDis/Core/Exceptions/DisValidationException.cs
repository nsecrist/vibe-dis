namespace SisoDis;

/// <summary>
/// Thrown when a DIS PDU field contains an invalid value per IEEE 1278.1-2012 §5.3.
/// </summary>
/// <remarks>Common causes include entity IDs outside valid range (0..65535 for short ID), out-of-spec 
/// type codes, missing required fields during deserialization validation, or coordinate values 
/// exceeding specification bounds.</remarks>
public sealed class DisValidationException : Exception
{
    /// <param name="message">Detailed message describing the validation failure and offending field</param>
    public DisValidationException(string message) : base(message) { }

    /// <param name="innerException">Cause of the exception (validation cascade)</param>
    public DisValidationException(string message, Exception innerException) 
        : base(message, innerException) { }
}

/// <summary>
/// Thrown when a deserialization operation encounters an unrecognized DIS PDU type per IEEE 1278.1-2012 §5.3.4.
/// </summary>
/// <remarks>Indicates the magic/version/type combination does not match any registered factory handlers.</remarks>
public sealed class DisUnknownPduException : Exception
{
    private readonly ushort _pdutype;

    public ushort PdType => _pdutype;

    /// <param name="pdutype">The unrecognized PDU type code found during deserialization</param>
    public DisUnknownPduException(ushort pdutype) 
        : base($"Unexpected PDU type 0x{pdutype:X4}")
    {
        _pdutype = pdutype;
    }
}
