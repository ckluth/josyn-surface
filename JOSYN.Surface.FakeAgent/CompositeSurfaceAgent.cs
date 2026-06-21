using JOSYN.Backend.SurfaceAgent;
using JOSYN.Foundation.ResultPattern;
using JOSYN.Surface.Contracts;

namespace JOSYN.Surface.FakeAgent;

/// <summary>
/// Composes the read-only <see cref="FakeSurfaceAgent"/> (DS-4 throwaway) with the real
/// platform-resident <see cref="SurfaceCommandHandler"/> for write operations (ADR-031 DS-5).
/// </summary>
/// <remarks>
/// Reads are served by <see cref="FakeSurfaceAgent"/> (direct DEV-DB access — the scoped DS-4
/// exception, read-only by construction). Write commands are forwarded to the backend
/// <see cref="SurfaceCommandHandler"/> which owns the store write and lives in the platform repo.
/// <para>
/// This composition keeps DS-4 honest: the fake never covers mutations. When the HttpAgent phase
/// arrives, this class is deleted and replaced by a single <c>HttpAgent</c> that handles both reads
/// and writes over REST (the DS-2 swap guarantee). The <see cref="ISurfaceAgent"/> contract above
/// it changes nothing.
/// </para>
/// </remarks>
public sealed class CompositeSurfaceAgent(
    FakeSurfaceAgent      reads,
    ISurfaceCommandHandler writes) : ISurfaceAgent
{
    // ── reads — delegated to the throwaway FakeAgent (DS-4) ──────────────────

    /// <inheritdoc/>
    public Task<Result<IReadOnlyList<SessionSummary>>> GetRecentSessions(
        GetRecentSessions query, CancellationToken cancellationToken = default)
        => reads.GetRecentSessions(query, cancellationToken);

    /// <inheritdoc/>
    public Task<Result<ErrorDetail>> GetErrorDetail(
        GetErrorDetail query, CancellationToken cancellationToken = default)
        => reads.GetErrorDetail(query, cancellationToken);

    /// <inheritdoc/>
    public Task<Result<IReadOnlyList<RegisteredJobSummary>>> GetRegisteredJobs(
        GetRegisteredJobs query, CancellationToken cancellationToken = default)
        => reads.GetRegisteredJobs(query, cancellationToken);

    /// <inheritdoc/>
    public Task<Result<JobArguments>> GetJobArguments(
        GetJobArguments query, CancellationToken cancellationToken = default)
        => reads.GetJobArguments(query, cancellationToken);

    /// <inheritdoc/>
    public Task<Result<JobSchedule>> GetJobSchedule(
        GetJobSchedule query, CancellationToken cancellationToken = default)
        => reads.GetJobSchedule(query, cancellationToken);

    // ── writes — delegated to the platform-resident handler (DS-5) ───────────

    /// <inheritdoc/>

    public Task<Result<ArgumentChangeOutcome>> ChangeJobArgument(
        ChangeJobArgument command, CancellationToken cancellationToken = default)
    {
        // The backend handler is synchronous at MVP-2b (transport deferred). Wrap in
        // Task.FromResult so the seam invariant (async by shape) is honoured above.
        var backendCommand = new ChangeJobArgumentCommand
        {
            Actor         = command.Actor,
            Environment   = command.Target.Environment.ToString(),
            Machine       = command.Target.Machine,
            CorrelationId = command.CorrelationId,
            JobName       = command.JobName,
            ArgumentName  = command.ArgumentName,
            Content       = command.Content
        };

        var outcome = writes.HandleChangeJobArgument(backendCommand);

        if (!outcome.Succeeded)
            return Task.FromResult(outcome.ToResult<ArgumentChangeOutcome>());

        return Task.FromResult(Result<ArgumentChangeOutcome>.Success(new ArgumentChangeOutcome
        {
            JobName      = outcome.Value.JobName,
            ArgumentName = outcome.Value.ArgumentName,
            Before       = outcome.Value.Before,
            After        = outcome.Value.After
        }));
    }
}
