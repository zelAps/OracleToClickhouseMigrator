using System;
using System.Collections.Generic;

namespace Migrator.Core.Models;

/// <summary>
/// Модель таблицы после чтения из Oracle и применения конфигурации.
/// </summary>
public sealed class TableDef
{
    public required string Source { get; init; }   
    public required string Target { get; init; }   
    public string? Owner { get; init; }

    public required IReadOnlyList<ColumnDef> Columns { get; init; }

    public IReadOnlyList<string> PrimaryKey { get; init; } = [];
    public string? ShardKey { get; init; }
    public string PartitionExpr { get; init; } = "toYYYYMM(toDate(1))";  

    public override string ToString() => $"{Source} -> {Target} ({Columns.Count} cols)";
}
