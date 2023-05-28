using TeamServer.Drones;
using TeamServer.Messages;

namespace TeamServer.Modules;

public sealed class FragmentsModule : ServerModule
{
    public override FrameType FrameType => FrameType.EXFILTRATION_FRAGMENT;
    
    public override async Task ProcessFrame(C2Frame frame)
    {
        var metadata = await Crypto.Decrypt<FragmentMetadata>(frame.Data);
        // Store the fragment data in the database
        await ExfiltrationService.AddFragment(metadata);
        await Hub.Clients.All.ExfiltratePrograss(metadata.TaskId,GetFragmentIndex(metadata),GetTotalFragments(metadata));
        // Check if the last fragment has been received
        if (IsLastFragment(metadata.ContentRange))
        {
            // Reassemble the fragmented request
            var fragments = await ExfiltrationService.GetAllFragments(metadata.TaskId);
            var reassembledfragments = ReassembleFragments(fragments.ToArray());
            var record = await Tasks.Get(metadata.TaskId);
            record.Update(new TaskOutput()
            {
                TaskId = metadata.TaskId,
                Status = Messages.TaskStatus.RUNNING,
                Output = reassembledfragments,
            });
            await Tasks.Update(record);
            await Hub.Clients.All.ExfiltrateFinished(metadata.TaskId);
        }
    }
    private int GetTotalFragments(FragmentMetadata metadata)
    {
        var contentRange = metadata.ContentRange;
        var parts = contentRange.Split('/');
        if (parts.Length == 2 && int.TryParse(parts[1], out var totalLength))
        {
            return totalLength;
        }

        return -1;
    }
    private int GetFragmentIndex(FragmentMetadata metadata)
    {
        var contentRange = metadata.ContentRange;
        var rangeInfo = contentRange.Replace("bytes ", "").Split('-');
        if (rangeInfo.Length == 2 && int.TryParse(rangeInfo[0], out var rangeStart))
        {
            return rangeStart;
        }

        return -1;
    }
    private byte[] ReassembleFragments(FragmentMetadata[] fragments)
    {
        int totalLength = 0;
        foreach (FragmentMetadata fragment in fragments)
        {
            totalLength += fragment.Content.Length;
        }

        byte[] requestData = new byte[totalLength];
        int offset = 0;

        foreach (FragmentMetadata fragment in fragments)
        {
            Array.Copy(fragment.Content, 0, requestData, offset, fragment.Content.Length);
            offset += fragment.Content.Length;
        }

        return requestData;
    }
    private bool IsLastFragment(string range)
    {
        var contentRange = range;
        if (!string.IsNullOrEmpty(contentRange))
        {
            var parts = contentRange.Split('/');
            if (parts.Length == 2)
            {
                var rangeInfo = parts[0].Replace("bytes ", "").Split('-');
                if (rangeInfo.Length == 2 && long.TryParse(rangeInfo[1], out var rangeEnd))
                {
                    var totalLength = long.Parse(parts[1]);
                    return rangeEnd == totalLength - 1;
                }
            }
        }

        return false;
    }
}