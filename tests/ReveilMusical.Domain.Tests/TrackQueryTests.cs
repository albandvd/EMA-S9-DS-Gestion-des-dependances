using ReveilMusical.Domain;

namespace ReveilMusical.Domain.Tests;

public class TrackQueryTests
{
    [Fact]
    public void Constructor_EmptySearchText_Throws()
    {
        Should.Throw<ArgumentException>(() => new TrackQuery(""));
    }

    [Fact]
    public void Equals_SameSearchText_AreEqual()
    {
        var first = new TrackQuery("Here Comes the Sun");
        var second = new TrackQuery("Here Comes the Sun");

        first.ShouldBe(second);
    }
}
