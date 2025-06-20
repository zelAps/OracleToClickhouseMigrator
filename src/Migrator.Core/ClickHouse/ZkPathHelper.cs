namespace Migrator.Core.ClickHouse;

/// <summary>
/// Формирует корректный ZooKeeper-путь ReplicatedMergeTree.
/// </summary>
public static class ZkPathHelper
{
    public static string Build(string zkPrefix, string database, string table)
     => $"{zkPrefix.TrimEnd('/')}/{{shard}}/{database}/{table}";
}
