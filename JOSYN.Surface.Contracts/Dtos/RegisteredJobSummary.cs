namespace JOSYN.Surface.Contracts;

/// <summary>
/// A brief summary of one registered job — name, technical user, and argument count.
/// Returned by <see cref="GetRegisteredJobs"/> as a discovery listing.
/// </summary>
public sealed record RegisteredJobSummary
{
    /// <summary>Registry name of the job.</summary>
    public required string JobName { get; init; }

    /// <summary>Technical OS user under which this job runs.</summary>
    public required string TechnicalUserName { get; init; }

    /// <summary>Number of argument records registered for this job.</summary>
    public required int ArgumentCount { get; init; }
}
