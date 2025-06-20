using System;
using System.Collections.Generic;
using Migrator.Core.Models;

namespace Migrator.Core.ClickHouse;

/// <summary>
/// Преобразуем Oracle-тип колонки в ClickHouse-тип.
/// Таблица соответствия может дополняться внешним YAML (typemap.yaml).
/// </summary>
public sealed class TypeMapper
{
    private readonly Dictionary<string, string> _map;

    public TypeMapper(IDictionary<string, string>? extra = null)
    {
        _map = new(StringComparer.OrdinalIgnoreCase)
        {
            ["CHAR"] = "String",
            ["NCHAR"] = "String",
            ["VARCHAR2"] = "String",
            ["NVARCHAR2"] = "String",
            ["CLOB"] = "String",
            ["BLOB"] = "Nullable(String)",
            ["RAW"] = "Nullable(String)",
            ["DATE"] = "Date32",
            ["TIMESTAMP"] = "DateTime64(6)",
            ["BINARY_FLOAT"] = "Float64",
            ["BINARY_DOUBLE"] = "Float64",
            ["FLOAT"] = "Float64",
        };

        if (extra is not null)
        {
            foreach (var (k, v) in extra)
            {
                _map[k] = v;
            }
        }
    }

    /// <summary>
    /// Определяем тип ClickHouse для колонки Oracle и возвращает обновлённый объект.
    /// </summary>
    public ColumnDef Map(ColumnDef column)
    {
        column.ClickHouseType = column.SourceType.ToUpperInvariant() switch
        {
            "NUMBER" when column.Scale is null or 0 && column.Precision is <= 9
                => "Int32",
            "NUMBER" when column.Scale is null or 0 && column.Precision is <= 18
                => "Int64",
            "NUMBER" => $"Decimal({column.Precision ?? 38},{column.Scale ?? 0})",
            var t when _map.TryGetValue(t, out var mapped) => mapped,
            _ => "String",
        };

        if (column.Nullable &&
            !column.ClickHouseType.StartsWith("Nullable", StringComparison.Ordinal))
        {
            column.ClickHouseType = $"Nullable({column.ClickHouseType})";
        }

        return column;
    }
}
