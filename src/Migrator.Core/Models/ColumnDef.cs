namespace Migrator.Core.Models;

/// <summary>
/// Описание колонки, полученное из Oracle
///     → и целевой ClickHouse-типа/имени (после маппинга/переименования).
/// </summary>
public sealed class ColumnDef
{
    /* ----- исходные данные Oracle ----- */
    public required string SourceName { get; init; }            // EMP_ID
    public required string SourceType { get; init; }            // NUMBER
    public int? Precision { get; init; }                        // 10
    public int? Scale { get; init; }                        // 0
    public bool Nullable { get; init; }
    public int? DataLength { get; init; }                        // для VARCHAR2
    public string? Default { get; init; }

    /* ----- целевые данные ClickHouse (заполняются далее) ----- */
    public required string TargetName { get; set; }             // id
    public required string ClickHouseType { get; set; }         // UInt32  | Nullable(String)

    public override string ToString() =>
        $"{SourceName} ({SourceType}) -> {TargetName} {ClickHouseType}";
}
