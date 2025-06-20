using Migrator.Core.Models;
using System;
using System.Collections.Generic;

namespace Migrator.Core.ClickHouse;

/// <summary>
/// Преобразует Oracle-тип колонки → ClickHouse-тип.
/// Таблица соответствия может дополняться внешним YAML (`typemap.yaml`).
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
            ["TIMESTAMP"] = "DateTime64(6)",      // точность до мкс
            ["BINARY_FLOAT"] = "Float64",
            ["BINARY_DOUBLE"] = "Float64",
            ["FLOAT"] = "Float64",
        };

        // подклеиваем кастомные/переопределённые
        if (extra is not null)
            foreach (var (k, v) in extra) _map[k] = v;
    }

    /// <summary>Заполняет поле <see cref="ColumnDef.ClickHouseType"/>.</summary>
    public ColumnDef Map(ColumnDef c)
    {
        if (c.SourceType.Equals("NUMBER", StringComparison.OrdinalIgnoreCase))
        {
            // без scale → целое
            if ((c.Scale is null or 0) && c.Precision is <= 9)
                c.ClickHouseType = "Int32";
            else if ((c.Scale is null or 0) && c.Precision is <= 18)
                c.ClickHouseType = "Int64";
            else
                c.ClickHouseType = $"Decimal({c.Precision ?? 38},{c.Scale ?? 0})";

            if (c.Nullable) c.ClickHouseType = $"Nullable({c.ClickHouseType})";
            return c;
        }

        if (!_map.TryGetValue(c.SourceType, out var chType))
            chType = "String";                       // fallback

        if (c.Nullable && !chType.StartsWith("Nullable"))
            chType = $"Nullable({chType})";

        c.ClickHouseType = chType;
        return c;
    }
}
