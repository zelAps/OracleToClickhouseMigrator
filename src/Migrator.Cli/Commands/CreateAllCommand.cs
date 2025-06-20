using Spectre.Console;
using Spectre.Console.Cli;
using Migrator.Core.Config;
using Migrator.Core.ClickHouse;
using Migrator.Core.Oracle;
using Migrator.Core.Models;
using System.Collections.Generic;

namespace Migrator.Cli.Commands;

/// <summary>
/// Создаёт все таблицы из конфигурации в ClickHouse.
/// </summary>
public sealed class CreateAllCommand : AsyncCommand<CommonSettings>
{
    /// <summary>
    /// Читает конфиг, генерирует DDL и выполняет его для каждой таблицы.
    /// </summary>
    public override async Task<int> ExecuteAsync(CommandContext ctx, CommonSettings s)
    {
        var cfg = await MigratorConfig.LoadAsync(s.ConfigPath).ConfigureAwait(false);
        TypeMapper mapper = new();
        var reader = new OracleSchemaReader(cfg.Oracle.ConnectionString);
        var ddl = new ClickHouseDdlBuilder(cfg);

        foreach (var tblCfg in cfg.Tables)
        {
            var tbl = await reader.GetTableAsync(tblCfg, mapper.Map).ConfigureAwait(false);
            var sql = ddl.BuildAll(tbl);

            if (s.DryRun)
            {
                AnsiConsole.Write(new Markup($"[grey]{Markup.Escape(sql)}[/]\n"));
            }
            else
            {
                await ExecClickHouseAsync(cfg, sql).ConfigureAwait(false);
            }
        }

        return 0;
    }

    /// <summary>
    /// Отправляет сгенерированный SQL в ClickHouse.
    /// </summary>
    private static async Task ExecClickHouseAsync(MigratorConfig cfg, string sql)
    {
        var cs = $"Host=localhost;Database={cfg.ClickHouse.Database}";
        await using var ch = new ClickHouse.Client.ADO.ClickHouseConnection(cs);
        await ch.OpenAsync().ConfigureAwait(false);
        await using var cmd = ch.CreateCommand();
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
    }
}
