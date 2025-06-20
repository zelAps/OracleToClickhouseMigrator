using Migrator.Core.Config;
using Migrator.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Migrator.Core.ClickHouse;

/// <summary>
/// Генерирует DDL-скрипты для локальных и распределённых таблиц.
/// </summary>
public sealed class ClickHouseDdlBuilder(MigratorConfig cfg)
{
    private readonly MigratorConfig _cfg = cfg;

    /* ---------- публичный API ---------- */

    public string BuildLocal(TableDef t)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"CREATE TABLE {_cfg.ClickHouse.Database}.{t.Target}_local");
        sb.AppendLine($"    ON CLUSTER {_cfg.ClickHouse.Cluster}");
        sb.AppendLine("(");
        sb.AppendLine(string.Join(",\n", t.Columns.Select(RenderColumn)));
        sb.AppendLine(")");
        sb.AppendLine("ENGINE = ReplicatedMergeTree(");
        sb.Append("    '");
        sb.Append(ZkPathHelper.Build(
            _cfg.ClickHouse.ZkPathPrefix,
            _cfg.ClickHouse.Database,
            t.Target));
        sb.AppendLine("', '{replica}')");
        sb.AppendLine($"PARTITION BY {t.PartitionExpr}");
        sb.AppendLine($"ORDER BY ({string.Join(", ", BuildOrderBy(t))})");
        sb.AppendLine("SETTINGS allow_nullable_key = 1;");

        return sb.ToString();
    }

    public string BuildDistributed(TableDef t)
    {
        var shardKey = t.ShardKey
                       ?? _cfg.ClickHouse.DistributedShardKey
                       ?? t.PrimaryKey.FirstOrDefault()
                       ?? "rand()";

        return $"""
        CREATE TABLE {_cfg.ClickHouse.Database}.{t.Target}
        ON CLUSTER {_cfg.ClickHouse.Cluster}
        AS {_cfg.ClickHouse.Database}.{t.Target}_local
        ENGINE = Distributed(
            '{_cfg.ClickHouse.Cluster}',
            '{_cfg.ClickHouse.Database}',
            '{t.Target}_local',
            {shardKey});
        """;
    }

    /* ---------- helpers ---------- */

    private static string RenderColumn(ColumnDef c)
        => $"    `{c.TargetName}` {c.ClickHouseType}";

    private static IEnumerable<string> BuildOrderBy(TableDef t)
        // сначала шардинг-ключ, затем PK (если есть)
        => (t.ShardKey is not null
                ? new[] { t.ShardKey }
                : Array.Empty<string>())
           .Concat(t.PrimaryKey);

    /* ---------- пакетный билд ---------- */

    public string BuildAll(TableDef t)
        => $"{BuildLocal(t)}\n\n{BuildDistributed(t)}";
}
