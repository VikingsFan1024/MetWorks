namespace DdiCodeGen.Generator.Models.Instance.Assignments.Initializer;
public record Assignment
{
    public required string ParameterName { get; init; }
    public required string InitializerArgumentExpression { get; init; }
}