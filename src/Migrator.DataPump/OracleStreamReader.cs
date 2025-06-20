using System.Collections.Generic;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using Migrator.Core.Models;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Threading;
using System;

namespace Migrator.DataPump;

/// <summary>
/// Потоково читает данные из Oracle курсором, 
/// возвращая строки порциями <see cref="BatchSize"/>.
/// </summary>
public sealed class OracleStreamReader : IAsyncDisposable
{
    private readonly OracleCommand _cmd;
    private readonly OracleDataReader _rdr;
    private readonly int _batchSize;

    public OracleStreamReader(
        OracleConnection conn,
        string sql,
        IEnumerable<OracleParameter>? parameters = null,
        int batchSize = 100_000)
    {
        _batchSize = batchSize;

        _cmd = conn.CreateCommand();
        _cmd.CommandText = sql;
        _cmd.BindByName = true;
        _cmd.FetchSize = batchSize * 1024;   // ~1K per row heuristic

        if (parameters is not null)
            foreach (var p in parameters) _cmd.Parameters.Add(p);

        _rdr = _cmd.ExecuteReader(CommandBehavior.SequentialAccess);
    }

    /// <summary>Асинхронная последовательность — каждая итерация = блок строк.</summary>
    public async IAsyncEnumerable<ReadOnlyMemory<object?[]>> ReadAsync(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var buffer = new List<object?[]>(_batchSize);

        while (await _rdr.ReadAsync(ct))
        {
            var row = new object?[_rdr.FieldCount];
            _rdr.GetValues(row);
            buffer.Add(row);

            if (buffer.Count >= _batchSize)
            {
                yield return buffer.ToArray();
                buffer.Clear();
            }
        }

        if (buffer.Count > 0)
            yield return buffer.ToArray();
    }

    public async ValueTask DisposeAsync()
    {
        await _rdr.DisposeAsync();
        await _cmd.DisposeAsync();
    }
}
