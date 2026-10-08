namespace ReveilMusical.Application;

public interface IOutbox
{
    void Write(string channel, string content);
}
