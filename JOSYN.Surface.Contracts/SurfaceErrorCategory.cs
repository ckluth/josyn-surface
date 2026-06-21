namespace JOSYN.Surface.Contracts;

/// <summary>
/// The stable categories of failure the surface seam can report.
/// </summary>
/// <remarks>
/// Named from MVP-1 (ADR-031 DS-2) so that phase-2 REST transport failures map onto existing
/// categories rather than forcing new ones. Each category has an obvious HTTP mapping when the
/// <c>HttpAgent</c> arrives (e.g. <see cref="NotFound"/> → 404, <see cref="Unauthorized"/> → 401/403,
/// <see cref="Unavailable"/> → 503, <see cref="Timeout"/> → 504). MVP-1's in-process
/// <c>FakeAgent</c> only ever produces <see cref="NotFound"/>, <see cref="Invalid"/>, and
/// <see cref="Internal"/>; the transport-only categories exist so the contract is complete before
/// the network boundary is real.
/// </remarks>
public enum SurfaceErrorCategory
{
    /// <summary>The requested resource does not exist (e.g. unknown error UID).</summary>
    NotFound,

    /// <summary>The request itself was malformed or violated a precondition.</summary>
    Invalid,

    /// <summary>The caller is not authorised. Reserved for when auth is enforced (ADR-030 D-9).</summary>
    Unauthorized,

    /// <summary>The target machine/agent could not be reached. Transport-only.</summary>
    Unavailable,

    /// <summary>The request exceeded its time budget or was cancelled. Transport-only.</summary>
    Timeout,

    /// <summary>An unexpected internal failure occurred while serving the request.</summary>
    Internal
}
