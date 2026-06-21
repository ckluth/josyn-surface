using JOSYN.Surface.Contracts;
using JOSYN.Surface.FakeAgent;
using JOSYN.Jap.Contract;

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
    private const string DefaultDevConnection =
        "Server=localhost\\SQLEXPRESS01;Database=josyn-db-local;User Id=tu.josyn;Password=josyn;TrustServerCertificate=True;";

    public static async Task<int> Run(string[] args)
    {
        if (args.Length == 0)
            return Usage();

        var agent = new FakeSurfaceAgent(ResolveDevConnection());
        var target = new SurfaceTarget { Environment = RuntimeEnvironment.DEV, Machine = Environment.MachineName };

        return args[0] switch
        {
            "sessions" => await RunSessions(agent, target, args),
            "error"    => await RunError(agent, target, args),
            _          => Usage($"Unknown command: '{args[0]}'.")
        };

        // ── helpers ────────────────────────────────────────────────────────────
        static string ResolveDevConnection()
        {
            var fromEnv = Environment.GetEnvironmentVariable(DevConnectionEnvVar);
            return string.IsNullOrWhiteSpace(fromEnv) ? DefaultDevConnection : fromEnv;
        }
    }

    private static async Task<int> RunSessions(ISurfaceAgent agent, SurfaceTarget target, string[] args)
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

    private static async Task<int> RunError(ISurfaceAgent agent, SurfaceTarget target, string[] args)
    {
        if (args.Length < 2 || !Guid.TryParse(args[1], out var uid))
            return Usage("error requires a valid GUID: error <error-guid>.");

        var result = await agent.GetErrorDetail(new GetErrorDetail { Target = target, ErrorUid = uid });
        if (!result.Succeeded)
            return Fail(result.ErrorMessage);

        RenderError(result.Value);
        return 0;
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
        Console.Error.WriteLine("  sessions [--max N]   List the N most recent sessions (default 20).");
        Console.Error.WriteLine("  error <error-guid>   Show the full detail of one error.");
        return problem is null ? 0 : 2;
    }
}
