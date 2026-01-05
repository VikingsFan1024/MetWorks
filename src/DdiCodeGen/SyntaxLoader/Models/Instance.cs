namespace DdiCodeGen.SyntaxLoader.Models;
// Named instance references a fully qualified class token; order in the list is significant
public sealed record Instance : BaseDto
{
    public string? InstanceName { get; }
    public string? ClassToken { get; }
    public string? ClassQualified { get; }
    public string? InterfaceQualified { get; }
    public bool InstanceIsArray { get; }
    public IReadOnlyList<Assignment> Assignments { get; }
    public bool HasAssignments => Assignments.Count > 0;
    public string InitializerParametersClause =>
        string.Join(Environment.NewLine, Assignments
            .Where(a => !string.IsNullOrWhiteSpace(a.InitializerParameterAssignmentClause))
            .Select(a => a.InitializerParameterAssignmentClause));
    public string ElementsConstructionExpression =>
        HasElements
            ? $"new {ClassQualified}[] {{ {string.Join(", ", Elements.Select(e => e.ElementInitializerClause))} }}"
            : $"new {ClassQualified}[] {{ }}";
    public IReadOnlyList<Element> Elements { get; }
    public bool HasElements => Elements.Count > 0;

    public Instance(
        string? instanceName,
        Class? @class,
        string? classToken,
        bool? instanceIsArray,
        IReadOnlyList<Assignment> assignments,
        IReadOnlyList<Element> elements,
        Location? location,
        IReadOnlyList<Diagnostic>? diagnostics = null
    ) : base(location, diagnostics)
    {
        InstanceName = instanceName;
        ClassToken = classToken;
        ClassQualified = @class?.ClassQualified;
        InterfaceQualified = @class?.InterfaceQualified;
        InstanceIsArray = instanceIsArray ?? false;
        Assignments = assignments;
        Elements = elements;
    }
}
