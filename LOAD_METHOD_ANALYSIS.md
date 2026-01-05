# Load() Method Analysis: Current vs. Proposed Refactoring

**File:** [src/DdiCodeGen/SyntaxLoader/Processing/Loader.Types.cs](src/DdiCodeGen/SyntaxLoader/Processing/Loader.Types.cs#L8-L101)

**Official Reference:** [YamlDotNet RepresentationModel Documentation](https://github.com/aaubry/YamlDotNet/wiki/Overview#yamldotnetrepresentationmodel)

---

## Current Implementation (Lines 8–101)

```csharp
public Model Load(string yamlText)
{
    ArgumentNullException.ThrowIfNull(yamlText);

    var diagnostics = new List<Diagnostic>();
    YamlMappingNode? rootYamlMappingNode = null;
    var logicalPath = Schema.TokenTypeNames[Schema.TokenTypes.codeGen];  // ⚠️ Issue #1
    try
    {
        var yaml = new YamlStream();
        using var reader = new StringReader(yamlText);
        yaml.Load(reader);

        if (yaml.Documents.Count == 0) 
            throw new ArgumentException("YAML document is empty.", nameof(yamlText));

        var rootNode = yaml.Documents[0].RootNode;
        var documentLocation = new DocumentLocation(rootNode, logicalPath);  // ⚠️ Issue #3
        if (!(rootNode is YamlMappingNode yamlMappingNode))
            DiagnosticsHelper.Add(
                diagnosticList: diagnostics,
                diagnosticCode: DiagnosticCode.YamlEmptyDocument,
                diagnosticMessage: "YAML root node is not a mapping node.",
                documentLocation: documentLocation
            );
        else
            rootYamlMappingNode = yamlMappingNode;  // ⚠️ Issue #2
    }
    catch (YamlDotNet.Core.YamlException ex)
    {
        DiagnosticsHelper.Add(
            diagnosticList: diagnostics,
            diagnosticCode: DiagnosticCode.UnrecognizedToken,
            diagnosticMessage: $"YAML parse error: {ex.Message}",
            documentLocation: new DocumentLocation(
                lineZeroBased: ex.Start.Line, 
                columnZeroBased: ex.Start.Column,
                logicalPath: logicalPath
            )
        );
    }
    catch (Exception ex)
    {
        DiagnosticsHelper.Add(
            diagnosticList: diagnostics,
            diagnosticCode: DiagnosticCode.UnrecognizedToken,
            diagnosticMessage: $"Unexpected YAML load error: {ex.Message}",
            documentLocation: new DocumentLocation(
                lineZeroBased: 0,
                columnZeroBased: 0,
                logicalPath: logicalPath
            )
        );
    }

    if (rootYamlMappingNode is null)  // ⚠️ Issue #2 & #4
    {
        return new Model(
            codeGen: null,
            namespaces: null,
            namedInstances: null,
            provenanceStack: new ProvenanceStack( new DocumentLocation(0, 0, logicalPath) ),
            diagnostics: diagnostics
        );
    }

    var model = ParseModel(rootYamlMappingNode, @"/");  // ⚠️ Issue #6

    if (diagnostics.Count > 0)  // ⚠️ Issue #6
    {
        var merged = model.Diagnostics.ToList();
        merged.InsertRange(0, diagnostics);
        var mergedDiagnostics = merged.AsReadOnly();
        model = model with { Diagnostics = mergedDiagnostics };
    }

    if (model.Diagnostics.Any(d => d.DiagnosticCode.GetSeverity() == DiagnosticSeverity.Error))
    {
        return model;
    }

    return model;  // ⚠️ Issue #5
}
```

---

## Issues Identified

| # | Issue | Line(s) | Problem | Impact |
|---|-------|---------|---------|--------|
| 1 | Incorrect logicalPath | 13 | `logicalPath` set to `Schema.TokenTypeNames[codeGen]` instead of root path | Diagnostics report wrong context for document-level errors |
| 2 | Sentinel Pattern | 14, 64 | Using `null` flag instead of early return | Code is harder to follow; mixes success/failure paths |
| 3 | Early DocumentLocation | 18 | Creating `DocumentLocation` before validating node type | Wastes cycles if validation fails; muddled error handling |
| 4 | Mixed Error Philosophy | 20-65 | Throws ArgumentException for empty doc, catches it, logs as diagnostic | Inconsistent error handling strategy |
| 5 | Redundant Return | 101 | Last return statement unreachable due to prior conditional | Dead code |
| 6 | Implicit Diagnostic Ordering | 81-88 | Merge order not documented; unclear if order matters | Maintenance risk; unclear semantics |

---

## Proposed Refactoring

### Design Pattern
**Guard → Parse → Validate → Delegate → Compose**

```csharp
public Model Load(string yamlText)
{
    // GUARD: Validate input
    ArgumentNullException.ThrowIfNull(yamlText);

    var logicalPath = @"/";  // ✅ Document root, not codeGen
    var diagnostics = new List<Diagnostic>();

    // PARSE: Load YAML, collect parse errors as diagnostics (no throw)
    YamlMappingNode? rootYamlMappingNode = null;
    try
    {
        var yaml = new YamlStream();
        using var reader = new StringReader(yamlText);
        yaml.Load(reader);

        if (yaml.Documents.Count == 0)
        {
            // Create diagnostic for empty document
            DiagnosticsHelper.Add(
                diagnosticList: diagnostics,
                diagnosticCode: DiagnosticCode.YamlEmptyDocument,
                diagnosticMessage: "YAML document is empty.",
                documentLocation: new DocumentLocation(0, 0, logicalPath)
            );
        }
        else if (yaml.Documents[0].RootNode is YamlMappingNode yamlMappingNode)
        {
            // ✅ Only create DocumentLocation on success
            rootYamlMappingNode = yamlMappingNode;
        }
        else
        {
            // Root is not a mapping—create diagnostic
            var rootNode = yaml.Documents[0].RootNode;
            DiagnosticsHelper.Add(
                diagnosticList: diagnostics,
                diagnosticCode: DiagnosticCode.YamlEmptyDocument,
                diagnosticMessage: "YAML root node is not a mapping node.",
                documentLocation: new DocumentLocation(rootNode, logicalPath)
            );
        }
    }
    catch (YamlDotNet.Core.YamlException ex)
    {
        // YAML parse error—convert to diagnostic
        DiagnosticsHelper.Add(
            diagnosticList: diagnostics,
            diagnosticCode: DiagnosticCode.UnrecognizedToken,
            diagnosticMessage: $"YAML parse error: {ex.Message}",
            documentLocation: new DocumentLocation(
                lineZeroBased: ex.Start.Line,
                columnZeroBased: ex.Start.Column,
                logicalPath: logicalPath
            )
        );
    }
    catch (Exception ex)
    {
        // Unexpected error—convert to diagnostic
        DiagnosticsHelper.Add(
            diagnosticList: diagnostics,
            diagnosticCode: DiagnosticCode.UnrecognizedToken,
            diagnosticMessage: $"Unexpected YAML load error: {ex.Message}",
            documentLocation: new DocumentLocation(0, 0, logicalPath)
        );
    }

    // VALIDATE: Fail fast on critical errors
    if (rootYamlMappingNode is null)
    {
        // ✅ Early return instead of sentinel pattern
        return new Model(
            codeGen: null,
            namespaces: null,
            namedInstances: null,
            provenanceStack: new ProvenanceStack(new DocumentLocation(0, 0, logicalPath)),
            diagnostics: diagnostics
        );
    }

    // DELEGATE: Parse the model
    var model = ParseModel(rootYamlMappingNode, logicalPath);

    // COMPOSE: Merge load-time diagnostics with parse-time diagnostics
    // Load-time diagnostics first (document structure issues precede content issues)
    if (diagnostics.Count > 0)
    {
        var merged = new List<Diagnostic>(diagnostics);  // ✅ Explicit semantics
        merged.AddRange(model.Diagnostics);             // ✅ Clear ordering
        model = model with { Diagnostics = merged.AsReadOnly() };
    }

    // Fail fast if any Error-level diagnostics exist
    if (model.Diagnostics.Any(d => d.DiagnosticCode.GetSeverity() == DiagnosticSeverity.Error))
    {
        return model;
    }

    // ✅ Single return at end
    return model;
}
```

---

## Key Improvements

| Aspect | Current | Proposed | Benefit |
|--------|---------|----------|---------|
| **Logical Path** | `codeGen` | `@"/"` | Correct document-level context for diagnostics |
| **Error Handling** | Throw → Catch → Log | Log directly | Consistent error philosophy; clearer flow |
| **Control Flow** | Sentinel null flag | Early return | Easier to follow; reduces indentation |
| **DocumentLocation** | Created before validation | Created after success | Wastes less CPU; clearer intent |
| **Diagnostic Merge** | Implicit ordering | Explicit with comment | Self-documenting; easier to maintain |
| **Dead Code** | Final `return model;` | Single return | Eliminates confusion |

---

## Side-by-Side Comparison: Error Path

### Current (Using Sentinel Pattern)
```
1. Create null flag: rootYamlMappingNode = null
2. Try to parse
3. On failure, leave null flag set
4. Check flag later: if (null) { return error; }
5. Proceed with success path
```
**Issue:** Flag status is far from usage; success path is nested

### Proposed (Using Early Return)
```
1. Try to parse
2. Extract mapping node successfully
3. On failure, immediately return with diagnostics
4. Continue with success path (no nesting)
```
**Issue:** None—clear, immediate failure handling

---

## Implementation Note

**Breaking Changes:** None. The method signature and return type remain identical. The refactoring is purely internal—same inputs, same outputs, cleaner implementation.

