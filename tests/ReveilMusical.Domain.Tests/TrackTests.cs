using ReveilMusical.Domain;

namespace ReveilMusical.Domain.Tests;

public class TrackTests
{
    [Fact]
    public void Constructor_EmptyTitle_Throws()
    {
        Should.Throw<ArgumentException>(() => new Track("", "The Doors"));
    }

    [Fact]
    public void Constructor_EmptyArtist_Throws()
    {
        Should.Throw<ArgumentException>(() => new Track("Riders on the Storm", ""));
    }

    [Fact]
    public void Equals_SameTitleAndArtist_AreEqual()
    {
        var first = new Track("Riders on the Storm", "The Doors");
        var second = new Track("Riders on the Storm", "The Doors");

        first.ShouldBe(second);
    }
}
