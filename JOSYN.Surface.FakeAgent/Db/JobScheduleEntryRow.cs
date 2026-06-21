namespace JOSYN.Surface.FakeAgent;

// THROWAWAY (ADR-031 DS-4): a raw row shape of josyn.JobScheduleEntries, read directly from the DEV DB.
// Never crosses the ISurfaceAgent seam. Deleted wholesale when the real agent replaces FakeAgent.
internal sealed class JobScheduleEntryRow
{
    public string JobName            { get; set; } = string.Empty;
    public string ArgumentRecordName { get; set; } = string.Empty;
    public string ScheduleDefinition { get; set; } = string.Empty;
    public int?   ToleranceMinutes   { get; set; }

    public JobScheduleRow? Schedule { get; set; }
}
