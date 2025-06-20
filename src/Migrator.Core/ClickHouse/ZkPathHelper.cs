namespace Migrator.Core.ClickHouse;

/// <summary>
/// Формируем корректный ZooKeeper-путь ReplicatedMergeTree.
/// </summary>
public static class ZkPathHelper
{
    /// <summary>
    /// Собирает путь к таблице в ZooKeeper для ReplicatedMergeTree.
    /// </summary>
    public static string Build(string zkPrefix, string database, string table)
        => $"{zkPrefix.TrimEnd('/')}/{{shard}}/{database}/{table}";
}
