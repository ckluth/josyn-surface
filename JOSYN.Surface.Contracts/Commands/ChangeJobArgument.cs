namespace JOSYN.Surface.Contracts;

/// <summary>
/// Command: change the content of an <em>existing</em> argument record for a registered job.
/// </summary>
/// <remarks>
/// This is the platform's first write command (ADR-031 DS-5 MVP-2b). It is change-only — it fails
/// with <see cref="SurfaceErrorCategory.NotFound"/> when the job or argument record is absent and
/// never silently creates a new record. Use a dedicated create command for that.
/// <para>
/// The full command envelope (actor, target, correlation) is designed-in from the first write so
/// that audit/auth enforcement (ADR-030 D-9/D-10) can be added later without a contract change.
/// </para>
/// </remarks>
public sealed record ChangeJobArgument
{
    /// <summary>Machine and environment the command is addressed to.</summary>
    public required SurfaceTarget Target { get; init; }

    /// <summary>
    /// The user or service principal issuing the command. Enforcement deferred; present from day 1.
    /// </summary>
    public required string Actor { get; init; }

    /// <summary>
    /// Correlation ID for tracing this command through logs and future audit records.
    /// </summary>
    public required Guid CorrelationId { get; init; }

    /// <summary>Registry name of the job whose argument is to be changed.</summary>
    public required string JobName { get; init; }

    /// <summary>Name of the argument record to change.</summary>
    public required string ArgumentName { get; init; }

    /// <summary>New content for the argument record.</summary>
    public required string Content { get; init; }
}
