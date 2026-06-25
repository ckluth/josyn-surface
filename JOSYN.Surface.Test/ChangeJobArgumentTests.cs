using JOSYN.Backend.Gateway;
using JOSYN.Foundation.ResultPattern;
using JOSYN.Jap.Contract;
using JOSYN.Jrp.Launch;
using JOSYN.Jrp.Surface;
using JOSYN.Surface.FakeAgent;
using NUnit.Framework;
using SurfaceOutcome = JOSYN.Jrp.Surface.ArgumentChangeOutcome;

namespace JOSYN.Surface.Test;

/// <summary>
/// Tests for the MVP-2b write path: command envelope shape, handler delegation,
/// outcome mapping, and error propagation through <see cref="CompositeSurfaceAgent"/>.
/// </summary>
[TestFixture]
internal sealed class ChangeJobArgumentTests
{
    private static readonly JrpTarget DevTarget =
        new() { Environment = RuntimeEnvironment.DEV, Machine = "TEST-BOX" };

    // ── ChangeJobArgument command record ──────────────────────────────────────

    [Test]
    public void ChangeJobArgument_CommandRecord_HasAllEnvelopeFields()
    {
        // Envelope fields (actor, target, correlation) must be present from day 1 (DS-5).
        var correlationId = Guid.NewGuid();
        var cmd = new ChangeJobArgument
        {
            Target        = DevTarget,
            Actor         = "alice",
            CorrelationId = correlationId,
            JobName       = "Contoso.DemoJob",
            ArgumentName  = "default",
            Content       = "{}"
        };

        Assert.Multiple(() =>
        {
            Assert.That(cmd.Actor,          Is.EqualTo("alice"));
            Assert.That(cmd.Target,         Is.EqualTo(DevTarget));
            Assert.That(cmd.CorrelationId,  Is.EqualTo(correlationId));
            Assert.That(cmd.JobName,        Is.EqualTo("Contoso.DemoJob"));
            Assert.That(cmd.ArgumentName,   Is.EqualTo("default"));
            Assert.That(cmd.Content,        Is.EqualTo("{}"));
        });
    }

    // ── ArgumentChangeOutcome DTO (surface-side) ──────────────────────────────

    [Test]
    public void ArgumentChangeOutcome_IsTransportSafe_NoBackendTypesRequired()
    {
        // Constructing the surface DTO without any backend/EF references proves it is independent.
        var outcome = new SurfaceOutcome
        {
            JobName      = "Contoso.DemoJob",
            ArgumentName = "default",
            Before       = "old",
            After        = "new"
        };

        Assert.Multiple(() =>
        {
            Assert.That(outcome.JobName,      Is.EqualTo("Contoso.DemoJob"));
            Assert.That(outcome.ArgumentName, Is.EqualTo("default"));
            Assert.That(outcome.Before,       Is.EqualTo("old"));
            Assert.That(outcome.After,        Is.EqualTo("new"));
        });
    }

    // ── CompositeSurfaceAgent write delegation ────────────────────────────────

    [Test]
    public async Task CompositeSurfaceAgent_SuccessfulChange_MapsOutcomeFromHandler()
    {
        var handler = new StubCommandHandler(Result<SurfaceOutcome>.Success(
            new SurfaceOutcome
            {
                JobName      = "Contoso.DemoJob",
                ArgumentName = "default",
                Before       = "old-content",
                After        = "new-content"
            }));

        var agent = BuildCompositeAgent(handler);
        var cmd   = BuildCommand("Contoso.DemoJob", "default", "new-content");

        var result = await agent.ChangeJobArgument(cmd);

        Assert.That(result.Succeeded, Is.True, result.ErrorMessage);
        Assert.Multiple(() =>
        {
            Assert.That(result.Value!.JobName,      Is.EqualTo("Contoso.DemoJob"));
            Assert.That(result.Value!.ArgumentName, Is.EqualTo("default"));
            Assert.That(result.Value!.Before,       Is.EqualTo("old-content"));
            Assert.That(result.Value!.After,        Is.EqualTo("new-content"));
        });
    }

    [Test]
    public async Task CompositeSurfaceAgent_HandlerReturnsNotFound_PropagatesFailure()
    {
        var handler = new StubCommandHandler(
            Result<SurfaceOutcome>.Fail("[NotFound] No argument 'x' found."));

        var agent  = BuildCompositeAgent(handler);
        var cmd    = BuildCommand("Contoso.DemoJob", "x", "irrelevant");

        var result = await agent.ChangeJobArgument(cmd);

        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("[NotFound]"));
    }

    [Test]
    public async Task CompositeSurfaceAgent_HandlerReceivesFullEnvelope()
    {
        var correlationId = Guid.NewGuid();
        var captured = new CaptureCommandHandler();
        var agent    = BuildCompositeAgent(captured);

        var cmd = new ChangeJobArgument
        {
            Target        = DevTarget,
            Actor         = "bob",
            CorrelationId = correlationId,
            JobName       = "Contoso.DemoJob",
            ArgumentName  = "default",
            Content       = "{\"x\":1}"
        };

        await agent.ChangeJobArgument(cmd);

        Assert.That(captured.ReceivedCommand, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(captured.ReceivedCommand!.Actor,         Is.EqualTo("bob"));
            Assert.That(captured.ReceivedCommand!.CorrelationId, Is.EqualTo(correlationId));
            Assert.That(captured.ReceivedCommand!.JobName,       Is.EqualTo("Contoso.DemoJob"));
            Assert.That(captured.ReceivedCommand!.ArgumentName,  Is.EqualTo("default"));
            Assert.That(captured.ReceivedCommand!.Content,       Is.EqualTo("{\"x\":1}"));
        });
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private static ChangeJobArgument BuildCommand(string jobName, string argName, string content) =>
        new()
        {
            Target        = DevTarget,
            Actor         = "test-actor",
            CorrelationId = Guid.NewGuid(),
            JobName       = jobName,
            ArgumentName  = argName,
            Content       = content
        };

    private static CompositeSurfaceAgent BuildCompositeAgent(IGatewayCommandHandler handler)
    {
        // FakeAgent reads are not exercised by write tests; use a dummy connection string.
        // CompositeSurfaceAgent wraps a FakeSurfaceAgent (reads) + handler (writes).
        return new CompositeSurfaceAgent(
            new FakeSurfaceAgent("Server=.;Database=dummy;"),
            handler);
    }

    // ── stubs ─────────────────────────────────────────────────────────────────

    private sealed class StubCommandHandler(
        Result<SurfaceOutcome> result) : IGatewayCommandHandler
    {
        public Result<SurfaceOutcome> HandleChangeJobArgument(
            ChangeJobArgument command) => result;
    }

    private sealed class CaptureCommandHandler : IGatewayCommandHandler
    {
        public ChangeJobArgument? ReceivedCommand { get; private set; }

        public Result<SurfaceOutcome> HandleChangeJobArgument(ChangeJobArgument command)
        {
            ReceivedCommand = command;
            return Result<SurfaceOutcome>.Success(new SurfaceOutcome
            {
                JobName      = command.JobName,
                ArgumentName = command.ArgumentName,
                Before       = "x",
                After        = command.Content
            });
        }
    }
}
