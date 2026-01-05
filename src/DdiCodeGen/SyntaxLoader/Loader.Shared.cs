using static DdiCodeGen.SyntaxLoader.Models.Schema;
namespace DdiCodeGen.SyntaxLoader;
public sealed partial class Loader
{
    // Safe scalar extraction helper
    private static string? GetScalar(YamlMappingNode yamlMappingNode, string key)
    {
        if (yamlMappingNode.Children
            .TryGetValue(
                new YamlScalarNode(key), out var value
            )
            && value is YamlScalarNode scalar
        )
        {
            if (scalar.Value is null) return null;
            var trimmedScalarValue = scalar.Value.Trim();
            if (trimmedScalarValue.Length == 0) return null;
            return trimmedScalarValue == @"null" ? null : trimmedScalarValue;
        }
            
        return null;
    }

    // Return null when child sequence is missing
    private static YamlSequenceNode? GetChildSequence(YamlMappingNode yamlMappingNode, string key)
        => yamlMappingNode.Children.TryGetValue(
            new YamlScalarNode(key), out var value) 
                && value is YamlSequenceNode seq ? seq : null;

    // Return null when child mapping is missing
    private static YamlMappingNode? GetChildMapping(YamlMappingNode yamlMappingNode, string key)
        => yamlMappingNode.Children.TryGetValue(new YamlScalarNode(key), out var value) && value is YamlMappingNode map ? map : null;


    // Validate mapping keys against schema and return diagnostics
    private IReadOnlyList<Diagnostic> ValidateMappingKeys(
        YamlMappingNode yamlMappingNode, 
        Type dtoType, 
        string logicalPath
    )
    {
        var diagnostics = new List<Diagnostic>();

        if (!AllowedKeys.TryGetValue(dtoType, out var allowed)) return diagnostics;

        var allowedSet = new HashSet<string>(allowed, StringComparer.Ordinal);

        foreach (var kv in yamlMappingNode.Children)
        {
            if (kv.Key is not YamlScalarNode keyScalar) continue;
            var key = keyScalar.Value ?? string.Empty;
            if (!allowedSet.Contains(key))
            {
                // Use DiagnosticsHelper so location and provenance are consistent
                var documentLocation = new Location(keyScalar, logicalPath);
                diagnostics.Add(
                    diagnosticCode: DiagnosticCode.UnrecognizedToken,
                    message: $"Unrecognized token '{key}' at {logicalPath}. Allowed keys: {string.Join(", ", allowed)}",
                    location: documentLocation
                );
            }
        }

        return diagnostics.ToList().AsReadOnly();
    }
}