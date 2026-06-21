namespace JOSYN.Surface.Contracts;

/// <summary>
/// The outcome of a <see cref="ChangeJobArgument"/> command — the before and after content of the
/// argument record, returned atomically by the platform-resident handler.
/// </summary>
/// <remarks>
/// This is a durable, transport-safe surface DTO (ADR-031 DS-2/DS-4). It mirrors the shape of the
/// backend <c>ArgumentChangeOutcome</c> by value, but it is independent of it: no backend or EF type
/// crosses the <see cref="ISurfaceAgent"/> seam.
/// </remarks>
public sealed record ArgumentChangeOutcome
{
    /// <summary>Registry name of the job.</summary>
    public required string JobName { get; init; }

    /// <summary>Name of the argument record that was changed.</summary>
    public required string ArgumentName { get; init; }

    /// <summary>Content of the argument record before this change.</summary>
    public required string Before { get; init; }

    /// <summary>Content of the argument record after this change.</summary>
    public required string After { get; init; }
}
