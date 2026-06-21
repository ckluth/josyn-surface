namespace JOSYN.Surface.Contracts;

/// <summary>
/// Query: the most recent sessions on a target, newest first.
/// </summary>
/// <remarks>
/// Carries a <see cref="MaxCount"/> bound from MVP-1 (ADR-031 DS-2) so that the eventual REST
/// transport does not have to introduce paging/limiting as a breaking contract change.
/// </remarks>
public sealed record GetRecentSessions
{
    /// <summary>Machine and environment to read from.</summary>
    public required SurfaceTarget Target { get; init; }

    /// <summary>Maximum number of sessions to return. Must be positive.</summary>
    public required int MaxCount { get; init; }
}
