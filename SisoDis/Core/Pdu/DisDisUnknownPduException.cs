/// <summary>
/// Exception thrown when DIS PDU validation fails per IEEE 1278.1-2012 §5.3.3.1.
/// </summary>
public sealed class DisDisUnknownPduException : Exception {
    public DisDisUnknownPduException() : base("DisDisUnknownPduException") { }

    public DisDisUnknownPduException(string message) : base(message) { }

    public DisDisUnknownPduException(string value, string message) : base($"{value}: {message}") { }
}

/// <summary>
/// Represents physical appearance identifier for PDUs (IEEE 1278.1-2012 §5.3.3.1).
/// </summary>
public sealed record PhysicalAppearanceId(int EnumValue);

/// <summary>
/// Represents protocol version identifier for PDUs (IEEE 1278.1-2012 §5.3.3.1).
/// </summary>
public sealed record ProtocolVersion(int EnumValue);