using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JOSYN.Foundation.ResultPattern;

namespace JOSYN.Surface.Contracts;

/// <summary>
/// The single seam between the surface shells and whatever answers their queries and commands.
/// </summary>
/// <remarks>
/// This is the load-bearing abstraction of the whole surface (ADR-031 DS-2): the shells depend on
/// it, never on a transport. MVP-1 satisfies it in-process with a fake agent that reads the DEV DB
/// directly; a later phase swaps in an HTTP-backed implementation talking to the real
/// platform-resident agent (ADR-030 D-16/D-17) — with no change above this seam.
/// <para>
/// To make that swap genuinely free, the seam is shaped for the network boundary it will later
/// cross: every method is asynchronous and cancellable, every request carries a
/// <see cref="SurfaceTarget"/>, results use the named <see cref="SurfaceErrorCategory"/> taxonomy
/// via <see cref="SurfaceError"/>, and list queries are bounded. It is also the ADR-030 D-20
/// boundary: above it, shells may be idiomatic; below it, implementations are functional-first.
/// </para>
/// </remarks>
public interface ISurfaceAgent
{
    /// <summary>Returns the most recent sessions on the query's target, newest first.</summary>
    Task<Result<IReadOnlyList<SessionSummary>>> GetRecentSessions(
        GetRecentSessions query, CancellationToken cancellationToken = default);

    /// <summary>Returns the full detail of a single error, or a <see cref="SurfaceErrorCategory.NotFound"/> failure.</summary>
    Task<Result<ErrorDetail>> GetErrorDetail(
        GetErrorDetail query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all registered jobs on the query's target as a lightweight discovery listing.
    /// </summary>
    Task<Result<IReadOnlyList<RegisteredJobSummary>>> GetRegisteredJobs(
        GetRegisteredJobs query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all argument records for a single registered job, or
    /// <see cref="SurfaceErrorCategory.NotFound"/> if the job is not registered on the target.
    /// </summary>
    Task<Result<JobArguments>> GetJobArguments(
        GetJobArguments query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the schedule (and all its time entries) for a single registered job, or
    /// <see cref="SurfaceErrorCategory.NotFound"/> if no schedule exists for the job on the target.
    /// </summary>
    Task<Result<JobSchedule>> GetJobSchedule(
        GetJobSchedule query, CancellationToken cancellationToken = default);
    /// <summary>
    /// Changes the content of an existing argument record for a registered job.
    /// Returns <see cref="SurfaceErrorCategory.NotFound"/> when the job or argument is absent.
    /// Never creates a new record — use a dedicated create command for that.
    /// </summary>
    Task<Result<ArgumentChangeOutcome>> ChangeJobArgument(
        ChangeJobArgument command, CancellationToken cancellationToken = default);
}
