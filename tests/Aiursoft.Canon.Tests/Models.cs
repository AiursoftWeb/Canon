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

internal class DemoService(SqlDbContext dbContext)
{
    public static bool Done;
    public static bool DoneAsync;
    public static int DoneTimes;
    private static readonly object Loc = new();

    public void DoSomethingSlow()
    {
        Done = false;
        Thread.Sleep(200);
        var _ = dbContext.Records.ToList();
        Done = true;
    }

    public async Task DoSomethingSlowAsync()
    {
        DoneAsync = false;
        await Task.Delay(200);
        dbContext.Records.Add(new InDbEntity { Content = "Test", Filter = "Test" });
        await dbContext.SaveChangesAsync();
        DoneAsync = true;
        lock (Loc)
        {
            DoneTimes++;
        }
    }
}

public class DemoController(
    CanonService canonService,
    CanonQueue canonQueue)
{
    public void DemoAction()
    {
        canonService.Fire<DemoService>(d => d.DoSomethingSlow());
    }

    public void DemoActionAsync()
    {
        canonService.FireAsync<DemoService>(d => d.DoSomethingSlowAsync());
    }

    public void QueueActionAsync()
    {
        for (var i = 0; i < 32; i++)
        {
            canonQueue.QueueWithDependency<DemoService>(d => d.DoSomethingSlowAsync());
        }
    }
}