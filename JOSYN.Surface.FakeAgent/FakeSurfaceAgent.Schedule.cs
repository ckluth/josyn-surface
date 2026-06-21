using JOSYN.Foundation.ResultPattern;
using JOSYN.Surface.Contracts;
using Microsoft.EntityFrameworkCore;

namespace JOSYN.Surface.FakeAgent;

public sealed partial class FakeSurfaceAgent
{
    /// <inheritdoc/>
    public async Task<Result<JobSchedule>> GetJobSchedule(
        GetJobSchedule query, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var ctx = new SurfaceReadDbContext(devConnectionString);

            var row = await ctx.JobSchedules
                .AsNoTracking()
                .Include(s => s.Entries)
                .FirstOrDefaultAsync(s => s.JobName == query.JobName, cancellationToken);

            if (row is null)
                return SurfaceError.NotFound($"No schedule found for job '{query.JobName}'.");

            return MapJobSchedule(row, query.Target);
        }
        catch (Exception ex)
        {
            return SurfaceError.Internal(
                $"Failed to read schedule for job '{query.JobName}' from the DEV database.", ex);
        }
    }

    // ── mapping ────────────────────────────────────────────────────────────────

    internal static JobSchedule MapJobSchedule(JobScheduleRow row, SurfaceTarget target) => new()
    {
        Environment    = target.Environment,
        Machine        = target.Machine,
        JobName        = row.JobName,
        Suspended      = row.Suspended,
        SuspendedUntil = row.SuspendedUntil,
        Entries        = row.Entries
                            .OrderBy(e => e.ArgumentRecordName)
                            .Select(MapScheduleEntry)
                            .ToList()
    };

    private static ScheduleEntrySummary MapScheduleEntry(JobScheduleEntryRow row) => new()
    {
        ArgumentRecordName = row.ArgumentRecordName,
        ScheduleDefinition = row.ScheduleDefinition,
        ToleranceMinutes   = row.ToleranceMinutes
    };
}
