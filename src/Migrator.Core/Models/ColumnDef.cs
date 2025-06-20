namespace Migrator.Core.Models;

public sealed class ColumnDef
{
    /* ----- исходные данные Oracle ----- */
    public required string SourceName { get; init; }            
    public required string SourceType { get; init; }            
    public int? Precision { get; init; }                       
    public int? Scale { get; init; }
    public bool Nullable { get; init; }
    public int? DataLength { get; init; }
    public string? Default { get; init; }


    public required string TargetName { get; set; }             
    public required string ClickHouseType { get; set; }     

    public override string ToString() =>
        $"{SourceName} ({SourceType}) -> {TargetName} {ClickHouseType}";
}
