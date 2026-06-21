namespace JOSYN.Surface.Contracts;

/// <summary>
/// Query: all argument records for one registered job on the target installation.
/// </summary>
/// <remarks>
/// Returns <see cref="SurfaceErrorCategory.NotFound"/> when no job with the given name is
/// registered on the target.
/// </remarks>
public sealed record GetJobArguments
{
    /// <summary>Machine and environment to read from.</summary>
    public required SurfaceTarget Target { get; init; }

    /// <summary>Registry name of the job whose arguments are requested.</summary>
    public required string JobName { get; init; }
}
