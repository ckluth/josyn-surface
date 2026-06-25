using JOSYN.Jrp.Launch;
using JOSYN.Jrp.Surface;
using NUnit.Framework;

namespace JOSYN.Surface.Test;

/// <summary>
/// Tests the error-category encoding that lets the named taxonomy (ADR-031 DS-2) ride on the
/// platform Result's single message field, and be recovered later for HTTP status mapping.
/// </summary>
[TestFixture]
internal sealed class JrpErrorTests
{
    [Test]
    public void CategoryOf_RoundTripsEveryCategory()
    {
        Assert.Multiple(() =>
        {
            Assert.That(JrpError.CategoryOf(JrpError.NotFound("x").ErrorMessage),
                Is.EqualTo(JrpErrorCategory.NotFound));
            Assert.That(JrpError.CategoryOf(JrpError.Invalid("x").ErrorMessage),
                Is.EqualTo(JrpErrorCategory.Invalid));
            Assert.That(JrpError.CategoryOf(JrpError.Internal("x").ErrorMessage),
                Is.EqualTo(JrpErrorCategory.Internal));
        });
    }

    [Test]
    public void CategoryOf_UnprefixedMessage_FallsBackToInternal()
    {
        Assert.That(JrpError.CategoryOf("just a bare message"),
            Is.EqualTo(JrpErrorCategory.Internal));
    }

    [Test]
    public void CategoryOf_NullOrEmpty_FallsBackToInternal()
    {
        Assert.Multiple(() =>
        {
            Assert.That(JrpError.CategoryOf(null), Is.EqualTo(JrpErrorCategory.Internal));
            Assert.That(JrpError.CategoryOf(string.Empty), Is.EqualTo(JrpErrorCategory.Internal));
        });
    }
}
