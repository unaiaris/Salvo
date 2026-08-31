namespace Salvo.Infrastructure.Persistence;

public sealed class FoundationCheckpoint
{
    private FoundationCheckpoint()
    {
    }

    public FoundationCheckpoint(string name)
    {
        Name = name;
    }

    public int Id { get; private set; }

    public string Name { get; private set; } = string.Empty;
}
