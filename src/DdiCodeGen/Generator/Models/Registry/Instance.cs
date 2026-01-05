namespace DdiCodeGen.Generator.Models.Registry;
public record Instance
{
    public required string Name { get; init; }
    public required bool HasAssignments { get; init; }
    public required bool HasDisposable { get; init; }
}