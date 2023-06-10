using Microsoft.EntityFrameworkCore;

namespace Aiursoft.Canon.Tests;
public class InDbEntity
{
    public int Id { get; set; }
    public string? Content { get; set; }
    public string? Filter { get; set; }
}

public class SqlDbContext : DbContext
{
    public DbSet<InDbEntity> Records => Set<InDbEntity>();

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options.UseSqlite("Data Source=test.db");
    }
}

internal class DemoService
{
    public static bool Done;
    public static bool DoneAsync;
    public static int DoneTimes;
    private static readonly object Loc = new();
    private readonly SqlDbContext _dbContext;

    public DemoService(SqlDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public void DoSomethingSlow()
    {
        Done = false;
        Thread.Sleep(200);
        var _ = _dbContext.Records.ToList();
        Done = true;
    }

    public async Task DoSomethingSlowAsync()
    {
        DoneAsync = false;
        await Task.Delay(200);
        _dbContext.Records.Add(new InDbEntity { Content = "Test", Filter = "Test" });
        await _dbContext.SaveChangesAsync();
        DoneAsync = true;
        lock (Loc)
        {
            DoneTimes++;
        }
    }
}

public class DemoController
{
    private readonly CanonQueue _canonQueue;
    private readonly CanonService _canonService;

    public DemoController(
        CanonService canonService,
        CanonQueue canonQueue)
    {
        _canonService = canonService;
        _canonQueue = canonQueue;
    }

    public void DemoAction()
    {
        _canonService.Fire<DemoService>(d => d.DoSomethingSlow());
    }

    public void DemoActionAsync()
    {
        _canonService.FireAsync<DemoService>(d => d.DoSomethingSlowAsync());
    }

    public void QueueActionAsync()
    {
        for (var i = 0; i < 32; i++)
        {
            _canonQueue.QueueWithDependency<DemoService>(d => d.DoSomethingSlowAsync());
        }
    }
}