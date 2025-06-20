using System;
using System.Diagnostics;

namespace Migrator.DataPump;

/// <summary>Собирает статистику передачи для итоговой сводки.</summary>
public sealed class TransferStats
{
    private readonly Stopwatch _sw = Stopwatch.StartNew();
    public long Rows { get; private set; }
    public long Bytes { get; private set; }
    public int Skipped { get; private set; }
    public int Failed { get; private set; }

    public void Add(long rows, long bytes)
    {
        Rows += rows;
        Bytes += bytes;
    }

    public void Skip(int cnt = 1) => Skipped += cnt;
    public void Fail(int cnt = 1) => Failed += cnt;

    public TimeSpan Elapsed => _sw.Elapsed;
}
