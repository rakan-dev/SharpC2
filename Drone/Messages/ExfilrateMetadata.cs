using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Principal;
using System.Threading.Tasks;

using Drone.Utilities;

using ProtoBuf;

namespace Drone.Messages;

[ProtoContract]
public sealed class ExfilrateMetadata
{
    [ProtoMember(1)]
    public string TaskId { get; set; }
    
    [ProtoMember(2)]
    public string FileFullPath { get; set; }
    
    [ProtoMember(3)]
    public long Size { get; set; }

}

