/// <summary>
/// Represents success or failure of an operation with optional error message and context.
/// Per IEEE 1278.1-2012 §5.3.3.1, used for validation result tracking.
/// </summary>
public record Result(
    bool Success = true,
    string? Error = null,
    object? Context = null
);
