using JOSYN.Jap.Contract;

namespace JOSYN.Surface.Contracts;

/// <summary>
/// All argument records for one registered job, as read from the target installation.
/// </summary>
/// <remarks>
/// Designed on API terms (ADR-031 DS-2/DS-4) — not a mirror of <c>josyn.JobRegistry</c> or
/// <c>josyn.ArgumentRecords</c>. The DTO is transport-safe; raw DB row shapes never cross the seam.
/// </remarks>
public sealed record JobArguments
{
    /// <summary>Environment this record was read from.</summary>
    public required RuntimeEnvironment Environment { get; init; }

    /// <summary>Machine this record was read from (the surface target's machine).</summary>
    public required string Machine { get; init; }

    /// <summary>Registry name of the job.</summary>
    public required string JobName { get; init; }

    /// <summary>Technical OS user under which this job runs.</summary>
    public required string TechnicalUserName { get; init; }

    /// <summary>All argument records belonging to this job, ordered by name.</summary>
    public required IReadOnlyList<ArgumentSummary> Arguments { get; init; }
}
