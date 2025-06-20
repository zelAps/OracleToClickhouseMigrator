using ClickHouse.Client.ADO;
using ClickHouse.Client.Copy;
using System.Collections.Generic;
using System;
using System.Data;
using System.Threading.Tasks;
using System.Threading;

namespace Migrator.DataPump;

public sealed class ClickHouseBulkWriter : IAsyncDisposable
{
    private readonly ClickHouseConnection _conn;
    private readonly ClickHouseBulkCopy _bulk;
    private readonly string[] _columnNames;
    private bool _inited;                       // <-- флаг

    public ClickHouseBulkWriter(
        string connectionString,
        string table,
        string[] columnNames,
        int batchSize = 100_000)
    {
        _conn = new ClickHouseConnection(connectionString);
        _conn.Open();                         // открываем синхронно, это допустимо

        _bulk = new ClickHouseBulkCopy(_conn)
        {
            DestinationTableName = table,
            ColumnNames = columnNames,   // <-- задаём сразу
            BatchSize = batchSize,
            MaxDegreeOfParallelism = Environment.ProcessorCount
        };

        _columnNames = columnNames;
    }

    /// <summary>
    /// Записывает блок строк в ClickHouse через BulkCopy.
    /// </summary>
    public async Task WriteAsync(IEnumerable<object?[]> rows,
                                 CancellationToken ct = default)
    {
        // ① первый вызов — инициируем BulkCopy (без аргументов!)
        if (!_inited)
        {
            await _bulk.InitAsync();  // ← в библиотеке 7.14 именно такой метод
            _inited = true;
        }

        // ② формируем DataTable c теми же именами
        var tbl = new DataTable();
        foreach (var col in _columnNames)
            tbl.Columns.Add(new DataColumn(col, typeof(object)));

        foreach (var r in rows)
            tbl.Rows.Add(r);

        await _bulk.WriteToServerAsync(tbl, ct);
    }

    public async ValueTask DisposeAsync() => await _conn.DisposeAsync();
}
