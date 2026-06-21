using JOSYN.Jap.Contract;
using JOSYN.Surface.Contracts;
using JOSYN.Surface.FakeAgent;
using NUnit.Framework;

namespace JOSYN.Surface.Test;

/// <summary>
/// Tests the FakeAgent's DB-row → DTO mapping for job arguments and schedule verbs in isolation
/// (no database). Mirrors the posture of <see cref="FakeSurfaceAgentMappingTests"/>: the mapping is
/// throwaway scaffolding, but it must be correct while it lives (ADR-031 DS-4 containment rule).
/// </summary>
[TestFixture]
internal sealed class FakeSurfaceAgentJobsMappingTests
{
    private static readonly SurfaceTarget DevTarget =
        new() { Environment = RuntimeEnvironment.DEV, Machine = "TEST-BOX" };

    // ── GetRegisteredJobs / MapRegisteredJob ───────────────────────────────────

    [Test]
    public void MapRegisteredJob_CopiesNameUserAndArgumentCount()
    {
        var row = new JobRegistrationRow
        {
            Name              = "Contoso.DemoJob",
            TechnicalUserName = "svc_josyn",
            ArgumentRecords   = [new ArgumentRecordRow { Name = "default" }, new ArgumentRecordRow { Name = "alt" }]
        };

        var summary = FakeSurfaceAgent.MapRegisteredJob(row);

        Assert.Multiple(() =>
        {
            Assert.That(summary.JobName,           Is.EqualTo("Contoso.DemoJob"));
            Assert.That(summary.TechnicalUserName, Is.EqualTo("svc_josyn"));
            Assert.That(summary.ArgumentCount,     Is.EqualTo(2));
        });
    }

    [Test]
    public void MapRegisteredJob_NoArguments_ArgumentCountIsZero()
    {
        var row = new JobRegistrationRow { Name = "Empty.Job", TechnicalUserName = "svc_x" };

        var summary = FakeSurfaceAgent.MapRegisteredJob(row);

        Assert.That(summary.ArgumentCount, Is.EqualTo(0));
    }

    // ── GetJobArguments / MapJobArguments ──────────────────────────────────────

    [Test]
    public void MapJobArguments_MapsAllFieldsAndStampsTarget()
    {
        var row = new JobRegistrationRow
        {
            Name              = "Contoso.DemoJob",
            TechnicalUserName = "svc_josyn",
            ArgumentRecords   =
            [
                new ArgumentRecordRow { JobName = "Contoso.DemoJob", Name = "beta",    Content = "{\"x\":2}" },
                new ArgumentRecordRow { JobName = "Contoso.DemoJob", Name = "alpha",   Content = "{\"x\":1}" }
            ]
        };

        var result = FakeSurfaceAgent.MapJobArguments(row, DevTarget);

        Assert.Multiple(() =>
        {
            Assert.That(result.JobName,           Is.EqualTo("Contoso.DemoJob"));
            Assert.That(result.TechnicalUserName, Is.EqualTo("svc_josyn"));
            Assert.That(result.Environment,       Is.EqualTo(RuntimeEnvironment.DEV));
            Assert.That(result.Machine,           Is.EqualTo("TEST-BOX"));
            Assert.That(result.Arguments.Count,   Is.EqualTo(2));
            // ordered by Name
            Assert.That(result.Arguments[0].Name, Is.EqualTo("alpha"));
            Assert.That(result.Arguments[1].Name, Is.EqualTo("beta"));
            Assert.That(result.Arguments[0].Content, Is.EqualTo("{\"x\":1}"));
        });
    }

    [Test]
    public void MapJobArguments_NoArguments_ReturnsEmptyList()
    {
        var row = new JobRegistrationRow { Name = "Empty.Job", TechnicalUserName = "svc_x" };

        var result = FakeSurfaceAgent.MapJobArguments(row, DevTarget);

        Assert.That(result.Arguments, Is.Empty);
    }

    // ── GetJobSchedule / MapJobSchedule ───────────────────────────────────────

    [Test]
    public void MapJobSchedule_ActiveScheduleWithEntries_MapsAllFieldsAndStampsTarget()
    {
        var row = new JobScheduleRow
        {
            JobName        = "Contoso.DemoJob",
            Suspended      = false,
            SuspendedUntil = null,
            Entries        =
            [
                new JobScheduleEntryRow
                {
                    JobName            = "Contoso.DemoJob",
                    ArgumentRecordName = "default",
                    ScheduleDefinition = "0 6 * * MON-FRI",
                    ToleranceMinutes   = 30
                }
            ]
        };

        var result = FakeSurfaceAgent.MapJobSchedule(row, DevTarget);

        Assert.Multiple(() =>
        {
            Assert.That(result.JobName,        Is.EqualTo("Contoso.DemoJob"));
            Assert.That(result.Suspended,      Is.False);
            Assert.That(result.SuspendedUntil, Is.Null);
            Assert.That(result.Environment,    Is.EqualTo(RuntimeEnvironment.DEV));
            Assert.That(result.Machine,        Is.EqualTo("TEST-BOX"));
            Assert.That(result.Entries.Count,  Is.EqualTo(1));

            var entry = result.Entries[0];
            Assert.That(entry.ArgumentRecordName, Is.EqualTo("default"));
            Assert.That(entry.ScheduleDefinition, Is.EqualTo("0 6 * * MON-FRI"));
            Assert.That(entry.ToleranceMinutes,   Is.EqualTo(30));
        });
    }

    [Test]
    public void MapJobSchedule_SuspendedWithEndDate_CopiesSuspendedUntil()
    {
        var until = new DateOnly(2026, 12, 31);
        var row = new JobScheduleRow
        {
            JobName        = "Contoso.DemoJob",
            Suspended      = true,
            SuspendedUntil = until
        };

        var result = FakeSurfaceAgent.MapJobSchedule(row, DevTarget);

        Assert.Multiple(() =>
        {
            Assert.That(result.Suspended,      Is.True);
            Assert.That(result.SuspendedUntil, Is.EqualTo(until));
        });
    }

    [Test]
    public void MapJobSchedule_EntriesAreOrderedByArgumentRecordName()
    {
        var row = new JobScheduleRow
        {
            JobName = "Contoso.DemoJob",
            Entries =
            [
                new JobScheduleEntryRow { ArgumentRecordName = "zulu",  ScheduleDefinition = "0 8 * * *" },
                new JobScheduleEntryRow { ArgumentRecordName = "alpha", ScheduleDefinition = "0 7 * * *" }
            ]
        };

        var result = FakeSurfaceAgent.MapJobSchedule(row, DevTarget);

        Assert.That(result.Entries[0].ArgumentRecordName, Is.EqualTo("alpha"));
        Assert.That(result.Entries[1].ArgumentRecordName, Is.EqualTo("zulu"));
    }

    [Test]
    public void MapJobSchedule_EntryWithNoTolerance_ToleranceMinutesIsNull()
    {
        var row = new JobScheduleRow
        {
            JobName = "Contoso.DemoJob",
            Entries = [new JobScheduleEntryRow { ArgumentRecordName = "default", ScheduleDefinition = "0 9 * * *" }]
        };

        var result = FakeSurfaceAgent.MapJobSchedule(row, DevTarget);

        Assert.That(result.Entries[0].ToleranceMinutes, Is.Null);
    }
}
