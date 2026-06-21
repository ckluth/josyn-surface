using JOSYN.Backend.Contracts;
using JOSYN.Jap.Contract;

namespace JOSYN.Surface.Contracts;

/// <summary>
/// A trimmed, transport-safe view of one job session, for reporting.
/// </summary>
/// <remarks>
/// Designed on API terms (ADR-031 DS-2/DS-4), not as a mirror of the <c>josyn.SessionStore</c> table.
/// Reporting-irrelevant fields (process IDs, last-write bookkeeping, raw argument/result payloads) are
/// deliberately omitted; <see cref="Environment"/> and <see cref="Machine"/> are added so a summary is
/// self-locating once more than one installation is reachable.
/// </remarks>
public sealed record SessionSummary
{
    /// <summary>Unique identifier of the session.</summary>
    public required Guid Uid { get; init; }

    /// <summary>Fully qualified name of the job type that ran.</summary>
    public required string JobTypeName { get; init; }

    /// <summary>Current execution status.</summary>
    public required ExecutionStatus ExecutionStatus { get; init; }

    /// <summary>When the session started.</summary>
    public required DateTime Started { get; init; }

    /// <summary>When the session finished, or <see langword="null"/> if still running.</summary>
    public DateTime? Finished { get; init; }

    /// <summary>Name of the user who initiated the session.</summary>
    public required string UserName { get; init; }

    /// <summary>Machine from which the session was requested.</summary>
    public required string ClientMachine { get; init; }

    /// <summary>Environment this session belongs to.</summary>
    public required RuntimeEnvironment Environment { get; init; }

    /// <summary>Machine this session was read from (the surface target's machine).</summary>
    public required string Machine { get; init; }
}
