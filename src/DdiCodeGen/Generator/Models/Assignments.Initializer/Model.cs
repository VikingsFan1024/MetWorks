namespace DdiCodeGen.Generator.Models.Instance.Assignments.Initializer;
public record Model : ModelBase, IModelSingleInstance
{
    public required string InitializerName { get; init; }
    public string InstanceName => Instance.Name;
    public required Instance Instance { get; init; }
}