using ProtoBuf;

using TeamServer.Tasks;

namespace TeamServer.Messages;

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
