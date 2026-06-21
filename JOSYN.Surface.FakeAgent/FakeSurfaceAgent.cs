using JOSYN.Backend.Contracts;
using JOSYN.Foundation.ResultPattern;
using JOSYN.Surface.Contracts;
using Microsoft.EntityFrameworkCore;

namespace JOSYN.Surface.FakeAgent;

/// <summary>
/// THROWAWAY (ADR-031 DS-4): an in-process <see cref="ISurfaceAgent"/> that answers the MVP-1 read
/// queries by reading the DEV <c>josyn-db-local</c> database directly.
/// </summary>
/// <remarks>
/// This is the deliberate, scoped, DEV-only, read-only exception to ADR-030 D-8 (API-mediated) and
/// D-17 (store access is platform-resident). It exists so the surface contracts and CLI shell can be
/// built and proven before the real platform-resident agent and its REST API exist. It is removed
/// wholesale when that agent lands — nothing above the <see cref="ISurfaceAgent"/> seam depends on it.
/// <para>
/// Containment rule: no DB row shape (<c>SessionRow</c>, <c>ErrorRow</c>) ever crosses the seam; this
/// agent maps every raw read to the durable DTOs here, internally.
/// </para>
/// </remarks>
public sealed partial class FakeSurfaceAgent(string devConnectionString) : ISurfaceAgent
{
    /// <inheritdoc/>
    public async Task<Result<IReadOnlyList<SessionSummary>>> GetRecentSessions(
        GetRecentSessions query, CancellationToken cancellationToken = default)
    {
        if (query.MaxCount <= 0)
            return SurfaceError.Invalid($"{nameof(query.MaxCount)} must be positive, was {query.MaxCount}.");

        try
        {
            await using var ctx = new SurfaceReadDbContext(devConnectionString);

            var rows = await ctx.Sessions
                .AsNoTracking()
                .OrderByDescending(s => s.Started)
                .Take(query.MaxCount)
                .ToListAsync(cancellationToken);

            return MapSessions(rows, query.Target);
        }
        catch (Exception ex)
        {
            return SurfaceError.Internal("Failed to read recent sessions from the DEV database.", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<Result<ErrorDetail>> GetErrorDetail(
        GetErrorDetail query, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var ctx = new SurfaceReadDbContext(devConnectionString);

            var row = await ctx.Errors
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.UID == query.ErrorUid, cancellationToken);

            if (row is null)
                return SurfaceError.NotFound($"No error found for UID '{query.ErrorUid}'.");

            return MapError(row, query.Target);
        }
        catch (Exception ex)
        {
            return SurfaceError.Internal($"Failed to read error '{query.ErrorUid}' from the DEV database.", ex);
        }
    }

    // ── mapping (DB row → durable DTO) ─────────────────────────────────────────
    // Internal + static so it is unit-testable without a database. Pure: no mutation of inputs.

    internal static Result<IReadOnlyList<SessionSummary>> MapSessions(
        IReadOnlyList<SessionRow> rows, SurfaceTarget target)
    {
        var summaries = new List<SessionSummary>(rows.Count);
        foreach (var row in rows)
        {
            var mapped = MapSession(row, target);
            if (!mapped.Succeeded)
                return mapped.ToResult<IReadOnlyList<SessionSummary>>();
            summaries.Add(mapped.Value);
        }

        return Result<IReadOnlyList<SessionSummary>>.Success(summaries);
    }

    internal static Result<SessionSummary> MapSession(SessionRow row, SurfaceTarget target)
    {
        var status = ExecutionStatusParser.Parse(row.ExecutionStatus);
        if (!status.Succeeded)
            return SurfaceError.Internal(
                $"Session '{row.UID}' has an unrecognised ExecutionStatus '{row.ExecutionStatus}'.");

        return new SessionSummary
        {
            Uid             = row.UID,
            JobTypeName     = row.JobTypeName,
            ExecutionStatus = status.Value,
            Started         = row.Started,
            Finished        = row.Finished,
            UserName        = row.UserName,
            ClientMachine   = row.ClientMachine,
            Environment     = target.Environment,
            Machine         = target.Machine
        };
    }

    internal static ErrorDetail MapError(ErrorRow row, SurfaceTarget target) => new()
    {
        Uid              = row.UID,
        OccurredAt       = row.OccurredAt,
        Causer           = row.Causer,
        Message          = row.Message,
        CallStack        = row.CallStack,
        ExceptionDetails = row.ExceptionDetails,
        JobName          = row.JobName,
        SessionGuid      = row.SessionGuid,
        Environment      = target.Environment,
        Machine          = target.Machine
    };
}
