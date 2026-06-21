using JOSYN.Foundation.ResultPattern;

namespace JOSYN.Surface.Contracts;

/// <summary>
/// Produces categorised <see cref="Error"/> values for the surface seam.
/// </summary>
/// <remarks>
/// The platform <see cref="Result"/> carries a single string message, not a typed category. To keep
/// the named taxonomy (ADR-031 DS-2) without fighting the Result pattern, the category is encoded as
/// a <c>[Category]</c> prefix on the message. <see cref="CategoryOf"/> reads it back — the seam point
/// the future <c>HttpAgent</c> uses to map categories onto HTTP status codes. The encoding is an
/// implementation detail of the contract layer, never a wire shape.
/// </remarks>
public static class SurfaceError
{
    /// <summary>The requested resource does not exist.</summary>
    public static Error NotFound(string detail) => Make(SurfaceErrorCategory.NotFound, detail);

    /// <summary>The request was malformed or violated a precondition.</summary>
    public static Error Invalid(string detail) => Make(SurfaceErrorCategory.Invalid, detail);

    /// <summary>An unexpected internal failure occurred while serving the request.</summary>
    public static Error Internal(string detail, Exception? exception = null)
        => Make(SurfaceErrorCategory.Internal, detail, exception);

    /// <summary>
    /// Recovers the <see cref="SurfaceErrorCategory"/> from a failed result's message.
    /// Falls back to <see cref="SurfaceErrorCategory.Internal"/> when no known prefix is present.
    /// </summary>
    public static SurfaceErrorCategory CategoryOf(string? errorMessage)
    {
        if (string.IsNullOrEmpty(errorMessage) || errorMessage[0] != '[')
            return SurfaceErrorCategory.Internal;

        var end = errorMessage.IndexOf(']');
        if (end <= 1)
            return SurfaceErrorCategory.Internal;

        var token = errorMessage[1..end];
        return Enum.TryParse<SurfaceErrorCategory>(token, out var category)
            ? category
            : SurfaceErrorCategory.Internal;
    }

    private static Error Make(SurfaceErrorCategory category, string detail, Exception? exception = null)
        => Result.Error($"[{category}] {detail}", exception);
}
