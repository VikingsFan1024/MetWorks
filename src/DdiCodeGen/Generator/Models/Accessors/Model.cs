namespace DdiCodeGen.Generator.Models.Accessors;
public record Model : ModelBase
{
    public required List<Instance> Instances { get; init; }
}

