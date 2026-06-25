using JOSYN.Jap.Contract;
using JOSYN.Jrp.Launch;
using JOSYN.Jrp.Surface;
using JOSYN.Surface.FakeAgent;
using NUnit.Framework;

namespace JOSYN.Surface.Test;

/// <summary>
/// Tests the FakeAgent's DB-row → DTO mapping in isolation (no database). The mapping is the one
/// piece of throwaway scaffolding that must still be correct while it lives, because a mistake there
/// would corrupt the durable DTOs that cross the seam (ADR-031 DS-4 containment rule).
/// </summary>
[TestFixture]
internal sealed class FakeSurfaceAgentMappingTests
{
    private static readonly JrpTarget DevTarget =
        new() { Environment = RuntimeEnvironment.DEV, Machine = "TEST-BOX" };

    [Test]
    public void MapSession_ValidRow_ProducesSummaryWithTargetIdentity()
    {
        var row = new SessionRow
        {
            UID             = Guid.NewGuid(),
            JobTypeName     = "Contoso.DemoJob",
            ExecutionStatus = "running",
            Started         = new DateTime(2026, 6, 21, 10, 0, 0),
            Finished        = null,
            UserName        = "alice",
            ClientMachine   = "CLIENT-1"
        };

        var result = FakeSurfaceAgent.MapSession(row, DevTarget);

        Assert.That(result.Succeeded, Is.True, result.ErrorMessage);
        var summary = result.Value!;
        Assert.Multiple(() =>
        {
            Assert.That(summary.Uid, Is.EqualTo(row.UID));
            Assert.That(summary.JobTypeName, Is.EqualTo("Contoso.DemoJob"));
            Assert.That(summary.ExecutionStatus, Is.EqualTo(SessionStatus.Running));
            Assert.That(summary.Started, Is.EqualTo(row.Started));
            Assert.That(summary.Finished, Is.Null);
            Assert.That(summary.UserName, Is.EqualTo("alice"));
            Assert.That(summary.ClientMachine, Is.EqualTo("CLIENT-1"));
            Assert.That(summary.Environment, Is.EqualTo(RuntimeEnvironment.DEV));
            Assert.That(summary.Machine, Is.EqualTo("TEST-BOX"));
        });
    }

    [Test]
    public void MapSession_UnknownStatus_FailsAsInternal()
    {
        var row = new SessionRow { UID = Guid.NewGuid(), ExecutionStatus = "not-a-real-status" };

        var result = FakeSurfaceAgent.MapSession(row, DevTarget);

        Assert.That(result.Succeeded, Is.False);
        Assert.That(JrpError.CategoryOf(result.ErrorMessage), Is.EqualTo(JrpErrorCategory.Internal));
    }

    [Test]
    public void MapSessions_PropagatesFirstFailure()
    {
        var rows = new[]
        {
            new SessionRow { UID = Guid.NewGuid(), ExecutionStatus = "running" },
            new SessionRow { UID = Guid.NewGuid(), ExecutionStatus = "broken" }
        };

        var result = FakeSurfaceAgent.MapSessions(rows, DevTarget);

        Assert.That(result.Succeeded, Is.False);
        Assert.That(JrpError.CategoryOf(result.ErrorMessage), Is.EqualTo(JrpErrorCategory.Internal));
    }

    [Test]
    public void MapError_CopiesAllFieldsAndStampsTarget()
    {
        var row = new ErrorRow
        {
            UID              = Guid.NewGuid(),
            OccurredAt       = DateTimeOffset.Now,
            Causer           = "SomeMethod",
            Message          = "boom",
            CallStack        = "stack",
            ExceptionDetails = "ex",
            JobName          = "Contoso.DemoJob",
            SessionGuid      = Guid.NewGuid()
        };

        var detail = FakeSurfaceAgent.MapError(row, DevTarget);

        Assert.Multiple(() =>
        {
            Assert.That(detail.Uid, Is.EqualTo(row.UID));
            Assert.That(detail.OccurredAt, Is.EqualTo(row.OccurredAt));
            Assert.That(detail.Causer, Is.EqualTo("SomeMethod"));
            Assert.That(detail.Message, Is.EqualTo("boom"));
            Assert.That(detail.CallStack, Is.EqualTo("stack"));
            Assert.That(detail.ExceptionDetails, Is.EqualTo("ex"));
            Assert.That(detail.JobName, Is.EqualTo("Contoso.DemoJob"));
            Assert.That(detail.SessionGuid, Is.EqualTo(row.SessionGuid));
            Assert.That(detail.Environment, Is.EqualTo(RuntimeEnvironment.DEV));
            Assert.That(detail.Machine, Is.EqualTo("TEST-BOX"));
        });
    }
}
