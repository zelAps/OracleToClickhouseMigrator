using System;
using System.Collections.Generic;

namespace Migrator.Core.Models;

/// <summary>
/// Модель таблицы после чтения из Oracle и применения конфигурации.
/// </summary>
public sealed class TableDef
{
    public required string Source { get; init; }   // EMPLOYEES
    public required string Target { get; init; }   // EMPLOYEES_NEW
    public string? Owner { get; init; }

    public required IReadOnlyList<ColumnDef> Columns { get; init; }

    /* Первичный ключ, шардинг или порядок сортировки   */
    public IReadOnlyList<string> PrimaryKey { get; init; } = [];
    public string? ShardKey { get; init; }
    public string PartitionExpr { get; init; } = "toYYYYMM(toDate(1))";   // дефолт

    public override string ToString() => $"{Source} -> {Target} ({Columns.Count} cols)";
}
