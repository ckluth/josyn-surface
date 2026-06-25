using JOSYN.Backend.Gateway;
using JOSYN.Jap.Contract;
using JOSYN.Jrp.Launch;
using JOSYN.Jrp.Surface;
using JOSYN.Jrp.Surface.Queries;
using JOSYN.Surface.Contracts;
using JOSYN.Surface.FakeAgent;

namespace JOSYN.Surface.Cli;

/// <summary>
/// The MVP-1 read-only CLI shell. Idiomatic above the <see cref="ISurfaceAgent"/> seam (ADR-030 D-20):
/// it parses arguments, builds a query, sends it through the seam, and renders the result. All data
/// access lives below the seam in the (throwaway) fake agent.
/// </summary>
internal static class SurfaceCli
{
    // MVP-1 is DEV-only (ADR-031 DS-4/DS-5). The connection string is the local dev default and may be
    // overridden for a differently-configured local box. It is NEVER an int/prod connection.
    private const string DevConnectionEnvVar = "JOSYN_SURFACE_DEV_CONNECTION";
    private const string BootstrapFileName   = "josyn.bootstrap.ini";
    private const string BootstrapKey        = "SessionStoreConnectionString";

    // Last-resort fallback for a bare repo run with neither the env var nor a deployed
    // josyn.bootstrap.ini present. In the deployed scenario the bootstrap.ini "sneak" wins.
    private const string DefaultDevConnection =
        "Server=(localdb)\\MSSQLLocalDB;Database=josyn-db-local;User Id=tu.josyn;Password=josyn;TrustServerCertificate=True;";

    public static async Task<int> Run(string[] args)
    {
        if (args.Length == 0)
            return Usage();

        var agent = new CompositeSurfaceAgent(
            new FakeSurfaceAgent(ResolveDevConnection()),
            new GatewayCommandHandler(ResolveDevConnection()));
        var target = new JrpTarget { Environment = RuntimeEnvironment.DEV, Machine = Environment.MachineName };

        return args[0] switch
        {
            "sessions"        => await RunSessions(agent, target, args),
            "error"           => await RunError(agent, target, args),
            "jobs"            => await RunJobs(agent, target),
            "arguments"       => await RunArguments(agent, target, args),
            "schedule"        => await RunSchedule(agent, target, args),
            "change-argument" => await RunChangeArgument(agent, target, args),
            _                 => Usage($"Unknown command: '{args[0]}'.")
        };

        // ── helpers ────────────────────────────────────────────────────────────
        static string ResolveDevConnection()
        {
            // 1. Explicit override always wins.
            var fromEnv = Environment.GetEnvironmentVariable(DevConnectionEnvVar);
            if (!string.IsNullOrWhiteSpace(fromEnv))
                return fromEnv;

            // 2. TEMPORARY DEV HACK (ADR-031 DS-4 spirit): the surface "sneaks" the connection
            //    string out of the backend's deployed josyn.bootstrap.ini. The surface can never
            //    legitimately live in the backend, so this read is a deliberate, scoped shortcut
            //    that disappears the moment the REST agent lands. It keeps the deployed surface in
            //    lockstep with the backend's single source of truth rather than a hardcoded copy.
            if (TryReadBootstrapConnection(out var fromBootstrap))
                return fromBootstrap;

            // 3. Last-resort fallback for a bare repo run with no deployment present.
            return DefaultDevConnection;
        }
    }

    // TEMPORARY DEV HACK: read SessionStoreConnectionString out of the backend's deployed
    // josyn.bootstrap.ini. Searches the surface's own folder and walks up the directory tree, so it
    // works whether the surface sits in C:\ProgramData\JOSYN\Surface\ or directly in the JOSYN root.
    private static bool TryReadBootstrapConnection(out string connectionString)
    {
        connectionString = string.Empty;

        var file = FindBootstrapFile(AppContext.BaseDirectory);
        if (file is null)
            return false;

        foreach (var raw in File.ReadLines(file))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith(';'))   // skip blanks and INI comments
                continue;

            var eq = line.IndexOf('=');
            if (eq <= 0)
                continue;

            if (!line[..eq].Trim().Equals(BootstrapKey, StringComparison.OrdinalIgnoreCase))
                continue;

            var value = line[(eq + 1)..].Trim();
            if (value.Length == 0)
                continue;

