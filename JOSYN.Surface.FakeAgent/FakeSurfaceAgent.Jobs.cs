using JOSYN.Foundation.ResultPattern;
using JOSYN.Jrp.Launch;
using JOSYN.Jrp.Surface;
using JOSYN.Jrp.Surface.Queries;
using Microsoft.EntityFrameworkCore;

namespace JOSYN.Surface.FakeAgent;

public sealed partial class FakeSurfaceAgent
{
    /// <inheritdoc/>
    public async Task<Result<IReadOnlyList<RegisteredJobSummary>>> GetRegisteredJobs(
        GetRegisteredJobs query, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var ctx = new SurfaceReadDbContext(devConnectionString);

            var rows = await ctx.JobRegistrations
                .AsNoTracking()
                .Include(j => j.ArgumentRecords)
                .OrderBy(j => j.Name)
                .ToListAsync(cancellationToken);

            return Result<IReadOnlyList<RegisteredJobSummary>>.Success(
                rows.Select(MapRegisteredJob).ToList());
        }
        catch (Exception ex)
        {
            return JrpError.Internal("Failed to read job registrations from the DEV database.", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<Result<JobArguments>> GetJobArguments(
        GetJobArguments query, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var ctx = new SurfaceReadDbContext(devConnectionString);

            var row = await ctx.JobRegistrations
                .AsNoTracking()
                .Include(j => j.ArgumentRecords)
                .FirstOrDefaultAsync(j => j.Name == query.JobName, cancellationToken);

            if (row is null)
                return JrpError.NotFound($"No job registered with name '{query.JobName}'.");

            return MapJobArguments(row, query.Target);
        }
        catch (Exception ex)
        {
            return JrpError.Internal(
                $"Failed to read arguments for job '{query.JobName}' from the DEV database.", ex);
        }
    }

    // ── mapping ────────────────────────────────────────────────────────────────

    internal static RegisteredJobSummary MapRegisteredJob(JobRegistrationRow row) => new()
    {
        JobName           = row.Name,
        TechnicalUserName = row.TechnicalUserName,
        ArgumentCount     = row.ArgumentRecords.Count
    };

    internal static JobArguments MapJobArguments(JobRegistrationRow row, JrpTarget target) => new()
    {
        Environment       = target.Environment,
        Machine           = target.Machine,
        JobName           = row.Name,
        TechnicalUserName = row.TechnicalUserName,
        Arguments         = row.ArgumentRecords
                               .OrderBy(a => a.Name)
                               .Select(a => new ArgumentSummary { Name = a.Name, Content = a.Content })
                               .ToList()
    };
}
