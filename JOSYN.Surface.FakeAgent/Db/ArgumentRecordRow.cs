namespace JOSYN.Surface.FakeAgent;

// THROWAWAY (ADR-031 DS-4): a raw row shape of josyn.ArgumentRecords, read directly from the DEV DB.
// Never crosses the ISurfaceAgent seam. Deleted wholesale when the real agent replaces FakeAgent.
internal sealed class ArgumentRecordRow
{
    public string JobName { get; set; } = string.Empty;
    public string Name    { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;

    public JobRegistrationRow? Registration { get; set; }
}