            connectionString = value;
            return true;
        }

        return false;

        // ── helpers ────────────────────────────────────────────────────────────
        static string? FindBootstrapFile(string startDirectory)
        {
            for (var dir = new DirectoryInfo(startDirectory); dir is not null; dir = dir.Parent)
            {
                var candidate = Path.Combine(dir.FullName, BootstrapFileName);
                if (File.Exists(candidate))
                    return candidate;
            }
            return null;
        }
    }

    private static async Task<int> RunSessions(ISurfaceAgent agent, JrpTarget target, string[] args)
    {
        if (!TryGetMax(args, out var max))
            return Usage("--max requires a positive integer.");

        var result = await agent.GetRecentSessions(new GetRecentSessions { Target = target, MaxCount = max });
        if (!result.Succeeded)
            return Fail(result.ErrorMessage);

        RenderSessions(result.Value);
        return 0;

        // ── helpers ────────────────────────────────────────────────────────────
        static bool TryGetMax(string[] args, out int max)
        {
            max = 20;
            var i = Array.IndexOf(args, "--max");
            if (i < 0) return true;
            return i + 1 < args.Length && int.TryParse(args[i + 1], out max) && max > 0;
        }
    }

    private static async Task<int> RunError(ISurfaceAgent agent, JrpTarget target, string[] args)
    {
        if (args.Length < 2 || !Guid.TryParse(args[1], out var uid))
            return Usage("error requires a valid GUID: error <error-guid>.");

        var result = await agent.GetErrorDetail(new GetErrorDetail { Target = target, ErrorUid = uid });
        if (!result.Succeeded)
            return Fail(result.ErrorMessage);

        RenderError(result.Value);
        return 0;
    }

    private static async Task<int> RunJobs(ISurfaceAgent agent, JrpTarget target)
    {
        var result = await agent.GetRegisteredJobs(new GetRegisteredJobs { Target = target });
        if (!result.Succeeded)
            return Fail(result.ErrorMessage);

        RenderJobs(result.Value);
        return 0;
    }

    private static async Task<int> RunArguments(ISurfaceAgent agent, JrpTarget target, string[] args)
    {
        if (args.Length < 2 || string.IsNullOrWhiteSpace(args[1]))
            return Usage("arguments requires a job name: arguments <job-name>.");

        var result = await agent.GetJobArguments(new GetJobArguments { Target = target, JobName = args[1] });
        if (!result.Succeeded)
            return Fail(result.ErrorMessage);

        RenderArguments(result.Value);
        return 0;
    }

    private static async Task<int> RunSchedule(ISurfaceAgent agent, JrpTarget target, string[] args)
    {
        if (args.Length < 2 || string.IsNullOrWhiteSpace(args[1]))
            return Usage("schedule requires a job name: schedule <job-name>.");

        var result = await agent.GetJobSchedule(new GetJobSchedule { Target = target, JobName = args[1] });
        if (!result.Succeeded)
            return Fail(result.ErrorMessage);

        RenderSchedule(result.Value);
        return 0;
    }

    private static async Task<int> RunChangeArgument(ISurfaceAgent agent, JrpTarget target, string[] args)
    {
        // change-argument <jobName> <argName> <content | @file>
        if (args.Length < 4)
            return Usage("change-argument requires: change-argument <job-name> <arg-name> <content|@file>.");

        var jobName  = args[1];
        var argName  = args[2];
        var content  = await ResolveContent(args[3]);

        var command = new ChangeJobArgument
        {
            Target        = target,
            Actor         = Environment.UserName,
            CorrelationId = Guid.NewGuid(),
            JobName       = jobName,
            ArgumentName  = argName,
            Content       = content
        };

        var result = await agent.ChangeJobArgument(command);
        if (!result.Succeeded)
            return Fail(result.ErrorMessage);

        RenderArgumentChange(result.Value);
        return 0;

        // ── helpers ────────────────────────────────────────────────────────────
        static async Task<string> ResolveContent(string arg)
        {
            // @path reads content from a file; useful for non-trivial JSON payloads.
            if (arg.StartsWith('@'))
            {
                var path = arg[1..];
                return await File.ReadAllTextAsync(path);
            }
            return arg;
        }
    }

    // ── rendering ──────────────────────────────────────────────────────────────

    private static void RenderSessions(IReadOnlyList<SessionSummary> sessions)
    {
        if (sessions.Count == 0)
        {
            Console.WriteLine("No sessions found.");
            return;
        }

        Console.WriteLine($"{"Started",-20}  {"Status",-30}  {"Job",-32}  {"User",-16}  UID");
        Console.WriteLine(new string('-', 130));
        foreach (var s in sessions)
        {
            Console.WriteLine(
                $"{s.Started,-20:yyyy-MM-dd HH:mm:ss}  {s.ExecutionStatus,-30}  {Truncate(s.JobTypeName, 32),-32}  {Truncate(s.UserName, 16),-16}  {s.Uid}");
        }
        Console.WriteLine();
        Console.WriteLine($"{sessions.Count} session(s).");
    }

    private static void RenderError(ErrorDetail e)
    {
        Console.WriteLine($"Error    : {e.Uid}");
        Console.WriteLine($"Occurred : {e.OccurredAt:yyyy-MM-dd HH:mm:ss zzz}");
        Console.WriteLine($"Causer   : {e.Causer}");
        Console.WriteLine($"Job      : {e.JobName ?? "(system error)"}");
        Console.WriteLine($"Session  : {(e.SessionGuid is { } g ? g.ToString() : "(none)")}");
        Console.WriteLine($"Env/Mach : {e.Environment} / {e.Machine}");
        Console.WriteLine();
        Console.WriteLine("Message:");
        Console.WriteLine(e.Message);
        if (e.CallStack is not null)
        {
            Console.WriteLine();
            Console.WriteLine("Call stack:");
            Console.WriteLine(e.CallStack);
        }
        if (e.ExceptionDetails is not null)
        {
            Console.WriteLine();
            Console.WriteLine("Exception details:");
            Console.WriteLine(e.ExceptionDetails);
        }
    }

    private static void RenderJobs(IReadOnlyList<RegisteredJobSummary> jobs)
    {
        if (jobs.Count == 0)
        {
            Console.WriteLine("No jobs registered.");
            return;
        }

        Console.WriteLine($"{"Job name",-48}  {"Technical user",-24}  Args");
        Console.WriteLine(new string('-', 82));
        foreach (var j in jobs)
        {
            Console.WriteLine(
                $"{Truncate(j.JobName, 48),-48}  {Truncate(j.TechnicalUserName, 24),-24}  {j.ArgumentCount}");
        }
        Console.WriteLine();
        Console.WriteLine($"{jobs.Count} registered job(s).");
    }

    private static void RenderArguments(JobArguments job)
    {
        Console.WriteLine($"Job           : {job.JobName}");
        Console.WriteLine($"Technical user: {job.TechnicalUserName}");
        Console.WriteLine($"Env / Machine : {job.Environment} / {job.Machine}");
        Console.WriteLine($"Arguments     : {job.Arguments.Count}");

        if (job.Arguments.Count == 0)
        {
            Console.WriteLine("(no argument records)");
            return;
        }

        Console.WriteLine();
        foreach (var a in job.Arguments)
        {
            Console.WriteLine($"── {a.Name}");
            Console.WriteLine(a.Content);
            Console.WriteLine();
        }
    }

    private static void RenderSchedule(JobSchedule schedule)
    {
        Console.WriteLine($"Job           : {schedule.JobName}");
        Console.WriteLine($"Env / Machine : {schedule.Environment} / {schedule.Machine}");

        if (schedule.Suspended)
        {
            var until = schedule.SuspendedUntil is { } d ? $"until {d:yyyy-MM-dd}" : "indefinitely";
            Console.WriteLine($"Suspended     : yes ({until})");
        }
        else
        {
            Console.WriteLine("Suspended     : no");
        }

        Console.WriteLine($"Entries       : {schedule.Entries.Count}");

        if (schedule.Entries.Count == 0)
        {
            Console.WriteLine("(no schedule entries)");
            return;
        }

        Console.WriteLine();
        Console.WriteLine($"{"Argument record",-32}  {"Schedule definition",-28}  Tolerance");
        Console.WriteLine(new string('-', 74));
        foreach (var e in schedule.Entries)
        {
            var tolerance = e.ToleranceMinutes is { } t ? $"{t} min" : "-";
            Console.WriteLine(
                $"{Truncate(e.ArgumentRecordName, 32),-32}  {Truncate(e.ScheduleDefinition, 28),-28}  {tolerance}");
        }
    }

    private static void RenderArgumentChange(ArgumentChangeOutcome outcome)
    {
        Console.WriteLine($"Changed   : {outcome.ArgumentName}  (job: {outcome.JobName})");
        Console.WriteLine();
        Console.WriteLine("── Before ──────────────────────────────────────────────────────");
        Console.WriteLine(outcome.Before);
        Console.WriteLine();
        Console.WriteLine("── After ───────────────────────────────────────────────────────");
        Console.WriteLine(outcome.After);
    }

    private static string Truncate(string value, int max)
        => value.Length <= max ? value : value[..(max - 1)] + "\u2026";

    // ── exits ────────────────────────────────────────────────────────────────────

    private static int Fail(string? message)
    {
        Console.Error.WriteLine($"[ERROR] {message}");
        return 1;
    }

    private static int Usage(string? problem = null)
    {
        if (problem is not null)
            Console.Error.WriteLine($"[ERROR] {problem}");

        Console.Error.WriteLine("Usage:");
        Console.Error.WriteLine("  sessions [--max N]                               List the N most recent sessions (default 20).");
        Console.Error.WriteLine("  error <error-guid>                               Show the full detail of one error.");
        Console.Error.WriteLine("  jobs                                             List all registered jobs.");
        Console.Error.WriteLine("  arguments <job-name>                             Show argument records for a job.");
        Console.Error.WriteLine("  schedule <job-name>                              Show the schedule for a job.");
        Console.Error.WriteLine("  change-argument <job-name> <arg-name> <content>  Change an existing argument record.");
        Console.Error.WriteLine("                                                   Use @<path> to read content from a file.");
        return problem is null ? 0 : 2;
    }
}
