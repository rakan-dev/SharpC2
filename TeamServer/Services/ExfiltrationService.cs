using TeamServer.Handlers;
using TeamServer.Interfaces;
using TeamServer.Messages;
using TeamServer.Storage;

namespace TeamServer.Services;

public sealed class ExfiltrationService : IExfiltrationService
{
    private readonly IDatabaseService _db;

    public ExfiltrationService(IDatabaseService db)
    {
        _db = db;
    }

  
    public async Task AddExfiltration(ExfilrateMetadata metadata)
    {
        var conn = _db.GetAsyncConnection();
        await conn.InsertAsync((ExfiltrationDao)metadata);
    }

    public async Task<IEnumerable<ExfilrateMetadata>> GetExfilrates()
    {
        var conn = _db.GetAsyncConnection();
        var files = await conn.Table<ExfiltrationDao>().ToArrayAsync();

        return files.Select(f => (ExfilrateMetadata)f);
    }

    public async Task<ExfilrateMetadata> GetExfilrate(string taskId)
    {
        var conn = _db.GetAsyncConnection();
        return await conn.Table<ExfiltrationDao>().FirstOrDefaultAsync(h => h.TaskId.Equals(taskId));
    }

    public async Task AddFragment(FragmentMetadata metadata)
    {
        var conn = _db.GetAsyncConnection();
        await conn.InsertAsync((FragmentDao)metadata);
    }

    public async Task<IEnumerable<FragmentMetadata>> GetAllFragments(string taskId)
    {
        var conn = _db.GetAsyncConnection();
        var files = await conn.Table<FragmentDao>().ToArrayAsync();

        return files.Select(f => (FragmentMetadata)f);
    }
}