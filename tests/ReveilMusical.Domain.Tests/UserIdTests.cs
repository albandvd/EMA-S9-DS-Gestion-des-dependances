using ReveilMusical.Domain;

namespace ReveilMusical.Domain.Tests;

public class UserIdTests
{
    [Fact]
    public void Constructor_EmptyValue_Throws()
    {
        Should.Throw<ArgumentException>(() => new UserId(""));
    }

    [Fact]
    public void Constructor_WhitespaceValue_Throws()
    {
        Should.Throw<ArgumentException>(() => new UserId("   "));
    }

    [Fact]
    public void Equals_SameValue_AreEqual()
    {
        var first = new UserId("u-42");
        var second = new UserId("u-42");

        first.ShouldBe(second);
    }

    [Fact]
    public void Equals_DifferentValue_AreNotEqual()
    {
        var first = new UserId("u-42");
        var second = new UserId("u-43");

        first.ShouldNotBe(second);
    }
}
