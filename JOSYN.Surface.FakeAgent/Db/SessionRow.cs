namespace JOSYN.Surface.FakeAgent;

// THROWAWAY (ADR-031 DS-4): a raw row shape of josyn.SessionStore, read directly from the DEV DB.
// This type is internal and never crosses the ISurfaceAgent seam — FakeAgent maps it to the durable
// SessionSummary DTO. It is deleted wholesale when the real platform-resident agent replaces FakeAgent.
internal sealed class SessionRow
{
    public int       Id              { get; set; }
    public Guid      UID             { get; set; }
    public string    JobTypeName     { get; set; } = string.Empty;
    public string    ExecutionStatus { get; set; } = string.Empty;
    public DateTime  Started         { get; set; }
    public DateTime? Finished        { get; set; }
    public string    UserName        { get; set; } = string.Empty;
    public string    ClientMachine   { get; set; } = string.Empty;
}
