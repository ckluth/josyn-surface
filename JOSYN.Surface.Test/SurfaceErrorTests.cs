using JOSYN.Surface.Contracts;
using NUnit.Framework;

namespace JOSYN.Surface.Test;

/// <summary>
/// Tests the error-category encoding that lets the named taxonomy (ADR-031 DS-2) ride on the
/// platform Result's single message field, and be recovered later for HTTP status mapping.
/// </summary>
[TestFixture]
internal sealed class SurfaceErrorTests
{
    [Test]
    public void CategoryOf_RoundTripsEveryCategory()
    {
        Assert.Multiple(() =>
        {
            Assert.That(SurfaceError.CategoryOf(SurfaceError.NotFound("x").ErrorMessage),
                Is.EqualTo(SurfaceErrorCategory.NotFound));
            Assert.That(SurfaceError.CategoryOf(SurfaceError.Invalid("x").ErrorMessage),
                Is.EqualTo(SurfaceErrorCategory.Invalid));
            Assert.That(SurfaceError.CategoryOf(SurfaceError.Internal("x").ErrorMessage),
                Is.EqualTo(SurfaceErrorCategory.Internal));
        });
    }

    [Test]
    public void CategoryOf_UnprefixedMessage_FallsBackToInternal()
    {
        Assert.That(SurfaceError.CategoryOf("just a bare message"),
            Is.EqualTo(SurfaceErrorCategory.Internal));
    }

    [Test]
    public void CategoryOf_NullOrEmpty_FallsBackToInternal()
    {
        Assert.Multiple(() =>
        {
            Assert.That(SurfaceError.CategoryOf(null), Is.EqualTo(SurfaceErrorCategory.Internal));
            Assert.That(SurfaceError.CategoryOf(string.Empty), Is.EqualTo(SurfaceErrorCategory.Internal));
        });
    }
}
