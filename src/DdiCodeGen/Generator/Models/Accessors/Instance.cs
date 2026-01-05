namespace DdiCodeGen.Generator.Models.Accessors;
public record Instance : InstanceBase
{
    public required string ClassQualified { get; init; }
    public string? InterfaceQualified { get; init; }
    public required bool IsArray { get; init; }
}