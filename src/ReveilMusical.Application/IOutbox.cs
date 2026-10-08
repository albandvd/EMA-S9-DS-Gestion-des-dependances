namespace ReveilMusical.Application;

public interface IOutbox
{
    Task WriteAsync(string channel, string content, CancellationToken cancellationToken);
}
