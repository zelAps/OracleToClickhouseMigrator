using System.Data;
using Oracle.ManagedDataAccess.Client;
using Migrator.Core.Models;
using Migrator.Core.Config;
using System.Threading.Tasks;
using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;

namespace Migrator.Core.Oracle;

/// <summary>
/// Извлекает метаданные (список колонок, PK) из Oracle.
/// </summary>
public sealed class OracleSchemaReader(string connectionString)
{
    private readonly string _connectionString = connectionString
        .Replace("\r", " ")
        .Replace("\n", " ")
        .Trim();


    /// <summary>
    /// Возвращает полное описание указанной таблицы,
    /// применяя переименования из конфигурации.
    /// </summary>
    public async Task<TableDef> GetTableAsync(
        MigratorConfig.TableSection cfg,
        Func<ColumnDef, ColumnDef> mapToClickHouse,
        CancellationToken ct = default)
    {
        await using var conn = new OracleConnection(_connectionString);
        await conn.OpenAsync(ct);

        var owner = cfg.Owner ?? await GetCurrentUserAsync(conn, ct);
        var columns = await GetColumnsAsync(conn, cfg.Source, owner, ct);
        var pk = await GetPrimaryKeyAsync(conn, cfg.Source, owner, ct);

        // Применяем rename + типы
        foreach (var c in columns)
        {
            if (cfg.RenameFields?.TryGetValue(c.SourceName, out var newName) == true)
                c.TargetName = newName;
            else
                c.TargetName = c.SourceName.ToLowerInvariant();

            // трансформируем в ClickHouse-тип
            c.ClickHouseType = mapToClickHouse(c).ClickHouseType;
        }

        return new TableDef
        {
            Source = cfg.Source,
            Target = cfg.Target ?? cfg.Source,
            Columns = columns,
            Owner = owner,             // 👈
            PrimaryKey = pk,
            ShardKey = cfg.ShardKey,
            PartitionExpr = BuildPartition(columns)
        };
    }

    /* ----- приватные методы ----- */

   private static async Task<List<ColumnDef>> GetColumnsAsync(
    OracleConnection conn, string tableName, string owner, CancellationToken ct)
    {
        const string sql = @"
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    DATA_PRECISION,
    DATA_SCALE,
    NULLABLE,
    DATA_LENGTH,
    DATA_DEFAULT
FROM ALL_TAB_COLUMNS
WHERE UPPER(TABLE_NAME) = :tbl
  AND OWNER = :own
ORDER BY COLUMN_ID";

        await using var cmd = new OracleCommand(sql, conn);
        cmd.BindByName = true;
        cmd.Parameters.Add(":tbl", OracleDbType.Varchar2, tableName.ToUpper(), ParameterDirection.Input);
        cmd.Parameters.Add(":own", OracleDbType.Varchar2, owner.ToUpper(), ParameterDirection.Input);
        await using var rdr = await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess, ct);

        var list = new List<ColumnDef>();
        while (await rdr.ReadAsync(ct))
        {
            list.Add(new ColumnDef
            {
                SourceName = rdr.GetString(0),
                SourceType = rdr.GetString(1),
                Precision = rdr.IsDBNull(2) ? null : rdr.GetInt32(2),
                Scale = rdr.IsDBNull(3) ? null : rdr.GetInt32(3),
                Nullable = rdr.GetString(4) == "Y",
                DataLength = rdr.IsDBNull(5) ? null : rdr.GetInt32(5),
                Default = rdr.IsDBNull(6) ? null : rdr.GetString(6),

                // временно, потом перезапишем
                TargetName = rdr.GetString(0).ToLowerInvariant(),
                ClickHouseType = "String"
            });
        }

        return list;
    }

private static async Task<List<string>> GetPrimaryKeyAsync(
    OracleConnection conn, string tableName, string owner, CancellationToken ct)
    {
        const string sql = @"
SELECT acc.COLUMN_NAME
FROM ALL_CONSTRAINTS ac
JOIN ALL_CONS_COLUMNS acc
  ON ac.OWNER = acc.OWNER
 WHERE ac.CONSTRAINT_NAME = acc.CONSTRAINT_NAME
   AND ac.CONSTRAINT_TYPE = 'P'
   AND ac.TABLE_NAME = :tbl
   AND ac.OWNER = :own
ORDER BY acc.POSITION";

        await using var cmd = new OracleCommand(sql, conn);
        cmd.BindByName = true;
        cmd.Parameters.Add(":tbl", OracleDbType.Varchar2, tableName.ToUpper(), ParameterDirection.Input);
        cmd.Parameters.Add(":own", OracleDbType.Varchar2, owner.ToUpper(), ParameterDirection.Input);
        await using var rdr = await cmd.ExecuteReaderAsync(ct);
        var pk = new List<string>();
        while (await rdr.ReadAsync(ct))
            pk.Add(rdr.GetString(0).ToLowerInvariant());

        return pk;
    }

    private static string BuildPartition(IEnumerable<ColumnDef> cols)
    {
        // Простейшая эвристика: ищем date/timestamp колонку c именем like '%date%'
        var dateCol = cols.FirstOrDefault(c =>
            c.SourceType.StartsWith("DATE", StringComparison.OrdinalIgnoreCase) ||
            c.SourceType.StartsWith("TIMESTAMP", StringComparison.OrdinalIgnoreCase));

        return dateCol is not null
            ? $"toYYYYMM({dateCol.TargetName})"
            : "toYYYYMM(toDate(1))";
    }

    private static async Task<string> GetCurrentUserAsync(
    OracleConnection conn, CancellationToken ct)
    {
        await using var cmd = new OracleCommand("SELECT USER FROM DUAL", conn);
        return (string)(await cmd.ExecuteScalarAsync(ct) ?? "UNKNOWN");
    }
}
