namespace DdiCodeGen.Generator.Models.Instance.Factory;
public record Instance : InstanceBase
{
    public required string ClassQualified { get; init; }
    public required bool IsArray { get; init; }
    public required bool HasElements { get; init; }
}