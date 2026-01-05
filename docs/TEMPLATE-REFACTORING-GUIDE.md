# Template Refactoring with Partials - Implementation Guide

## Overview

Partials have been introduced to eliminate duplication across templates. This document explains what needs to change in the code generator to support them.

## Created Partials

1. **`_partials/GeneratedFileHeader.hbs`** - Standard file header (// TemplateName, // GeneratedHeader, #nullable enable)
2. **`_partials/AccessorTriple.hbs`** - Register + Get (external) + Get_Internal (internal) accessor pattern

## Code Changes Required

### 1. Update Template Loading Code

**Location:** Wherever you load templates (likely in `TemplateStore` or `CodeGenerator`)

**Before:**
```csharp
var templateContent = LoadTemplate("Registry.Accessors.hbs");
var compiledTemplate = handlebars.Compile(templateContent);
```

**After:**
```csharp
var handlebars = Handlebars.Create();

// Load and register all partials first
var partialsDir = Path.Combine(templatesDir, "_partials");
foreach (var partialFile in Directory.GetFiles(partialsDir, "*.hbs"))
{
    var partialName = Path.GetFileNameWithoutExtension(partialFile);
    var partialContent = File.ReadAllText(partialFile);
    handlebars.RegisterTemplate(partialName, partialContent);
}

// Now compile main templates
var templateContent = LoadTemplate("Registry.Accessors.hbs");
var compiledTemplate = handlebars.Compile(templateContent);
```

### 2. Embedded Resource Considerations

If templates are embedded resources, you'll need to:

1. Mark `_partials/*.hbs` files as embedded resources in `.csproj`
2. Load partials from resources:

```csharp
var assembly = typeof(TemplateStore).Assembly;
var resourceNames = assembly.GetManifestResourceNames()
    .Where(r => r.Contains("._partials.") && r.EndsWith(".hbs"));

foreach (var resourceName in resourceNames)
{
    using var stream = assembly.GetManifestResourceStream(resourceName);
    using var reader = new StreamReader(stream);
    var partialContent = reader.ReadToEnd();
    
    // Extract partial name from resource name
    var partialName = resourceName.Split('.')[^2]; // e.g., "GeneratedFileHeader"
    handlebars.RegisterTemplate(partialName, partialContent);
}
```

### 3. Update .csproj to Include Partials

**Add to `TemplateStore.csproj`:**
```xml
<ItemGroup>
  <EmbeddedResource Include="Templates\**\*.hbs" />
</ItemGroup>
```

This ensures partials are included in builds.

### 4. Template Migration Path

**Phase 1: Proof of Concept (Current)**
- ✅ Created partials directory
- ✅ Created 2 sample partials
- ✅ Created refactored example (`Registry.Accessors.REFACTORED.hbs`)
- ⏳ Test with code generator

**Phase 2: Validate Approach**
- Update code generator to register partials
- Test that `Registry.Accessors.REFACTORED.hbs` produces identical output
- Compare byte-for-byte with current generated files

**Phase 3: Full Migration**
Once validated, refactor remaining templates:
- [ ] `Registry.hbs` - Use `GeneratedFileHeader` partial
- [ ] `Registry.InstanceFactory.hbs` - Use `GeneratedFileHeader` partial  
- [ ] `Registry.InstanceField.hbs` - Use `GeneratedFileHeader` partial
- [ ] `Assignments.Initializer.hbs` - Use `GeneratedFileHeader` partial
- [ ] `Elements.Initializer.hbs` - Use `GeneratedFileHeader` partial

## Additional Partials to Consider

Once the pattern is proven, consider extracting:

### `PartialClassOpen.hbs`
```handlebars
namespace {{GeneratedNamespace}}
{
    {{ClassComment}}
    {{ClassModifiers}} partial class {{ClassName}}
    {
```

### `PartialClassClose.hbs`
```handlebars
    }
}
```

### `InitializerMethodSignature.hbs`
```handlebars
public static async Task Initialize_{{NamedInstanceName}}Async({{RegistryClassName}} registry)
```

### `FactoryMethodSignature.hbs`
```handlebars
public static {{NamedInstanceQualifiedClassName}} Create({{RegistryClassName}} registry)
```

## Benefits

1. **DRY**: Header appears once, used 6+ times
2. **Consistency**: Change header format in one place
3. **Testing**: Test partials independently
4. **Maintainability**: Clear separation of reusable vs template-specific logic
5. **Documentation**: Partials directory serves as template component library

## Testing Checklist

- [ ] Partials load correctly from embedded resources or file system
- [ ] Partial context (variables) passed correctly
- [ ] Generated output identical to non-partial version
- [ ] Build/CI passes with partial templates
- [ ] No performance regression

## Questions to Answer

1. **Where are templates currently loaded?** (Find the Handlebars.Compile call)
2. **Are templates embedded resources or files?** (Check .csproj)
3. **Is there a single Handlebars instance or per-template?** (Partials need shared instance)

---

Next step: Find template loading code and update it to register partials.
