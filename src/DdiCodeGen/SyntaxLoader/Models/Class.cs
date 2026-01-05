namespace DdiCodeGen.SyntaxLoader.Models;

// Class declaration inside a namespace: short ClassName only; transformer derives QualifiedClassName
public sealed record Class : BaseDto
{
    public string? ClassName { get; }
    public string? ClassQualified { get; }
    public string? InterfaceQualified { get; }
    public bool HasInterface => InterfaceQualified is not null;
    public string? TypeQualified => InterfaceQualified ?? ClassQualified;
    public Dictionary<string, Parameter> Parameters { get; }
    public Class(
        string? namespaceName,
        string? className,
        string? interfaceQualified,
        Dictionary<string, Parameter>? parameters,
        Location? location,
        IReadOnlyList<Diagnostic>? diagnostics = null
    ) : base(location, diagnostics)
    {
        ClassName = className;
        ClassQualified = $"{namespaceName}.{className}";
        InterfaceQualified = interfaceQualified;
        Parameters = parameters ?? new Dictionary<string, Parameter>();
    }
}
