using SQLite;

using TeamServer.Events;
using TeamServer.Messages;
using TeamServer.Webhooks;

namespace TeamServer.Storage;

[Table("exfiltration")]
public sealed class ExfiltrationDao
{

    [PrimaryKey,Column("task_id")]
    public string TaskId { get; set; }

    [Column("full_path")]
    public string FullPath { get; set; }

    [Column("size")]
    public long Size { get; set; }

    public static implicit operator ExfiltrationDao(ExfilrateMetadata metadata)
    {
        return new ExfiltrationDao
        {
           TaskId = metadata.TaskId,
            FullPath = metadata.FileFullPath,
            Size = metadata.Size
        };
    }

    public static implicit operator ExfilrateMetadata(ExfiltrationDao dao)
    {
        return dao is null
            ? null
            : new ExfilrateMetadata
            {
                TaskId = dao.TaskId,
                FileFullPath = dao.FullPath,
                Size = dao.Size
            };
    }
}