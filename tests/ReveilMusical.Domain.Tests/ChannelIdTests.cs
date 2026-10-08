using ReveilMusical.Domain;

namespace ReveilMusical.Domain.Tests;

public class ChannelIdTests
{
    [Fact]
    public void Constructor_EmptyValue_Throws()
    {
        Should.Throw<ArgumentException>(() => new ChannelId(""));
    }

    [Theory]
    [InlineData("EMAIL", "email")]
    [InlineData("  Sms  ", "sms")]
    [InlineData("push", "push")]
    public void Constructor_Normalizes_ToLowerInvariantTrimmed(string input, string expected)
    {
        var channelId = new ChannelId(input);

        channelId.Value.ShouldBe(expected);
    }

    [Fact]
    public void Equals_DifferentCasing_AreEqual()
    {
        var first = new ChannelId("Email");
        var second = new ChannelId("email");

        first.ShouldBe(second);
    }
}
