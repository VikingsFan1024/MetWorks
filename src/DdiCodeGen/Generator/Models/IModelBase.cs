namespace DdiCodeGen.Generator.Models;
public interface IModelBase
{
    string GenerationTimeRoundTripUtc { get; init; }
    string TemplateRequested { get; init; }
    string Namespace { get; init; }
    string ContainerClass { get; init; }
}