using TeamServer.Handlers;
using TeamServer.Messages;

namespace TeamServer.Interfaces;

public interface IExfiltrationService
{
    Task AddExfiltration(ExfilrateMetadata metadata);
    Task<IEnumerable<ExfilrateMetadata>> GetExfilrates();
    Task<ExfilrateMetadata> GetExfilrate(string taskId);



    Task AddFragment(FragmentMetadata metadata);
    Task<IEnumerable<FragmentMetadata>> GetAllFragments(string taskId);
}