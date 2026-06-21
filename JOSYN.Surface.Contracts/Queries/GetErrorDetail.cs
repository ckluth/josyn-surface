namespace JOSYN.Surface.Contracts;

/// <summary>
/// Query: the full detail of a single error on a target, by its UID.
/// </summary>
public sealed record GetErrorDetail
{
    /// <summary>Machine and environment to read from.</summary>
    public required SurfaceTarget Target { get; init; }

    /// <summary>Unique identifier of the error to fetch.</summary>
    public required Guid ErrorUid { get; init; }
}
