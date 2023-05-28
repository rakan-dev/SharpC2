using ProtoBuf;

using TeamServer.Tasks;

namespace TeamServer.Messages;

[ProtoContract]
public sealed class FragmentMetadata
{
    [ProtoMember(1)]
    public string TaskId { get; set; }

    [ProtoMember(2)]
    public string ContentRange { get; set; }

    [ProtoMember(3)]
    public byte[] Content { get; set; }

}
