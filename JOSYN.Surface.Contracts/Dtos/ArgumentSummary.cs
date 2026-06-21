namespace JOSYN.Surface.Contracts;

/// <summary>
/// One argument record belonging to a registered job — name and raw content.
/// </summary>
public sealed record ArgumentSummary
{
    /// <summary>Name of the argument record (unique within its job).</summary>
    public required string Name { get; init; }

    /// <summary>Raw content of the argument record (typically JSON).</summary>
    public required string Content { get; init; }
}
