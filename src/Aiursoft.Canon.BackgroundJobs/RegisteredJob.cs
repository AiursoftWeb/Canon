namespace Aiursoft.Canon.BackgroundJobs;

/// <summary>
/// Descriptor for a background job that has been registered via
/// <see cref="BackgroundJobRegistryExtensions.RegisterBackgroundJob{TJob}"/>.
/// Returned by <see cref="BackgroundJobRegistry.GetAll"/> and used as the
/// handle for scheduling.
/// </summary>
public class RegisteredJob
{
    /// <summary>The concrete <see cref="IBackgroundJob"/> implementation type.</summary>
    public required Type JobType { get; init; }

    /// <summary>
    /// Human-readable name. Populated lazily by the registry when queried;
    /// <see langword="null"/> when built directly (e.g. inside
    /// <c>RegisterBackgroundJob</c> before the first DI resolution).
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Short description. Populated lazily like <see cref="Name"/>.
    /// </summary>
    public string? Description { get; init; }
}
