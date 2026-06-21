using JOSYN.Jap.Contract;

namespace JOSYN.Surface.Contracts;

/// <summary>
/// The schedule for one registered job, including all of its time entries, as read from the target
/// installation.
/// </summary>
/// <remarks>
/// Designed on API terms (ADR-031 DS-2/DS-4) — not a mirror of <c>josyn.JobSchedules</c> or
/// <c>josyn.JobScheduleEntries</c>. Transport-safe; DB row shapes never cross the seam.
/// </remarks>
public sealed record JobSchedule
{
    /// <summary>Environment this record was read from.</summary>
    public required RuntimeEnvironment Environment { get; init; }

    /// <summary>Machine this record was read from (the surface target's machine).</summary>
    public required string Machine { get; init; }

    /// <summary>Registry name of the job.</summary>
    public required string JobName { get; init; }

    /// <summary>Whether the entire schedule is currently suspended.</summary>
    public required bool Suspended { get; init; }

    /// <summary>
    /// Date until which the schedule is suspended, if the suspension has an end date.
    /// <see langword="null"/> when not suspended or when the suspension is indefinite.
    /// </summary>
    public DateOnly? SuspendedUntil { get; init; }

    /// <summary>All time entries for this schedule, ordered by argument-record name.</summary>
    public required IReadOnlyList<ScheduleEntrySummary> Entries { get; init; }
}
