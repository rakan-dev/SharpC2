using SQLite;

using TeamServer.Events;
using TeamServer.Messages;
using TeamServer.Webhooks;

namespace TeamServer.Storage;

[Table("fragments")]
public sealed class FragmentDao
{
    [PrimaryKey,Column("id"),AutoIncrement]
    public int Id { get; set; }
    [Column("task_id")]
    public string TaskId { get; set; }

    [Column("start_offset")]
    public long StartOffset { get; set; }
    [Column("end_offset")]
    public long EndOffset { get; set; }
    [Column("length")]
    public long Length { get; set; }

    [Column("data")]
    public byte[] Data { get; set; }

    public static implicit operator FragmentDao(FragmentMetadata metadata)
    {
        var parts = metadata.ContentRange.Split('/');
        long startOffset = 0;
        long endOffset = 0;
        long length = 0;
        if (parts.Length == 2)
        {
            var rangeInfo = parts[0].Replace("bytes ", "").Split('-');
            if (rangeInfo.Length == 2 && long.TryParse(rangeInfo[0], out var rangeStart) && long.TryParse(rangeInfo[1], out var rangeEnd))
            {
                length = int.Parse(parts[1]);
                startOffset = rangeStart;
                endOffset = rangeEnd;
            }
        }
        return new FragmentDao
        {
          TaskId = metadata.TaskId,
          StartOffset = startOffset,   
          EndOffset = endOffset,
          Length = length,
          Data = metadata.Content
        };
    }

    public static implicit operator FragmentMetadata(FragmentDao dao)
    {
        return dao is null
            ? null
            : new FragmentMetadata
            {
                TaskId = dao.TaskId,
                ContentRange = $"bytes {dao.StartOffset}-{dao.EndOffset}/{dao.Length}",
                Content = dao.Data
            };
    }
}