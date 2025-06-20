using Oracle.ManagedDataAccess.Client;
using ClickHouse.Client.ADO;
using Migrator.Core.Models;
using Migrator.DataPump;
using Migrator.Core.ClickHouse;
using Serilog;
using System.Threading.Tasks;
using System.Threading;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Migrator.DataPump;

/// <summary>
/// Оркестрация: читаем, маппим, пишем блоками.
/// </summary>
public sealed class DataMigrator(
    string oracleCs,
    string clickhouseCs,
    TableDef table,
    int batchSize = 100_000)
{
    private readonly string _oracleCs = oracleCs;
    private readonly string _clickCs = clickhouseCs;
    private readonly TableDef _tbl = table;
    private readonly int _batchSize = batchSize;

    private readonly ILogger _log = Log.ForContext<DataMigrator>();

    /// <summary>
    /// Запускает полный процесс переноса данных для выбранной таблицы.
    /// </summary>
    public async Task<TransferStats> RunAsync(string? where, CancellationToken ct = default)
    {
        var stats = new TransferStats();

        await using var orclConn = new OracleConnection(_oracleCs);
        await orclConn.OpenAsync(ct).ConfigureAwait(false);

        var owner = _tbl.Owner;
        var sourceFull = string.IsNullOrEmpty(owner)
                         ? _tbl.Source
                         : $"{owner}.{_tbl.Source}";

        var select = $"SELECT {string.Join(",", _tbl.Columns.Select(c => c.SourceName))} " +
                     $"FROM {sourceFull}" +
                     (string.IsNullOrWhiteSpace(where) ? "" : $" WHERE {where}");

        await using (var cntCmd = orclConn.CreateCommand())
        {
            cntCmd.CommandText = $"SELECT COUNT(*) FROM {sourceFull}";
            var total = Convert.ToInt64(await cntCmd.ExecuteScalarAsync(ct).ConfigureAwait(false));
            _log.Information("Oracle rows total = {Total}", total);
        }

        _log.Information("⏩ Start {Table} ({Where})", _tbl.Source, where ?? "full");
        _log.Information("▶️  SELECT: {Sql}", select);

        await using var reader = new OracleStreamReader(orclConn, select, batchSize: _batchSize);
        await using var writer = new ClickHouseBulkWriter(
            _clickCs,
            $"{_tbl.Target}_local",
            _tbl.Columns.Select(c => c.TargetName).ToArray(),
            _batchSize);


        await foreach (var block in reader.ReadAsync(ct).ConfigureAwait(false))
        {
            try
            {
                await writer.WriteAsync(block.Span.ToArray(), ct).ConfigureAwait(false);
                stats.Add(block.Length, 0);           // bytes ≈ неточные, можно оценить

                if (stats.Rows % 500_000 == 0 || block.Length < _batchSize)
                {
                    _log.Information("Copied {Rows} rows so far…", stats.Rows);
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Insert failed:\n{Stack}", ex.ToString());
                stats.Fail();
            }
        }

        _log.Information("✅ {Rows} rows → {Table} in {Elapsed}",
            stats.Rows, _tbl.Target, stats.Elapsed);

        return stats;
    }
}
