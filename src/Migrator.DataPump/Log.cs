using Serilog;

public static class Log
{
    public static ILogger ForContext<T>() => _logger.ForContext<T>();

    private static readonly ILogger _logger = new LoggerConfiguration()
        .MinimumLevel.Debug()               // выводим всё, что Log.Information(...)
        .WriteTo.Console(
            outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}")
        .WriteTo.File(
            path: "logs/migrator-.log",
            rollingInterval: RollingInterval.Day,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3} {Message:lj}{NewLine}")
        .CreateLogger();
}
