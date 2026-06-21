using JOSYN.Jap.Contract;

namespace JOSYN.Surface.Contracts;

/// <summary>
/// Identifies the machine and environment a query or command is addressed to.
/// </summary>
/// <remarks>
/// Every request carries a target from MVP-1 onward (ADR-031 DS-2), even though MVP-1 reaches
/// only a single DEV installation. This keeps cross-environment / cross-machine targeting a
/// property of the durable contract rather than something retrofitted when the aggregator and
/// the cross-machine REST layer (ADR-030 D-4/D-16) arrive.
/// </remarks>
public sealed record SurfaceTarget
{
    /// <summary>The environment the request is addressed to (ADR-010: environment = installation).</summary>
    public required RuntimeEnvironment Environment { get; init; }

    /// <summary>The machine the request is addressed to. A short, stable machine identifier.</summary>
    public required string Machine { get; init; }
}
