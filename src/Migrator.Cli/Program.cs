using Spectre.Console;
using Spectre.Console.Cli;
using Migrator.Cli.Commands;

// Точка входа CLI-приложения.
var app = new CommandApp();

app.Configure(cfg =>
{
    cfg.SetApplicationName("oracle2ch");
    cfg.AddBranch("create", b =>
    {
        b.AddCommand<CreateAllCommand>("all");
        b.AddCommand<CreateTablesCommand>("tables");
    });

    cfg.AddBranch("migrate", b =>
    {
        b.AddCommand<MigrateAllCommand>("all");
        b.AddCommand<MigrateTablesCommand>("tables");
    });

    cfg.PropagateExceptions();
});

return await app.RunAsync(args);
