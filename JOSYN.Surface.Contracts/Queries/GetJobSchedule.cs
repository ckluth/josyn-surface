namespace JOSYN.Surface.Contracts;

/// <summary>
/// Query: the schedule (and all its time entries) for one registered job on the target installation.
/// </summary>
/// <remarks>
/// Returns <see cref="SurfaceErrorCategory.NotFound"/> when no schedule exists for the given job
/// name on the target.
/// </remarks>
public sealed record GetJobSchedule
{
    /// <summary>Machine and environment to read from.</summary>
    public required SurfaceTarget Target { get; init; }

    /// <summary>Registry name of the job whose schedule is requested.</summary>
    public required string JobName { get; init; }
}
