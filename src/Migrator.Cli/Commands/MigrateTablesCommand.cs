using Spectre.Console.Cli;
using Migrator.Core.Config;
using Migrator.Core.ClickHouse;
using Migrator.Core.Oracle;
using Migrator.DataPump;
using System.Threading.Tasks;
using System.Linq;

namespace Migrator.Cli.Commands;

public sealed class MigrateTablesCommand : AsyncCommand<TableNamesSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext ctx, TableNamesSettings s)
    {
        var cfg = await MigratorConfig.LoadAsync(s.ConfigPath);

        Console.WriteLine($"Args: [{string.Join(",", s.TableNames ?? Array.Empty<string>())}]");

        var want = s.TableNames.Select(t => t.ToUpperInvariant()).ToHashSet();

        if (s.TableNames is null || s.TableNames.Length == 0)
        {
            Console.WriteLine("⚠️  Не переданы имена таблиц после слова 'tables'.");
            Console.WriteLine("   Пример: migrate tables DECLARATION_ACTIVE");
            return -1;
        }

        var mapper = new TypeMapper();
        var reader = new OracleSchemaReader(cfg.Oracle.ConnectionString);

        var toMove = cfg.Tables
                        .Where(t => want.Contains(t.Source.ToUpperInvariant()))
                        .ToList();

        if (toMove.Count == 0)
        {
            Console.WriteLine("⛔ Нечего мигрировать — список таблиц пустой.");
            return -1;
        }

        foreach (var t in toMove)
        {
            var tbl = await reader.GetTableAsync(t, mapper.Map);
            var pump = new DataMigrator(
                cfg.Oracle.ConnectionString,
                      cfg.ClickHouse.ConnectionString
                          ?? Environment.GetEnvironmentVariable("CH_CS")
                          ?? $"Host=localhost;Database={cfg.ClickHouse.Database}",
                tbl);

            await pump.RunAsync(t.Where ?? string.Empty);
        }

        return 0;
    }
}
