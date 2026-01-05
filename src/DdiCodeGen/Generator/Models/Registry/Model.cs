namespace DdiCodeGen.Generator.Models.Registry;
public record Model : ModelBase
{
    public required List<Instance> Instances { get; init; }
}