namespace Aiursoft.Canon.BackgroundJobs;

public interface IBackgroundJob
{
    string Name { get; }

    string Description { get; }

    Task ExecuteAsync();
}
