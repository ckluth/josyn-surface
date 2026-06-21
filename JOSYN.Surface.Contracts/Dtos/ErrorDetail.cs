using JOSYN.Jap.Contract;

namespace JOSYN.Surface.Contracts;

/// <summary>
/// The full detail of one stored error, for investigation.
/// </summary>
/// <remarks>
/// Mirrors the reporting need of the legacy <c>get-error-report</c> capability (ADR-030 D-11), designed
/// on API terms (ADR-031 DS-4). <see cref="Environment"/> and <see cref="Machine"/> locate the error
/// once more than one installation is reachable.
/// </remarks>
public sealed record ErrorDetail
{
    /// <summary>Unique identifier of the error.</summary>
    public required Guid Uid { get; init; }

    /// <summary>When the error occurred.</summary>
    public required DateTimeOffset OccurredAt { get; init; }

    /// <summary>Method name of the backend component that observed and reported the error.</summary>
    public required string Causer { get; init; }

    /// <summary>Human-readable error description.</summary>
    public required string Message { get; init; }

    /// <summary>Serialized Result call chain or stack trace, if available.</summary>
    public string? CallStack { get; init; }

    /// <summary>Serialized exception, if available.</summary>
    public string? ExceptionDetails { get; init; }

    /// <summary>Name of the job involved, or <see langword="null"/> for system errors.</summary>
    public string? JobName { get; init; }

    /// <summary>Session GUID if a session was established, or <see langword="null"/>.</summary>
    public Guid? SessionGuid { get; init; }

    /// <summary>Environment this error belongs to.</summary>
    public required RuntimeEnvironment Environment { get; init; }

    /// <summary>Machine this error was read from (the surface target's machine).</summary>
    public required string Machine { get; init; }
}
