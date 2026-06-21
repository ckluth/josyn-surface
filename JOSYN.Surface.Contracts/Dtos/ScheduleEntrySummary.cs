namespace JOSYN.Surface.Contracts;

/// <summary>
/// One schedule entry belonging to a job schedule — which argument record to use, when, and with
/// what tolerance.
/// </summary>
public sealed record ScheduleEntrySummary
{
    /// <summary>Name of the argument record used for this schedule entry.</summary>
    public required string ArgumentRecordName { get; init; }

    /// <summary>Schedule definition string (cron-like) that controls when this entry fires.</summary>
    public required string ScheduleDefinition { get; init; }

    /// <summary>
    /// How many minutes after the scheduled time the entry may still fire before it is considered
    /// missed. <see langword="null"/> if no tolerance is configured.
    /// </summary>
    public int? ToleranceMinutes { get; init; }
}
