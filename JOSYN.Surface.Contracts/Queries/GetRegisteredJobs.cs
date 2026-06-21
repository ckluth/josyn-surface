namespace JOSYN.Surface.Contracts;

/// <summary>
/// Query: all registered jobs on the target installation, as a lightweight discovery listing.
/// </summary>
public sealed record GetRegisteredJobs
{
    /// <summary>Machine and environment to read from.</summary>
    public required SurfaceTarget Target { get; init; }
}
