using TeamServer.Drones;
using TeamServer.Messages;

namespace TeamServer.Modules;

public sealed class ExfiltrationModule : ServerModule
{
    public override FrameType FrameType => FrameType.EXFILTRATION_METADATA;
    
    public override async Task ProcessFrame(C2Frame frame)
    {
        var metadata = await Crypto.Decrypt<ExfilrateMetadata>(frame.Data);
        await ExfiltrationService.AddExfiltration(metadata);
        await Hub.Clients.All.ExfiltrateStarted(metadata.TaskId);
        
    }
}