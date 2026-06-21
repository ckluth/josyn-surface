namespace JOSYN.Surface.FakeAgent;

// THROWAWAY (ADR-031 DS-4): a raw row shape of josyn.ErrorStore, read directly from the DEV DB.
// Internal; never crosses the ISurfaceAgent seam. Mapped to the durable ErrorDetail DTO.
internal sealed class ErrorRow
{
    public int            Id               { get; set; }
    public Guid           UID              { get; set; }
    public DateTimeOffset OccurredAt       { get; set; }
    public string         Causer           { get; set; } = string.Empty;
    public string         Message          { get; set; } = string.Empty;
    public string?        CallStack        { get; set; }
    public string?        ExceptionDetails { get; set; }
    public string?        JobName          { get; set; }
    public Guid?          SessionGuid      { get; set; }
}
