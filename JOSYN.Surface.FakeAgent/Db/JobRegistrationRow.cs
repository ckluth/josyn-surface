namespace JOSYN.Surface.FakeAgent;

// THROWAWAY (ADR-031 DS-4): a raw row shape of josyn.JobRegistry, read directly from the DEV DB.
// Never crosses the ISurfaceAgent seam — FakeSurfaceAgent maps it to the durable JobArguments DTO.
// Deleted wholesale when the real platform-resident agent replaces FakeAgent.
internal sealed class JobRegistrationRow
{
    public int    Id                { get; set; }
    public string Name              { get; set; } = string.Empty;
    public string TechnicalUserName { get; set; } = string.Empty;

    public ICollection<ArgumentRecordRow> ArgumentRecords { get; set; } = [];
}
