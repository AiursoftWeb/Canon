namespace Aiursoft.Canon.BackgroundJobs;

public class RegisteredJob
{
    public required Type JobType { get; init; }

    public string? Name { get; init; }

    public string? Description { get; init; }
}
