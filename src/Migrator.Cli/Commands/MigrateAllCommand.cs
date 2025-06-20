using Spectre.Console.Cli;
using Migrator.Core.ClickHouse;
using Migrator.Core.Oracle;
using Migrator.Core.Config;
using Migrator.DataPump;
using System.Collections.Generic;

namespace Migrator.Cli.Commands;

/// <summary>
/// Переносит данные по всем таблицам из конфигурации.
/// </summary>
public sealed class MigrateAllCommand : AsyncCommand<CommonSettings>
{
    /// <summary>
    /// Читает конфиг и запускает перенос данных для каждой таблицы.
    /// </summary>
    public override async Task<int> ExecuteAsync(CommandContext ctx, CommonSettings s)
    {
        var cfg = await MigratorConfig.LoadAsync(s.ConfigPath).ConfigureAwait(false);
        var mapper = new TypeMapper();
        var reader = new OracleSchemaReader(cfg.Oracle.ConnectionString);

        foreach (var t in cfg.Tables)
        {
            var tbl = await reader.GetTableAsync(t, mapper.Map).ConfigureAwait(false);
            var pump = new DataMigrator(
                cfg.Oracle.ConnectionString,
                $"Host=localhost;Database={cfg.ClickHouse.Database}",
                tbl);

            await pump.RunAsync(t.Where ?? string.Empty).ConfigureAwait(false);
        }

        return 0;
    }
}
