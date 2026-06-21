namespace JOSYN.Surface.FakeAgent;

// THROWAWAY (ADR-031 DS-4): a raw row shape of josyn.JobSchedules, read directly from the DEV DB.
// Never crosses the ISurfaceAgent seam. Deleted wholesale when the real agent replaces FakeAgent.
internal sealed class JobScheduleRow
{
    public string    JobName        { get; set; } = string.Empty;
    public bool      Suspended      { get; set; }
    public DateOnly? SuspendedUntil { get; set; }

    public ICollection<JobScheduleEntryRow> Entries { get; set; } = [];
}
