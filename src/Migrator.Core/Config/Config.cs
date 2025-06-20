using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using System.Text.Json;
using Migrator.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System;

namespace Migrator.Core.Config;

/// <summary>
/// Корневая модель конфигурационного файла (YAML/JSON/TOML).
/// </summary>
public sealed class MigratorConfig
{
    public required OracleSection Oracle { get; init; }
    public required ClickHouseSection ClickHouse { get; init; }
    public required List<TableSection> Tables { get; init; }

    /* ---------- секции ---------- */

    public sealed record OracleSection
    {
        public required string ConnectionString { get; init; }
    }

    public sealed record ClickHouseSection
    {
        public string? ConnectionString { get; init; }
        public required string Cluster { get; init; }
        public required string ZkPathPrefix { get; init; }
        public required string Database { get; init; }
        public required List<string> Replicas { get; init; }
        public string? DistributedShardKey { get; init; }
    }

    public sealed record TableSection
    {
        public required string Source { get; init; }
        public string? Target { get; init; }
        public Dictionary<string, string>? RenameFields { get; init; }
        public string? Where { get; init; }
        public string? ShardKey { get; init; }
        public string? Owner { get; init; }
    }

    /* ---------- загрузка ---------- */

    public static async Task<MigratorConfig> LoadAsync(string path, CancellationToken ct = default)
    {
        await using var fs = File.OpenRead(path);
        var ext = Path.GetExtension(path).ToLowerInvariant();

        return ext switch
        {
            ".yaml" or ".yml" => LoadYaml(await new StreamReader(fs).ReadToEndAsync(ct)),
            ".json" => JsonSerializer.Deserialize<MigratorConfig>(fs)
                                 ?? throw new InvalidDataException("bad json"),
            _ => throw new NotSupportedException($"Unknown config format: {ext}")
        };
    }

    private static MigratorConfig LoadYaml(string text)
    {
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        return deserializer.Deserialize<MigratorConfig>(text)
               ?? throw new InvalidDataException("bad yaml");
    }
}
